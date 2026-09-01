using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NeonHorde.EditorTools
{
    /// <summary>
    /// One-shot project setup: creates the URP pipeline + 2D-friendly renderer, a
    /// bloom post-processing profile, and the M0 Game scene, then wires build settings.
    /// Run via: menu "NeonHorde/Setup Project" or
    /// Unity.exe -executeMethod NeonHorde.EditorTools.ProjectBootstrapper.Run
    /// </summary>
    public static class ProjectBootstrapper
    {
        const string SettingsDir  = "Assets/_Project/Settings";
        const string ScenesDir    = "Assets/_Project/Scenes";
        const string UrpAssetPath = SettingsDir + "/NeonHorde_URP.asset";
        const string RendererPath = SettingsDir + "/NeonHorde_Renderer.asset";
        const string VolumePath   = SettingsDir + "/NeonHorde_PostFX.asset";
        const string GameScenePath = ScenesDir + "/Game.unity";

        [MenuItem("NeonHorde/Setup Project")]
        public static void Run()
        {
            EnsureFolders();

            PlayerSettings.colorSpace = ColorSpace.Linear;

            var urp = CreateUrp();
            GraphicsSettings.defaultRenderPipeline = urp;
            ApplyToAllQualityLevels(urp);

            var profile = CreatePostFxProfile();
            EnsureAlwaysIncludedShaders();
            BuildGameScene(profile);
            AddSceneToBuild(GameScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NeonHorde] Setup Project complete — URP + PostFX + Game scene ready.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(ScenesDir);
            AssetDatabase.Refresh();
        }

        static UniversalRenderPipelineAsset CreateUrp()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (existing != null) return existing;

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "NeonHorde_Renderer";
            AssetDatabase.CreateAsset(rendererData, RendererPath);

            var urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            urp.name = "NeonHorde_URP";

            var so = new SerializedObject(urp);
            var list = so.FindProperty("m_RendererDataList");
            if (list != null)
            {
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }
            var idx = so.FindProperty("m_DefaultRendererIndex");
            if (idx != null) idx.intValue = 0;
            var hdr = so.FindProperty("m_SupportsHDR");
            if (hdr != null) hdr.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(urp, UrpAssetPath);
            EditorUtility.SetDirty(urp);
            return urp;
        }

        static void ApplyToAllQualityLevels(RenderPipelineAsset urp)
        {
            int current = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        /// <summary>
        /// The instanced renderers (enemies/projectiles/gems/terrain) build materials at
        /// runtime via Shader.Find. In a player build that shader is stripped unless it's
        /// in Always Included Shaders — which is why on-device everything but the player
        /// was invisible. Force the ones we need into the build.
        /// </summary>
        static void EnsureAlwaysIncludedShaders()
        {
            string[] wanted =
            {
                "Universal Render Pipeline/Unlit",
                "Universal Render Pipeline/2D/Sprite-Unlit-Default",
                "Sprites/Default",
            };

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0) { Debug.LogWarning("[Setup] GraphicsSettings.asset not loadable"); return; }
            var so = new SerializedObject(assets[0]);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) { Debug.LogWarning("[Setup] m_AlwaysIncludedShaders not found"); return; }

            var have = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var s = list.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (s != null) have.Add(s.name);
            }

            foreach (var name in wanted)
            {
                if (have.Contains(name)) continue;
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning($"[Setup] shader not found: {name}"); continue; }
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedProperties();
            Debug.Log("[Setup] Always Included Shaders ensured.");
        }

        static VolumeProfile CreatePostFxProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumePath);
            }

            var bloom = profile.TryGet<Bloom>(out var b) ? b : profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(0.75f);
            bloom.intensity.Override(2.2f);
            bloom.scatter.Override(0.78f);
            bloom.tint.Override(new Color(0.9f, 0.95f, 1f));

            var tone = profile.TryGet<Tonemapping>(out var tm) ? tm : profile.Add<Tonemapping>(true);
            tone.active = true;
            tone.mode.Override(TonemappingMode.ACES);

            var vig = profile.TryGet<Vignette>(out var v) ? v : profile.Add<Vignette>(true);
            vig.active = true;
            vig.intensity.Override(0.34f);
            vig.smoothness.Override(0.4f);
            vig.color.Override(new Color(0.02f, 0.0f, 0.05f));

            var ca = profile.TryGet<ChromaticAberration>(out var c) ? c : profile.Add<ChromaticAberration>(true);
            ca.active = true;
            ca.intensity.Override(0.12f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static void BuildGameScene(VolumeProfile profile)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = Balance.CameraOrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            var follow = camGo.AddComponent<CameraFollow>();

            var volGo = new GameObject("Global Volume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;

            var sysGo = new GameObject("Systems");
            sysGo.AddComponent<RunManager>();

            var playerGo = new GameObject("Player");
            playerGo.transform.position = Vector3.zero;
            playerGo.AddComponent<PlayerInputReader>();
            playerGo.AddComponent<PlayerController>();
            var shape = playerGo.AddComponent<NeonShape>();
            shape.kind = NeonShape.Kind.Circle;
            shape.color = Palette.Player;
            shape.size = 0.7f;
            shape.sortingOrder = 10;
            playerGo.AddComponent<PlayerWeaponRig>();

            follow.target = playerGo.transform;

            var bgGo = new GameObject("Neon Grid");
            bgGo.AddComponent<NeonGridBackground>();

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
                scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------------------------------------------------------------
        // M1 — wires combat systems + HUD/overlays into the Game scene.
        // ---------------------------------------------------------------------

        [MenuItem("NeonHorde/Setup M1 Scene")]
        public static void SetupM1Scene()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var systems = GameObject.Find("Systems");
            if (systems == null)
            {
                systems = new GameObject("Systems");
                systems.AddComponent<RunManager>();
            }
            AddIfMissing<Arena>(systems);
            AddIfMissing<FlowFieldSystem>(systems);
            AddIfMissing<DirectorSystem>(systems);
            AddIfMissing<EnemyManager>(systems);
            AddIfMissing<WeaponSystem>(systems);
            AddIfMissing<ProjectileManager>(systems);
            AddIfMissing<HostileProjectileManager>(systems);
            AddIfMissing<PickupManager>(systems);
            AddIfMissing<DamageNumberSystem>(systems);
            AddIfMissing<NeonVfx>(systems);
            AddIfMissing<FirstRunHint>(systems);
            AddIfMissing<DebugSpawner>(systems);
            AddIfMissing<DevHud>(systems);

            var oldCanvas = GameObject.Find("HUDCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);
            BuildUi();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[NeonHorde] Setup Game Scene complete — M2 systems + HUD wired.");
        }

        static T AddIfMissing<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void BuildUi()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("HUDCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;   // match height; width flexes with the phone
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // --- Joystick (below everything so panels take click priority) ---
            var joyArea = NewImage("JoystickArea", root, new Color(1, 1, 1, 0));
            Stretch(joyArea.rectTransform);
            joyArea.raycastTarget = true;
            var joy = joyArea.gameObject.AddComponent<VirtualJoystick>();
            var ring = NewImage("Ring", joyArea.transform, new Color(0.3f, 0.8f, 1f, 0.18f), KnobSprite());
            ring.rectTransform.sizeDelta = new Vector2(220, 220);
            var knob = NewImage("Knob", joyArea.transform, new Color(0.4f, 0.9f, 1f, 0.40f), KnobSprite());
            knob.rectTransform.sizeDelta = new Vector2(96, 96);
            joy.ring = ring.rectTransform;
            joy.knob = knob.rectTransform;

            // --- HUD ---
            var hud = NewRect("HUD", root);
            Stretch(hud);
            hud.gameObject.AddComponent<RuntimeVignette>(); // builds a full-screen vignette Image at runtime

            var xpBg = NewImage("XpBarBg", hud, new Color(0.05f, 0.06f, 0.09f, 0.9f), UiSprite());
            AnchorTopStretch(xpBg.rectTransform, 18f);
            var xpFill = NewImage("XpFill", xpBg.transform, new Color(0.20f, 1.4f, 1.9f, 1f), UiSprite());
            Stretch(xpFill.rectTransform);
            MakeFilled(xpFill);
            xpFill.fillAmount = 0f;

            var info = NewText("Info", hud, "00:00   Lv 1   Kills 0", 28, TextAnchor.UpperLeft, Color.white);
            info.rectTransform.anchorMin = new Vector2(0, 1);
            info.rectTransform.anchorMax = new Vector2(0, 1);
            info.rectTransform.pivot = new Vector2(0, 1);
            info.rectTransform.sizeDelta = new Vector2(700, 44);
            info.rectTransform.anchoredPosition = new Vector2(24, -28);

            var hpBg = NewImage("HpBarBg", hud, new Color(0.05f, 0.06f, 0.09f, 0.9f), UiSprite());
            hpBg.rectTransform.anchorMin = new Vector2(0.5f, 0);
            hpBg.rectTransform.anchorMax = new Vector2(0.5f, 0);
            hpBg.rectTransform.pivot = new Vector2(0.5f, 0);
            hpBg.rectTransform.sizeDelta = new Vector2(540, 26);
            hpBg.rectTransform.anchoredPosition = new Vector2(0, 44);
            var hpFill = NewImage("HpFill", hpBg.transform, new Color(0.30f, 1.6f, 0.7f, 1f), UiSprite());
            Stretch(hpFill.rectTransform);
            MakeFilled(hpFill);
            hpFill.fillAmount = 1f;

            // boss bar (hidden until a boss is alive)
            var bossBg = NewImage("BossBarBg", hud, new Color(0.09f, 0.03f, 0.06f, 0.92f), UiSprite());
            bossBg.rectTransform.anchorMin = new Vector2(0.5f, 1);
            bossBg.rectTransform.anchorMax = new Vector2(0.5f, 1);
            bossBg.rectTransform.pivot = new Vector2(0.5f, 1);
            bossBg.rectTransform.sizeDelta = new Vector2(720, 20);
            bossBg.rectTransform.anchoredPosition = new Vector2(0, -40);
            var bossFill = NewImage("BossFill", bossBg.transform, new Color(2.6f, 0.3f, 1.4f, 1f), UiSprite());
            Stretch(bossFill.rectTransform);
            MakeFilled(bossFill);

            var hudCtl = hud.gameObject.AddComponent<HudController>();
            hudCtl.xpFill = xpFill;
            hudCtl.hpFill = hpFill;
            hudCtl.infoText = info;
            hudCtl.bossBarRoot = bossBg.gameObject;
            hudCtl.bossFill = bossFill;

            // --- Level Up ---
            var luRoot = NewRect("LevelUp", root);
            Stretch(luRoot);
            var luCtl = luRoot.gameObject.AddComponent<LevelUpOverlay>();
            var luPanel = NewRect("Panel", luRoot);
            Stretch(luPanel);
            var luDim = NewImage("Dim", luPanel, new Color(0, 0, 0, 0.72f));
            Stretch(luDim.rectTransform);
            luDim.raycastTarget = true;
            var luTitle = NewText("Title", luPanel, "LEVEL UP", 60, TextAnchor.MiddleCenter, new Color(0.4f, 1.6f, 2f, 1f));
            CenterRect(luTitle.rectTransform, new Vector2(800, 120), new Vector2(0, 320));
            luCtl.panel = luPanel.gameObject;
            luCtl.titleText = luTitle;
            for (int i = 0; i < 3; i++)
            {
                var btn = NewButton("Btn" + i, luPanel, out var lbl, "옵션 " + (i + 1));
                CenterRect((RectTransform)btn.transform, new Vector2(680, 130), new Vector2(-40, 120 - i * 150));
                luCtl.buttons[i] = btn;
                luCtl.labels[i] = lbl;

                var ban = NewButton("Ban" + i, luPanel, out _, "✕");
                CenterRect((RectTransform)ban.transform, new Vector2(110, 130), new Vector2(360, 120 - i * 150));
                luCtl.banishButtons[i] = ban;
            }
            var reroll = NewButton("Reroll", luPanel, out var rerollLbl, "리롤 (1)");
            CenterRect((RectTransform)reroll.transform, new Vector2(400, 110), new Vector2(0, -330));
            luCtl.rerollButton = reroll;
            luCtl.rerollLabel = rerollLbl;

            // --- Game Over ---
            var goRoot = NewRect("GameOver", root);
            Stretch(goRoot);
            var goCtl = goRoot.gameObject.AddComponent<GameOverOverlay>();
            var goPanel = NewRect("Panel", goRoot);
            Stretch(goPanel);
            var goDim = NewImage("Dim", goPanel, new Color(0, 0, 0, 0.8f));
            Stretch(goDim.rectTransform);
            goDim.raycastTarget = true;
            var goTitle = NewText("Title", goPanel, "GAME OVER", 64, TextAnchor.MiddleCenter, new Color(2.4f, 0.5f, 0.5f, 1f));
            CenterRect(goTitle.rectTransform, new Vector2(800, 120), new Vector2(0, 340));

            var promptGroup = NewRect("Prompt", goPanel);
            Stretch(promptGroup);
            var adRevive = NewButton("AdRevive", promptGroup, out _, "광고 보고 부활");
            CenterRect((RectTransform)adRevive.transform, new Vector2(620, 150), new Vector2(0, 40));
            var seeResult = NewButton("SeeResult", promptGroup, out _, "결과 보기");
            CenterRect((RectTransform)seeResult.transform, new Vector2(620, 130), new Vector2(0, -140));

            var resultGroup = NewRect("Result", goPanel);
            Stretch(resultGroup);
            var goResult = NewText("ResultText", resultGroup, "생존 00:00\n레벨 1\n처치 0\n골드 +0", 42, TextAnchor.MiddleCenter, Color.white);
            CenterRect(goResult.rectTransform, new Vector2(800, 300), new Vector2(0, 120));
            var doubleGold = NewButton("DoubleGold", resultGroup, out _, "골드 2배 (광고)");
            CenterRect((RectTransform)doubleGold.transform, new Vector2(620, 120), new Vector2(0, -60));
            var retry = NewButton("Retry", resultGroup, out _, "다시하기");
            CenterRect((RectTransform)retry.transform, new Vector2(520, 120), new Vector2(0, -200));
            var menu = NewButton("Menu", resultGroup, out _, "메뉴");
            CenterRect((RectTransform)menu.transform, new Vector2(520, 120), new Vector2(0, -335));

            goCtl.panel = goPanel.gameObject;
            goCtl.promptGroup = promptGroup.gameObject;
            goCtl.resultGroup = resultGroup.gameObject;
            goCtl.adReviveButton = adRevive;
            goCtl.seeResultButton = seeResult;
            goCtl.retryButton = retry;
            goCtl.menuButton = menu;
            goCtl.doubleGoldButton = doubleGold;
            goCtl.resultText = goResult;
            goCtl.titleText = goTitle;

            // --- Boss banner ---
            var bannerRoot = NewRect("BossBanner", root);
            Stretch(bannerRoot);
            var bannerCtl = bannerRoot.gameObject.AddComponent<BossBanner>();
            var bannerText = NewText("Label", bannerRoot, "B O S S", 90, TextAnchor.MiddleCenter, new Color(2.4f, 0.3f, 0.3f, 0f));
            CenterRect(bannerText.rectTransform, new Vector2(900, 160), new Vector2(0, 260));
            bannerCtl.label = bannerText;
        }

        // ---------------------------------------------------------------------
        // M3 — Boot + MainMenu scenes and build settings.
        // ---------------------------------------------------------------------

        const string BootScenePath = ScenesDir + "/Boot.unity";
        const string MenuScenePath = ScenesDir + "/MainMenu.unity";

        [MenuItem("NeonHorde/Setup Everything")]
        public static void SetupEverything()
        {
            Run();
            SetupM1Scene();
            SetupMenuScenes();
            ConfigureBuild();
            BrandingGen.Apply();
            Debug.Log("[NeonHorde] Setup Everything complete.");
        }

        [MenuItem("NeonHorde/Configure Build Settings")]
        public static void ConfigureBuild()
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.muteOtherAudioSources = true;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            AssetDatabase.SaveAssets();
            Debug.Log("[NeonHorde] Build settings configured (Android IL2CPP/ARM64/minSDK24, iOS IL2CPP/13.0).");
        }

        [MenuItem("NeonHorde/Setup Menu Scenes")]
        public static void SetupMenuScenes()
        {
            Directory.CreateDirectory(ScenesDir);

            var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();
            new GameObject("Boot").AddComponent<BootFlow>();
            EditorSceneManager.SaveScene(boot, BootScenePath);

            var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mcam = new GameObject("Main Camera") { tag = "MainCamera" };
            var mc = mcam.AddComponent<Camera>();
            mc.orthographic = true;
            mc.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);
            mc.clearFlags = CameraClearFlags.SolidColor;
            mcam.AddComponent<AudioListener>();
            new GameObject("MainMenu").AddComponent<MainMenuController>();
            EditorSceneManager.SaveScene(menu, MenuScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[NeonHorde] Menu scenes built + build settings ordered (Boot/MainMenu/Game).");
        }

        // ---- uGUI construction helpers ----

        static Font _uiFont;

        static Font UiFont()
        {
            if (_uiFont != null) return _uiFont;
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null) _uiFont = AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf");
            if (_uiFont == null) _uiFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return _uiFont;
        }

        static Sprite UiSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite KnobSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void AnchorTopStretch(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = Vector2.zero;
        }

        static void CenterRect(RectTransform rt, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        static Image NewImage(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            if (sprite != null) img.type = Image.Type.Sliced;
            return img;
        }

        static void MakeFilled(Image img)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        static Text NewText(string name, Transform parent, string content, int size, TextAnchor anchor, Color color)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UiFont();
            t.text = content;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static Button NewButton(string name, Transform parent, out Text label, string labelText)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.10f, 0.16f, 0.24f, 0.95f);
            img.sprite = UiSprite();
            img.type = Image.Type.Sliced;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            label = NewText("Label", rt, labelText, 34, TextAnchor.MiddleCenter, Color.white);
            Stretch((RectTransform)label.transform);
            return btn;
        }
    }
}
