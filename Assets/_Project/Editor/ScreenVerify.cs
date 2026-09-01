using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonHorde.EditorTools
{
    /// <summary>
    /// Headless visual check: enters play mode, drives the menu panels and a short
    /// gameplay session, and dumps screenshots to ./verify_shots/. Run with
    ///   Unity -batchmode -projectPath . -executeMethod NeonHorde.EditorTools.ScreenVerify.Run
    ///         -screen-width 720 -screen-height 1560 -logFile verify.log
    /// (no -nographics — we need the frame to render).
    /// </summary>
    public static class ScreenVerify
    {
        const string Dir = "verify_shots";
        static int _frame;
        static readonly List<(int at, string what)> _plan = new();

        public static void Run()
        {
            if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
            Directory.CreateDirectory(Dir);

            // keep statics + the update hook alive across the play-mode transition
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            _plan.Clear();
            _plan.Add((30, "open:menu"));
            _plan.Add((70, "shot:menu_home"));
            _plan.Add((90, "call:OpenCharacters"));
            _plan.Add((130, "shot:menu_characters"));
            _plan.Add((150, "call:OpenShop"));
            _plan.Add((190, "shot:menu_shop"));
            _plan.Add((210, "call:OpenQuests"));
            _plan.Add((250, "shot:menu_quests"));
            _plan.Add((270, "call:OpenSettings"));
            _plan.Add((310, "shot:menu_settings"));
            _plan.Add((330, "scene:Game"));
            _plan.Add((520, "spawn"));
            _plan.Add((560, "shot:game_spawned"));
            _plan.Add((330 + 500, "shot:game_run"));
            _plan.Add((330 + 900, "shot:game_late"));
            _plan.Add((330 + 960, "done"));

            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
            _frame = 0;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            _frame++;

            foreach (var step in _plan)
            {
                if (step.at != _frame) continue;
                var (kind, arg) = Split(step.what);
                switch (kind)
                {
                    case "shot": Shot(arg); break;
                    case "call": CallMenu(arg); break;
                    case "scene": SceneManager.LoadScene(arg); break;
                    case "spawn": SpawnEnemies(); break;
                    case "done":
                        EditorApplication.update -= Tick;
                        EditorApplication.isPlaying = false;
                        EditorApplication.delayCall += () => EditorApplication.Exit(0);
                        break;
                }
            }
        }

        static (string, string) Split(string s)
        {
            int i = s.IndexOf(':');
            return i < 0 ? (s, "") : (s.Substring(0, i), s.Substring(i + 1));
        }

        const int W = 720, H = 1560;
        static Camera _cap;
        static RenderTexture _rt;

        static void EnsureCap()
        {
            if (_cap != null) return;
            _rt = new RenderTexture(W, H, 24) { name = "verify_rt" };
            var go = new GameObject("VerifyCapCam");
            _cap = go.AddComponent<Camera>();
            _cap.targetTexture = _rt;
            _cap.cullingMask = ~0;
            _cap.depth = 100;
        }

        static void Shot(string name)
        {
            EnsureCap();
            var main = Camera.main;
            if (main != null)
            {
                _cap.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
                _cap.orthographic = main.orthographic;
                _cap.orthographicSize = main.orthographicSize;
                _cap.fieldOfView = main.fieldOfView;
                _cap.nearClipPlane = Mathf.Min(0.05f, main.nearClipPlane);
                _cap.farClipPlane = Mathf.Max(200f, main.farClipPlane);
                _cap.clearFlags = CameraClearFlags.SolidColor;
                _cap.backgroundColor = main.backgroundColor;
            }

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var restore = new List<(Canvas c, RenderMode m, Camera cam, float pd)>();
            foreach (var c in canvases)
            {
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                restore.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = _cap;
                c.planeDistance = 10f;
            }
            Canvas.ForceUpdateCanvases();
            _cap.Render();
            foreach (var r in restore) { r.c.renderMode = r.m; r.c.worldCamera = r.cam; r.c.planeDistance = r.pd; }

            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"[Verify] shot {name} (frame {_frame})");
        }

        static void SpawnEnemies()
        {
            var em = Object.FindFirstObjectByType<EnemyManager>();
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (em == null || pc == null) { Debug.LogWarning("[Verify] no EnemyManager/PlayerController"); return; }
            Vector2 p = pc.transform.position;
            void Sp(EnemyId id, Vector2 off, bool boss = false) => em.Spawn(id, p + off, false, boss);
            Sp(EnemyId.Walker, new Vector2(-2.5f, 2.0f));
            Sp(EnemyId.Swarmer, new Vector2(0f, 2.4f));
            Sp(EnemyId.Swarmer, new Vector2(0.8f, 2.8f));
            Sp(EnemyId.Tank, new Vector2(2.6f, 1.6f));
            Sp(EnemyId.Spitter, new Vector2(-2.8f, -1.5f));
            Sp(EnemyId.Exploder, new Vector2(1.8f, -2.2f));
            Sp(EnemyId.FrostTank, new Vector2(-1.2f, -3.0f));
            Sp(EnemyId.Walker, new Vector2(3.2f, -0.5f), true); // boss
            pc.TakeContactDamage(38);                            // so the HP bar shows
            Debug.Log("[Verify] spawned 8 enemies + hurt player");
        }

        static void CallMenu(string method)
        {
            var mm = Object.FindFirstObjectByType<MainMenuController>();
            if (mm == null) { Debug.LogWarning("[Verify] no MainMenuController"); return; }
            var m = typeof(MainMenuController).GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (m == null) { Debug.LogWarning("[Verify] no method " + method); return; }
            m.Invoke(mm, null);
            Debug.Log("[Verify] called " + method);
        }
    }
}
