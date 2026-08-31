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
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)canvasGo.transform;

            gameObject.AddComponent<MenuBackdrop>();
        }

        Image _logo;
        Text _charLabelSub;

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

        void BuildHome()
        {
            // logo mark
            var logo = UiKit.Rect("Logo", _root);
            logo.anchorMin = logo.anchorMax = new Vector2(0.5f, 1f);
            logo.pivot = new Vector2(0.5f, 1f);
            logo.sizeDelta = new Vector2(280, 280);
            logo.anchoredPosition = new Vector2(0, -140);
            _logo = logo.gameObject.AddComponent<Image>();
            _logo.raycastTarget = false;
            var loaded = Resources.Load<Sprite>("branding/logo_mark");
            _logo.sprite = loaded != null ? loaded : NeonArt.Sprite(NeonShapeKind.Star, 256, 0.6f, 0.5f);
            _logo.color = UiKit.Ink; // ink stamp, not a neon glow, on the paper backdrop

            UiKit.Title("NEON HORDE", _root, new Vector2(0.5f, 1f), new Vector2(0, -470), 92, UiKit.Accent);

            var strap = UiKit.Text("Strap", _root,
                "PAID FOR BY THE HORDE  ·  NOT AUTHORIZED BY ANY CANDIDATE", 22);
            UiKit.Place(strap.rectTransform, new Vector2(0.5f, 1f), new Vector2(1000, 40), new Vector2(0, -560));
            strap.color = UiKit.Ink;
            strap.fontStyle = FontStyle.Italic;

            // currency chips
            _goldChip = UiKit.Chip("GoldChip", _root, "0", UiKit.Gold, NeonShapeKind.Disc);
            UiKit.Place(_goldChip.rectTransform.parent as RectTransform, new Vector2(0.5f, 1f), new Vector2(280, 76), new Vector2(-150, -650));
            _coreChip = UiKit.Chip("CoreChip", _root, "0", UiKit.Core, NeonShapeKind.Diamond);
            UiKit.Place(_coreChip.rectTransform.parent as RectTransform, new Vector2(0.5f, 1f), new Vector2(280, 76), new Vector2(150, -650));

            _charLabel = UiKit.Text("Char", _root, "", 34);
            UiKit.Place(_charLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1000, 48), new Vector2(0, 300));
            _charLabel.color = UiKit.Ink;

            var play = UiKit.Button("Play", _root, Loc.T("play"), 62, out _, UiKit.Accent);
            UiKit.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(780, 190), new Vector2(0, 150));
            play.onClick.AddListener(() => { RunConfig.Clear(); SceneManager.LoadScene("Game"); });

            var daily = UiKit.Button("Daily", _root, Loc.T("daily"), 40, out _, new Color(1.6f, 1.1f, 0.3f, 1f));
            UiKit.Place((RectTransform)daily.transform, new Vector2(0.5f, 0.5f), new Vector2(780, 100), new Vector2(0, 20));
            daily.onClick.AddListener(() =>
            {
                RunConfig.SetRun(_meta.SelectedCharacter, DailySeed(), daily: true);
                SceneManager.LoadScene("Game");
            });

            AddNav("CharacterBtn", Loc.T("character"), -110, OpenCharacters);
            AddNav("ShopBtn", Loc.T("shop"), -240, OpenShop);
            AddNav("QuestBtn", Loc.T("quests"), -370, OpenQuests);

            var store = UiKit.Button("StoreBtn", _root, Loc.T("store"), 34, out _, new Color(1.4f, 0.6f, 1.6f, 1f));
            UiKit.Place((RectTransform)store.transform, new Vector2(0.5f, 0f), new Vector2(370, 92), new Vector2(-200, 250));
            store.onClick.AddListener(OpenStore);

            var settings = UiKit.Button("SettingsBtn", _root, Loc.T("settings"), 34, out _, new Color(0.6f, 0.8f, 1f, 1f));
            UiKit.Place((RectTransform)settings.transform, new Vector2(0.5f, 0f), new Vector2(370, 92), new Vector2(200, 250));
            settings.onClick.AddListener(OpenSettings);

            // account banner
            var bannerImg = _root.gameObject == null ? null : new GameObject("Banner", typeof(RectTransform)).AddComponent<Image>();
            _bannerRoot = bannerImg.gameObject;
            bannerImg.transform.SetParent(_root, false);
            bannerImg.sprite = NeonArt.Panel(48, 16, 2f, 0.4f, 4f);
            bannerImg.type = UnityEngine.UI.Image.Type.Sliced;
            bannerImg.color = new Color(1.4f, 0.9f, 0.3f, 1f);
            var brt = (RectTransform)_bannerRoot.transform;
            brt.anchorMin = new Vector2(0.5f, 0f); brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f); brt.sizeDelta = new Vector2(1000, 140); brt.anchoredPosition = new Vector2(0, 100);
            _bannerLabel = UiKit.Text("txt", brt, "", 25, TextAnchor.MiddleLeft);
            UiKit.Place(_bannerLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(680, 120), new Vector2(-130, 0));
            _bannerLabel.color = UiKit.Ink;
            var linkBtn = UiKit.Button("link", brt, "연결", 30, out _, new Color(0.2f, 1.4f, 1.9f, 1f));
            UiKit.Place((RectTransform)linkBtn.transform, new Vector2(1f, 0.5f), new Vector2(210, 100), new Vector2(-120, 0));
            linkBtn.onClick.AddListener(() => OpenAccount(false));

            RefreshHome();
        }

        void AddNav(string name, string label, float y, UnityEngine.Events.UnityAction onClick)
        {
            var b = UiKit.Button(name, _root, label, 42, out _, new Color(0.16f, 0.55f, 0.9f, 1f));
            UiKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(780, 118), new Vector2(0, y));
            b.onClick.AddListener(onClick);
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

            var card = UiKit.Image("Card", host, UiKit.Panel);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(980, 1500), Vector2.zero);

            var head = UiKit.Text("Head", card.transform, title, 54);
            UiKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(900, 90), new Vector2(0, -70));

            var back = UiKit.Button("Back", card.transform, Loc.T("close"), 40, out _);
            UiKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(400, 110), new Vector2(0, 90));
            back.onClick.AddListener(() => { ClosePanel(); RefreshHome(); });

            var list = UiKit.Rect("List", card.transform);
            list.anchorMin = new Vector2(0.5f, 0.5f);
            list.anchorMax = new Vector2(0.5f, 0.5f);
            list.pivot = new Vector2(0.5f, 1f);
            list.sizeDelta = new Vector2(900, 1200);
            list.anchoredPosition = new Vector2(0, 620);
            return list;
        }

        void ClosePanel()
        {
            if (_panelHost != null) Destroy(_panelHost);
            _panelHost = null;
        }

        void Row(RectTransform list, int index, out RectTransform row)
        {
            row = UiKit.Rect($"Row{index}", list);
            row.anchorMin = new Vector2(0.5f, 1f);
            row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(880, 150);
            row.anchoredPosition = new Vector2(0, -index * 165f);
            var bg = UiKit.Image("bg", row, UiKit.Card);
            UiKit.Stretch(bg.rectTransform);
        }

        // ---------- characters ----------

        void OpenCharacters()
        {
            var list = OpenPanel("캐릭터");
            for (int i = 0; i < CharacterCatalog.All.Length; i++)
            {
                var def = CharacterCatalog.Get((CharacterId)i);
                Row(list, i, out var row);

                var name = UiKit.Text("name", row, def.name, 40, TextAnchor.UpperLeft);
                UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(560, 44), new Vector2(150, -14));
                var abil = UiKit.Text("abil", row, def.abilityText, 26, TextAnchor.UpperLeft);
                UiKit.Place(abil.rectTransform, new Vector2(0f, 1f), new Vector2(560, 60), new Vector2(150, -64));

                var id = (CharacterId)i;
                bool owned = _meta.IsCharacterUnlocked(id);
                bool selected = _meta.SelectedCharacter == id;

                if (!owned)
                {
                    var btn = UiKit.Button("unlock", row, $"{def.coreCost} 코어로 해금", 28, out _);
                    UiKit.Place((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(320, 100), new Vector2(-170, 0));
                    btn.onClick.AddListener(() =>
                    {
                        if (_meta.UnlockCharacterWithCores(id)) { _meta.SelectCharacter(id); }
                        OpenCharacters();
                        RefreshHome();
                    });
                    if (IapCatalog.TryGet(def.iapProductId ?? "", out var prod))
                    {
                        var buy = UiKit.Button("iap", row, $"구매 {prod.priceText}", 22, out _);
                        UiKit.Place((RectTransform)buy.transform, new Vector2(1f, 0f), new Vector2(320, 54), new Vector2(-170, 10));
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
                    var btn = UiKit.Button("sel", row, selected ? "선택됨" : "선택", 30, out var bl);
                    UiKit.Place((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(240, 100), new Vector2(-130, 0));
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

                var name = UiKit.Text("name", row, def.name, 34, TextAnchor.UpperLeft);
                UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(520, 40), new Vector2(30, -14));
                var desc = UiKit.Text("desc", row, $"{def.desc}   ({lvl}/{def.maxLevel})", 24, TextAnchor.UpperLeft);
                UiKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(560, 56), new Vector2(30, -60));

                var id = powers[i];
                bool max = lvl >= def.maxLevel;
                string btnLabel = max ? "MAX" : $"{def.CostForLevel(lvl)} G";
                var btn = UiKit.Button("buy", row, btnLabel, 30, out _);
                UiKit.Place((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(300, 110), new Vector2(-160, 0));
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
            var h = UiKit.Text($"h{idx}", list, header, 30, TextAnchor.UpperLeft);
            h.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            h.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            h.rectTransform.pivot = new Vector2(0.5f, 1f);
            h.rectTransform.sizeDelta = new Vector2(880, 40);
            h.rectTransform.anchoredPosition = new Vector2(0, -idx * 165f);
            h.color = UiKit.Accent;
            idx++;

            foreach (var e in entries)
            {
                var defN = QuestCatalog.Find(e.id);
                if (defN == null) continue;
                var def = defN.Value;
                Row(list, idx, out var row);

                var name = UiKit.Text("name", row, def.description, 30, TextAnchor.UpperLeft);
                UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(560, 40), new Vector2(30, -16));
                var prog = UiKit.Text("prog", row, $"{Mathf.Min(e.progress, def.target)}/{def.target}   +{def.rewardCores} 코어", 24, TextAnchor.UpperLeft);
                UiKit.Place(prog.rectTransform, new Vector2(0f, 1f), new Vector2(560, 40), new Vector2(30, -64));

                string id = e.id;
                bool claimed = e.claimed;
                bool canClaim = _quests.CanClaim(id);
                var btn = UiKit.Button("claim", row, claimed ? "완료" : (canClaim ? "수령" : "진행중"), 28, out _);
                UiKit.Place((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(240, 110), new Vector2(-130, 0));
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

                var name = UiKit.Text("name", row, p.title, 32, TextAnchor.UpperLeft);
                UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(520, 40), new Vector2(30, -18));
                string sub = p.kind switch
                {
                    IapKind.CoreBundle => $"코어 +{p.cores}",
                    IapKind.Character => "캐릭터 즉시 해금",
                    IapKind.RemoveAds => "리워드 외 광고 제거",
                    IapKind.StarterPack => $"코어 +{p.cores}, 골드 +{p.gold}, 광고 제거",
                    _ => ""
                };
                var desc = UiKit.Text("desc", row, sub, 22, TextAnchor.UpperLeft);
                UiKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(520, 40), new Vector2(30, -62));

                bool owned = (p.kind == IapKind.RemoveAds && _meta.State.adsRemoved)
                             || (p.kind == IapKind.Character && _meta.IsCharacterUnlocked(p.character))
                             || _meta.State.ownedProducts.Contains(p.id) && p.kind != IapKind.CoreBundle;

                var id = p.id;
                var btn = UiKit.Button("buy", row, owned ? "보유" : p.priceText, 28, out _);
                UiKit.Place((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(280, 110), new Vector2(-150, 0));
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

            var body = UiKit.Text("body", list, _account != null && _account.IsLinked
                    ? $"연결됨: {_account.DisplayName}"
                    : "게스트 모드에서는 데이터가 이 기기에만 저장됩니다.\n계정을 연결하면 클라우드에 백업되어\n앱을 지우거나 기기를 바꿔도 이어집니다.",
                26, TextAnchor.UpperLeft);
            UiKit.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(860, 240), new Vector2(0, -60));

            if (_account == null || !_account.IsLinked)
            {
                var g = UiKit.Button("google", list, "Google로 연결", 30, out _);
                UiKit.Place((RectTransform)g.transform, new Vector2(0.5f, 1f), new Vector2(700, 120), new Vector2(0, -320));
                g.onClick.AddListener(() => _account.LinkWithProvider("google", _ => { OpenAccount(false); RefreshHome(); }));

                var ap = UiKit.Button("apple", list, "Apple로 연결", 30, out _);
                UiKit.Place((RectTransform)ap.transform, new Vector2(0.5f, 1f), new Vector2(700, 120), new Vector2(0, -460));
                ap.onClick.AddListener(() => _account.LinkWithProvider("apple", _ => { OpenAccount(false); RefreshHome(); }));

                var em = UiKit.Button("email", list, "이메일로 연결", 30, out _);
                UiKit.Place((RectTransform)em.transform, new Vector2(0.5f, 1f), new Vector2(700, 120), new Vector2(0, -600));
                em.onClick.AddListener(() => _account.LinkWithEmail($"player{_meta.State.totalRuns}@example.com", _ => { OpenAccount(false); RefreshHome(); }));

                var note = UiKit.Text("note", list, "* 실제 로그인 연동은 백엔드 연결 후 활성화됩니다 (docs/EXTERNAL_SETUP.md).", 18, TextAnchor.UpperLeft);
                UiKit.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(860, 60), new Vector2(0, -740));
            }
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
            var langTxt = UiKit.Text("t", langRow, "언어 / Language", 30, TextAnchor.UpperLeft);
            UiKit.Place(langTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(520, 60), new Vector2(30, 0));
            var langBtn = UiKit.Button("b", langRow, Loc.Current == Lang.Ko ? "한국어" : "English", 28, out _);
            UiKit.Place((RectTransform)langBtn.transform, new Vector2(1f, 0.5f), new Vector2(240, 100), new Vector2(-130, 0));
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
            var accTxt = UiKit.Text("t", accRow, _account != null && _account.IsLinked ? $"계정: {_account.DisplayName}" : "계정: 게스트", 30, TextAnchor.UpperLeft);
            UiKit.Place(accTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(520, 60), new Vector2(30, 0));
            var accBtn = UiKit.Button("b", accRow, _account != null && _account.IsLinked ? "연결됨" : "연결", 26, out _);
            UiKit.Place((RectTransform)accBtn.transform, new Vector2(1f, 0.5f), new Vector2(240, 100), new Vector2(-130, 0));
            accBtn.interactable = _account == null || !_account.IsLinked;
            accBtn.onClick.AddListener(() => OpenAccount(false));
            r++;

            Row(list, r, out var delRow);
            var delTxt = UiKit.Text("t", delRow, "저장 데이터 삭제", 30, TextAnchor.UpperLeft);
            UiKit.Place(delTxt.rectTransform, new Vector2(0f, 0.5f), new Vector2(520, 60), new Vector2(30, 0));
            var delBtn = UiKit.Button("b", delRow, "삭제", 26, out _);
            UiKit.Place((RectTransform)delBtn.transform, new Vector2(1f, 0.5f), new Vector2(240, 100), new Vector2(-130, 0));
            delBtn.onClick.AddListener(() =>
            {
                if (ServiceLocator.TryGet<ISaveService>(out var sv)) sv.Delete();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }

        void SettingToggleRow(RectTransform list, int index, string label, Func<bool> get, Action<bool> set)
        {
            Row(list, index, out var row);
            var t = UiKit.Text("t", row, label, 30, TextAnchor.UpperLeft);
            UiKit.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(520, 60), new Vector2(30, 0));
            var btn = UiKit.Button("b", row, get() ? "ON" : "OFF", 28, out var bl);
            UiKit.Place((RectTransform)btn.transform, new Vector2(1f, 0.5f), new Vector2(220, 100), new Vector2(-130, 0));
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
