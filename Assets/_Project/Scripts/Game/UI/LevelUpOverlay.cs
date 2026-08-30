using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>
    /// Level-up / chest card panel. Options come from RunManager (UpgradeGenerator).
    /// Supports one reroll and one banish per run (M4).
    /// </summary>
    public sealed class LevelUpOverlay : MonoBehaviour
    {
        public GameObject panel;
        public Button[] buttons = new Button[3];
        public Text[] labels = new Text[3];
        public Button[] banishButtons = new Button[3];
        public Button rerollButton;
        public Text rerollLabel;
        public Text titleText;

        void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (RunManager.Instance != null) RunManager.Instance.OnLevelUp += Show;

            for (int i = 0; i < buttons.Length; i++)
            {
                int idx = i;
                if (buttons[i] != null) buttons[i].onClick.AddListener(() => Choose(idx));
                if (banishButtons[i] != null) banishButtons[i].onClick.AddListener(() => Banish(idx));
            }
            if (rerollButton != null) rerollButton.onClick.AddListener(Reroll);
        }

        void OnDestroy()
        {
            if (RunManager.Instance != null) RunManager.Instance.OnLevelUp -= Show;
        }

        void Show()
        {
            var rm = RunManager.Instance;
            var opts = rm != null ? rm.CurrentOptions : null;
            if (opts == null) return;

            if (titleText != null)
                titleText.text = rm.State.pendingChests > 0 ? "CHEST" : "LEVEL UP";

            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i < opts.Count;
                if (buttons[i] != null) buttons[i].gameObject.SetActive(on);
                if (on && labels[i] != null) labels[i].text = opts[i].title;
                if (banishButtons[i] != null)
                {
                    banishButtons[i].gameObject.SetActive(on);
                    banishButtons[i].interactable = rm.Banishes > 0;
                }
            }

            bool adReroll = rm.Rerolls == 0
                            && ServiceLocator.TryGet<IAdsService>(out var ads)
                            && ads.IsRewardedReady(AdPlacement.Reroll);
            if (rerollButton != null) rerollButton.interactable = rm.Rerolls > 0 || adReroll;
            if (rerollLabel != null) rerollLabel.text = rm.Rerolls > 0 ? $"리롤 ({rm.Rerolls})" : "리롤 (광고)";

            if (panel != null) panel.SetActive(true);
        }

        void Choose(int i)
        {
            if (panel != null) panel.SetActive(false);
            RunManager.Instance.ChooseUpgrade(i);
        }

        void Reroll()
        {
            var rm = RunManager.Instance;
            if (rm.RerollOptions()) return;
            if (!ServiceLocator.TryGet<IAdsService>(out var ads)) return;
            ads.ShowRewarded(AdPlacement.Reroll, ok =>
            {
                if (!ok) return;
                rm.State.rerolls++;
                rm.RerollOptions();
            });
        }

        void Banish(int i) => RunManager.Instance.BanishOption(i);
    }
}
