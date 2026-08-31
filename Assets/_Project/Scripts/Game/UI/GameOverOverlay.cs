using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>
    /// Two states: a death prompt (ad-revive / see results) and the results panel
    /// (stats + retry / menu / watch-ad-for-2x-gold).
    /// </summary>
    public sealed class GameOverOverlay : MonoBehaviour
    {
        public GameObject panel;
        public GameObject promptGroup;
        public GameObject resultGroup;

        public Button adReviveButton;
        public Button seeResultButton;

        public Button retryButton;
        public Button menuButton;
        public Button doubleGoldButton;
        public Text resultText;
        public Text titleText;

        void Start()
        {
            if (panel != null) panel.SetActive(false);
            var rm = RunManager.Instance;
            if (rm != null)
            {
                rm.OnDeathPrompt += ShowPrompt;
                rm.OnGameOver += ShowResult;
            }

            if (adReviveButton != null) adReviveButton.onClick.AddListener(OnAdRevive);
            if (seeResultButton != null) seeResultButton.onClick.AddListener(() => RunManager.Instance.EndRun(false));
            if (retryButton != null) retryButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            if (menuButton != null) menuButton.onClick.AddListener(GoMenu);
            if (doubleGoldButton != null) doubleGoldButton.onClick.AddListener(OnDoubleGold);
        }

        void OnDestroy()
        {
            var rm = RunManager.Instance;
            if (rm != null) { rm.OnDeathPrompt -= ShowPrompt; rm.OnGameOver -= ShowResult; }
        }

        static void GoMenu()
        {
            if (Application.CanStreamedLevelBeLoaded("MainMenu")) SceneManager.LoadScene("MainMenu");
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void ShowPrompt()
        {
            var rm = RunManager.Instance;
            bool adOk = rm.AdReviveAvailable
                        && ServiceLocator.TryGet<IAdsService>(out var ads)
                        && ads.IsRewardedReady(AdPlacement.Revive);

            if (titleText != null) titleText.text = "YOU DIED";
            if (adReviveButton != null) adReviveButton.gameObject.SetActive(adOk);
            if (promptGroup != null) promptGroup.SetActive(true);
            if (resultGroup != null) resultGroup.SetActive(false);
            if (panel != null) panel.SetActive(true);
        }

        void ShowResult()
        {
            RunState s = RunManager.Instance.State;
            int m = (int)(s.time / 60f), sec = (int)(s.time % 60f);
            if (titleText != null) titleText.text = s.victory ? "VICTORY" : "GAME OVER";
            if (resultText != null)
                resultText.text = $"SURVIVED {m:00}:{sec:00}\nLEVEL {s.level}\nKILLS {s.kills}\nGOLD +{s.goldBanked}";
            if (doubleGoldButton != null)
                doubleGoldButton.gameObject.SetActive(
                    s.goldBanked > 0 && ServiceLocator.TryGet<IAdsService>(out var ads) && ads.IsRewardedReady(AdPlacement.DoubleGold));
            if (promptGroup != null) promptGroup.SetActive(false);
            if (resultGroup != null) resultGroup.SetActive(true);
            if (panel != null) panel.SetActive(true);
        }

        void OnAdRevive()
        {
            if (!ServiceLocator.TryGet<IAdsService>(out var ads)) { RunManager.Instance.EndRun(false); return; }
            ads.ShowRewarded(AdPlacement.Revive, ok =>
            {
                if (ok) { if (panel != null) panel.SetActive(false); RunManager.Instance.AdRevive(); }
                else RunManager.Instance.EndRun(false);
            });
        }

        void OnDoubleGold()
        {
            if (!ServiceLocator.TryGet<IAdsService>(out var ads)) return;
            if (doubleGoldButton != null) doubleGoldButton.interactable = false;
            ads.ShowRewarded(AdPlacement.DoubleGold, ok =>
            {
                if (ok) RunManager.Instance.DoubleGoldReward();
                ShowResult();
            });
        }
    }
}
