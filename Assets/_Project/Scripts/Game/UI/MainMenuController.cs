using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>
    /// Code-built main menu: play, character select (cores / IAP unlock), permanent
    /// shop (gold), and quests (claim cores). Reads GameBootstrap services.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        MetaController _meta;
        QuestService _quests;
        IIapService _iap;
        IAccountService _account;

        RectTransform _root;
        Text _currency;
        Text _charLabel;
        Text _bannerLabel;
        GameObject _bannerRoot;
        GameObject _panelHost;

        void Awake()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;   // match height; width flexes with the phone
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)canvasGo.transform;

            gameObject.AddComponent<MenuBackdrop>();
        }


        void Start()
        {
            _meta = GameBootstrap.MetaCtl;
            _quests = GameBootstrap.Quests;
            ServiceLocator.TryGet(out _iap);
            ServiceLocator.TryGet(out _account);
            BuildHome();
            MaybeShowSignupNudge();
        }

        void MaybeShowSignupNudge()
        {
            if (_account == null || _account.IsLinked) return;
            if (_meta.State.signupNudgeShown) return;
            if (_meta.State.totalRuns < 3) return;
            _meta.State.signupNudgeShown = true;
            _meta.Save();
            OpenAccount(nudge: true);
        }

        Text _goldChip, _coreChip;

        static readonly Color Slate  = new Color(0.34f, 0.42f, 0.52f, 1f);
        static readonly Color Plum   = new Color(0.46f, 0.34f, 0.5f, 1f);

        // full-width button in the top-anchored stack; advances the y cursor
        Button Stack(ref float y, string name, string label, int size, Color tint, float h, UnityEngine.Events.UnityAction onClick)
        {
            var b = UiKit.Button(name, _root, label, size, out _, tint);
            UiKit.RowStretch((RectTransform)b.transform, 40f, 40f, h, y);
            b.onClick.AddListener(onClick);
            y += h + 16f;
            return b;
        }

        void BuildHome()
        {
            float top = 150f;

            UiKit.Title("NEON HORDE", _root, new Vector2(0.5f, 1f), new Vector2(0, -top - 40f), 84, UiKit.Accent);

            float y = top + 150f;

            // currency chips (two half-width)
            var gcRoot = UiKit.Chip("GoldChip", _root, "0", UiKit.Gold, NeonShapeKind.Disc).rectTransform.parent as RectTransform;
            gcRoot.anchorMin = new Vector2(0f, 1f); gcRoot.anchorMax = new Vector2(0.5f, 1f);
            gcRoot.pivot = new Vector2(0.5f, 1f);
            gcRoot.offsetMin = new Vector2(40f, -y - 70f); gcRoot.offsetMax = new Vector2(-8f, -y);
            _goldChip = gcRoot.GetComponentInChildren<Text>();
            var ccRoot = UiKit.Chip("CoreChip", _root, "0", UiKit.Core, NeonShapeKind.Diamond).rectTransform.parent as RectTransform;
            ccRoot.anchorMin = new Vector2(0.5f, 1f); ccRoot.anchorMax = new Vector2(1f, 1f);
            ccRoot.pivot = new Vector2(0.5f, 1f);
            ccRoot.offsetMin = new Vector2(8f, -y - 70f); ccRoot.offsetMax = new Vector2(-40f, -y);
            _coreChip = ccRoot.GetComponentInChildren<Text>();
            y += 92f;

            y += 14f;
            _charLabel = UiKit.Text("Char", _root, "", 26, TextAnchor.MiddleCenter);
            UiKit.Place(_charLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(940, 40), new Vector2(0, -y));
            _charLabel.color = UiKit.Ink;
            y += 62f;

            Stack(ref y, "Play", Loc.T("play"), 56, UiKit.Accent, 150f,
                () => { RunConfig.Clear(); SceneManager.LoadScene("Game"); });
            Stack(ref y, "Daily", Loc.T("daily"), 36, UiKit.Gold, 92f, () =>
                { RunConfig.SetRun(_meta.SelectedCharacter, DailySeed(), daily: true); SceneManager.LoadScene("Game"); });
            Stack(ref y, "CharacterBtn", Loc.T("character"), 36, Slate, 104f, OpenCharacters);
            Stack(ref y, "ShopBtn", Loc.T("shop"), 36, Slate, 104f, OpenShop);
            Stack(ref y, "QuestBtn", Loc.T("quests"), 36, Slate, 104f, OpenQuests);

            // Store / Settings row (two half-width) anchored to the BOTTOM
            var store = UiKit.Button("StoreBtn", _root, Loc.T("store"), 32, out _, Plum);
            var srt = (RectTransform)store.transform;
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(0.5f, 0f); srt.pivot = new Vector2(0.5f, 0f);
            srt.offsetMin = new Vector2(40f, 190f); srt.offsetMax = new Vector2(-8f, 280f);
            store.onClick.AddListener(OpenStore);
            var settings = UiKit.Button("SettingsBtn", _root, Loc.T("settings"), 32, out _, Slate);
            var sert = (RectTransform)settings.transform;
            sert.anchorMin = new Vector2(0.5f, 0f); sert.anchorMax = new Vector2(1f, 0f); sert.pivot = new Vector2(0.5f, 0f);
            sert.offsetMin = new Vector2(8f, 190f); sert.offsetMax = new Vector2(-40f, 280f);
            settings.onClick.AddListener(OpenSettings);

            // guest banner along the very bottom
            var banner = UiKit.Image("Banner", _root, new Color(UiKit.Accent.r, UiKit.Accent.g, UiKit.Accent.b, 0.14f));
            _bannerRoot = banner.gameObject;
            var brt = banner.rectTransform;
            brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 0f); brt.pivot = new Vector2(0.5f, 0f);
            brt.offsetMin = new Vector2(0f, 0f); brt.offsetMax = new Vector2(0f, 170f);
            _bannerLabel = UiKit.Text("txt", brt, "", 22, TextAnchor.MiddleLeft);
            var blr = _bannerLabel.rectTransform;
            blr.anchorMin = new Vector2(0f, 0f); blr.anchorMax = new Vector2(1f, 1f);
            blr.offsetMin = new Vector2(24f, 10f); blr.offsetMax = new Vector2(-190f, -10f);
            _bannerLabel.color = UiKit.Ink;
            var linkBtn = UiKit.Button("link", brt, Loc.T("select") == "선택" ? "연결" : "LINK", 28, out _, UiKit.Accent);
            var lrt = (RectTransform)linkBtn.transform;
            lrt.anchorMin = new Vector2(1f, 0.5f); lrt.anchorMax = new Vector2(1f, 0.5f); lrt.pivot = new Vector2(1f, 0.5f);
            lrt.sizeDelta = new Vector2(140f, 78f); lrt.anchoredPosition = new Vector2(-24f, 6f);
            linkBtn.onClick.AddListener(() => OpenAccount(false));

            RefreshHome();
        }

        static int DailySeed()
        {
            string d = DateTime.UtcNow.ToString("yyyy-MM-dd");
            ulong h = 1469598103934665603UL;
            foreach (char c in d) { h ^= c; h *= 1099511628211UL; }
            return unchecked((int)h);
        }

        void RefreshHome()
        {
            if (_meta != null)
            {
                if (_goldChip != null) _goldChip.text = _meta.Gold.ToString();
                if (_coreChip != null) _coreChip.text = _meta.Cores.ToString();
                _charLabel.text = $"{CharacterCatalog.Get(_meta.SelectedCharacter).name}  ·  {CharacterCatalog.Get(_meta.SelectedCharacter).abilityText}";
            }
            if (_bannerRoot != null)
            {
                bool guest = _account != null && !_account.IsLinked;
                _bannerRoot.SetActive(guest);
                if (guest) _bannerLabel.text = Loc.T("guest_warn");
            }
        }

        // ---------- panels ----------

        RectTransform _scrollContent;
        const float RowH = 148f, RowStride = 160f;

        RectTransform OpenPanel(string title)
        {
            ClosePanel();
            _panelHost = new GameObject("Panel", typeof(RectTransform));
            var host = (RectTransform)_panelHost.transform;
            host.SetParent(_root, false);
            UiKit.Stretch(host);

            var dim = UiKit.Image("Dim", host, UiKit.Dim);
            UiKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            // card: full width minus a small margin, ~90% height, centred — fits any phone
            var card = UiKit.Image("Card", host, UiKit.Panel);
            var crt = card.rectTransform;
            crt.anchorMin = new Vector2(0f, 0.05f);
            crt.anchorMax = new Vector2(1f, 0.95f);
            crt.offsetMin = new Vector2(20f, 0f);
            crt.offsetMax = new Vector2(-20f, 0f);

            var head = UiKit.Text("Head", card.transform, title, 52);
            UiKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(900, 84), new Vector2(0, -24));

            var back = UiKit.Button("Back", card.transform, Loc.T("close"), 38, out _);
            UiKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(360, 96), new Vector2(0, 24));
            back.onClick.AddListener(() => { ClosePanel(); RefreshHome(); });

            // scroll viewport between head and back
            var viewport = UiKit.Rect("Viewport", card.transform);
            viewport.anchorMin = new Vector2(0f, 0f); viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(8f, 140f);
            viewport.offsetMax = new Vector2(-8f, -120f);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

            _scrollContent = UiKit.Rect("Content", viewport);
            _scrollContent.anchorMin = new Vector2(0f, 1f); _scrollContent.anchorMax = new Vector2(1f, 1f);
            _scrollContent.pivot = new Vector2(0.5f, 1f);
            _scrollContent.offsetMin = new Vector2(0f, 0f); _scrollContent.offsetMax = new Vector2(0f, 0f);
            _scrollContent.sizeDelta = new Vector2(0f, 0f);

            var sr = viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            sr.content = _scrollContent;
            sr.viewport = viewport;
            sr.horizontal = false;
            sr.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            return _scrollContent;
        }

        void ClosePanel()
        {
            if (_panelHost != null) Destroy(_panelHost);
            _panelHost = null;
            _scrollContent = null;
        }

        void Row(RectTransform content, int index, out RectTransform row)
        {
            row = UiKit.Rect($"Row{index}", content);
            UiKit.RowStretch(row, 6f, 6f, RowH, index * RowStride);
            var bg = UiKit.Image("bg", row, UiKit.Card);
            UiKit.Stretch(bg.rectTransform);
            float need = (index + 1) * RowStride + 8f;
            if (content.sizeDelta.y < need) content.sizeDelta = new Vector2(0f, need);
        }

        // ---------- characters ----------

        void OpenCharacters()
        {
            var list = OpenPanel("캐릭터");
            for (int i = 0; i < CharacterCatalog.All.Length; i++)
            {
                var def = CharacterCatalog.Get((CharacterId)i);
                Row(list, i, out var row);

                UiKit.Label("name", row, def.name, 34, 12f, 40f, 280f);
                UiKit.Label("abil", row, def.abilityText, 20, 56f, 78f, 280f);

                var id = (CharacterId)i;
                bool owned = _meta.IsCharacterUnlocked(id);
                bool selected = _meta.SelectedCharacter == id;

                if (!owned)
                {
                    bool hasIap = IapCatalog.TryGet(def.iapProductId ?? "", out var prod);
                    var btn = UiKit.RightButton("unlock", row, $"{def.coreCost} 코어", 26, out _, 250f, 78f, hasIap ? 30f : 0f);
                    btn.onClick.AddListener(() =>
                    {
                        if (_meta.UnlockCharacterWithCores(id)) { _meta.SelectCharacter(id); }
                        OpenCharacters();
                        RefreshHome();
                    });
                    if (hasIap)
                    {
                        var buy = UiKit.RightButton("iap", row, prod.priceText, 22, out _, 250f, 54f, -40f);
                        buy.onClick.AddListener(() => _iap?.Purchase(prod.id, r =>
                        {
                            if (r.success) IapFulfillment.Grant(prod.id, _meta);
                            OpenCharacters();
                            RefreshHome();
                        }));
                    }
                }
                else
                {
                    var btn = UiKit.RightButton("sel", row, selected ? "선택됨" : "선택", 28, out var bl, 230f, 96f);
                    btn.interactable = !selected;
                    if (selected) bl.color = UiKit.Accent;
                    btn.onClick.AddListener(() => { _meta.SelectCharacter(id); OpenCharacters(); RefreshHome(); });
                }
            }
        }

        // ---------- shop ----------

        void OpenShop()
        {
            var list = OpenPanel("영구 강화 (골드)");
            var powers = (PowerId[])Enum.GetValues(typeof(PowerId));
            for (int i = 0; i < powers.Length; i++)
            {
                var def = PowerCatalog.Get(powers[i]);
                int lvl = _meta.PowerLevel(powers[i]);
                Row(list, i, out var row);

                UiKit.Label("name", row, def.name, 30, 14f, 38f, 240f);
                UiKit.Label("desc", row, $"{def.desc}   ({lvl}/{def.maxLevel})", 20, 56f, 60f, 240f);

                var id = powers[i];
                bool max = lvl >= def.maxLevel;
                string btnLabel = max ? "MAX" : $"{def.CostForLevel(lvl)} G";
                var btn = UiKit.RightButton("buy", row, btnLabel, 28, out _, 210f, 96f);
                btn.interactable = !max && _meta.Gold >= def.CostForLevel(lvl);
                btn.onClick.AddListener(() => { _meta.BuyPower(id); OpenShop(); RefreshHome(); });
            }
        }

        // ---------- quests ----------

        void OpenQuests()
        {
            _quests?.EnsureDaily();
            var list = OpenPanel("퀘스트");

            int idx = 0;
            AddQuestSection(list, ref idx, "일일", _quests.Daily);
            AddQuestSection(list, ref idx, "마일스톤", _quests.Milestones);
        }

        void AddQuestSection(RectTransform list, ref int idx, string header,
                             System.Collections.Generic.IReadOnlyList<MetaState.QuestEntry> entries)
        {
            var hRow = UiKit.Rect($"h{idx}", list);
            UiKit.RowStretch(hRow, 6f, 6f, 56f, idx * RowStride);
            var h = UiKit.Label("t", hRow, header, 28, 8f, 44f, 0f);
            h.color = UiKit.Accent;
            h.fontStyle = FontStyle.Bold;
            idx++;

            foreach (var e in entries)
            {
                var defN = QuestCatalog.Find(e.id);
                if (defN == null) continue;
                var def = defN.Value;
                Row(list, idx, out var row);

                UiKit.Label("name", row, def.description, 26, 14f, 38f, 200f);
                UiKit.Label("prog", row,
                    $"{Mathf.Min(e.progress, def.target)}/{def.target}   +{def.rewardCores} 코어", 20, 56f, 40f, 200f);

                string id = e.id;
                bool claimed = e.claimed;
                bool canClaim = _quests.CanClaim(id);
                var btn = UiKit.RightButton("claim", row, claimed ? "완료" : (canClaim ? "수령" : "진행중"), 26, out _, 170f, 92f);
                btn.interactable = canClaim;
                btn.onClick.AddListener(() => { _quests.Claim(id); OpenQuests(); RefreshHome(); });

                idx++;
            }
        }

        // ---------- store (IAP) ----------

        void OpenStore()
        {
            var list = OpenPanel("스토어");
            var products = IapCatalog.All;
            for (int i = 0; i < products.Length; i++)
            {
                var p = products[i];
                Row(list, i, out var row);

                string sub = p.kind switch
                {
                    IapKind.CoreBundle => $"코어 +{p.cores}",
                    IapKind.Character => "캐릭터 즉시 해금",
                    IapKind.RemoveAds => "리워드 외 광고 제거",
                    IapKind.StarterPack => $"코어 +{p.cores}, 골드 +{p.gold}, 광고 제거",
                    _ => ""
                };
                UiKit.Label("name", row, p.title, 30, 14f, 38f, 230f);
                UiKit.Label("desc", row, sub, 20, 56f, 60f, 230f);

                bool owned = (p.kind == IapKind.RemoveAds && _meta.State.adsRemoved)
                             || (p.kind == IapKind.Character && _meta.IsCharacterUnlocked(p.character))
                             || _meta.State.ownedProducts.Contains(p.id) && p.kind != IapKind.CoreBundle;

                var id = p.id;
                var btn = UiKit.RightButton("buy", row, owned ? "보유" : p.priceText, 26, out _, 200f, 96f);
                btn.interactable = !owned;
                btn.onClick.AddListener(() => _iap?.Purchase(id, r =>
                {
                    if (r.success) IapFulfillment.Grant(id, _meta);
                    OpenStore();
                    RefreshHome();
                }));
            }
        }

        // ---------- account ----------

        void OpenAccount(bool nudge)
        {
            var list = OpenPanel(nudge ? "진행상황을 지키세요" : "계정");

            var bodyRow = UiKit.Rect("bodyRow", list);
            UiKit.RowStretch(bodyRow, 6f, 6f, 220f, 0f);
            var body = UiKit.Label("body", bodyRow, _account != null && _account.IsLinked
                    ? $"연결됨: {_account.DisplayName}"
                    : "게스트 모드에서는 데이터가 이 기기에만 저장됩니다.\n계정을 연결하면 클라우드에 백업되어\n앱을 지우거나 기기를 바꿔도 이어집니다.",
                24, 8f, 200f, 0f);
            list.sizeDelta = new Vector2(0f, 240f);

            if (_account == null || !_account.IsLinked)
            {
                AddAccBtn(list, "google", "Google로 연결", 1,
                    () => _account.LinkWithProvider("google", _ => { OpenAccount(false); RefreshHome(); }));
                AddAccBtn(list, "apple", "Apple로 연결", 2,
                    () => _account.LinkWithProvider("apple", _ => { OpenAccount(false); RefreshHome(); }));
                AddAccBtn(list, "email", "이메일로 연결", 3,
                    () => _account.LinkWithEmail($"player{_meta.State.totalRuns}@example.com", _ => { OpenAccount(false); RefreshHome(); }));

                var noteRow = UiKit.Rect("noteRow", list);
                UiKit.RowStretch(noteRow, 6f, 6f, 80f, 4 * RowStride + 20f);
                var note = UiKit.Label("t", noteRow, "* 실제 로그인 연동은 백엔드 연결 후 활성화됩니다.", 18, 8f, 60f, 0f);
                note.color = new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.6f);
                list.sizeDelta = new Vector2(0f, 5 * RowStride + 40f);
            }
        }

        void AddAccBtn(RectTransform list, string name, string label, int slot, UnityEngine.Events.UnityAction cb)
        {
            var b = UiKit.Button(name, list, label, 30, out _, Slate);
            UiKit.RowStretch((RectTransform)b.transform, 40f, 40f, 104f, slot * RowStride);
            b.onClick.AddListener(cb);
        }

        // ---------- settings ----------

        void OpenSettings()
        {
            var list = OpenPanel("설정");
            var s = _meta.State.settings;

            int r = 0;
            SettingToggleRow(list, r++, "BGM", () => s.bgm > 0.01f, on => { s.bgm = on ? 1f : 0f; ApplyAudio(); });
            SettingToggleRow(list, r++, "효과음", () => s.sfx > 0.01f, on => { s.sfx = on ? 1f : 0f; ApplyAudio(); });
            SettingToggleRow(list, r++, "진동", () => s.haptics, on => { s.haptics = on; _meta.Save(); });
            SettingToggleRow(list, r++, "저사양 모드", () => s.quality == 0, on => { s.quality = on ? 0 : 1; _meta.Save(); });

            Row(list, r, out var langRow);
            UiKit.Label("t", langRow, "언어 / Language", 28, 0f, RowH, 260f, TextAnchor.MiddleLeft);
            var langBtn = UiKit.RightButton("b", langRow, Loc.Current == Lang.Ko ? "한국어" : "English", 26, out _, 230f, 92f);
            langBtn.onClick.AddListener(() =>
            {
                s.language = s.language == 0 ? 1 : 0;
                Loc.Current = (Lang)s.language;
                _meta.Save();
                ClosePanel();
                int n = _root.childCount;
                for (int c = n - 1; c >= 0; c--) Destroy(_root.GetChild(c).gameObject);
                BuildHome();
                OpenSettings();
            });
            r++;

            Row(list, r, out var accRow);
            UiKit.Label("t", accRow, _account != null && _account.IsLinked ? $"계정: {_account.DisplayName}" : "계정: 게스트", 28, 0f, RowH, 260f, TextAnchor.MiddleLeft);
            var accBtn = UiKit.RightButton("b", accRow, _account != null && _account.IsLinked ? "연결됨" : "연결", 26, out _, 230f, 92f);
            accBtn.interactable = _account == null || !_account.IsLinked;
            accBtn.onClick.AddListener(() => OpenAccount(false));
            r++;

            Row(list, r, out var delRow);
            UiKit.Label("t", delRow, "저장 데이터 삭제", 28, 0f, RowH, 260f, TextAnchor.MiddleLeft);
            var delBtn = UiKit.RightButton("b", delRow, "삭제", 26, out _, 230f, 92f);
            delBtn.onClick.AddListener(() =>
            {
                if (ServiceLocator.TryGet<ISaveService>(out var sv)) sv.Delete();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }

        void SettingToggleRow(RectTransform list, int index, string label, Func<bool> get, Action<bool> set)
        {
            Row(list, index, out var row);
            UiKit.Label("t", row, label, 28, 0f, RowH, 220f, TextAnchor.MiddleLeft);
            var btn = UiKit.RightButton("b", row, get() ? "ON" : "OFF", 26, out var bl, 190f, 92f);
            if (get()) bl.color = UiKit.Accent;
            btn.onClick.AddListener(() => { set(!get()); OpenSettings(); });
        }

        void ApplyAudio()
        {
            _meta.Save();
            if (ServiceLocator.TryGet<IAudioService>(out var a))
                a.SetVolumes(_meta.State.settings.bgm, _meta.State.settings.sfx);
        }
    }
}
