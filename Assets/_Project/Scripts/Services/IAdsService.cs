using System;

namespace NeonHorde
{
    public enum AdPlacement { Revive, DoubleGold, Reroll, ChestBonus }

    public interface IAdsService
    {
        bool IsRewardedReady(AdPlacement placement);
        /// <summary>onComplete(true) = watched to reward. onComplete(false) = skipped / no fill / error.</summary>
        void ShowRewarded(AdPlacement placement, Action<bool> onComplete);
        void Preload();
    }

    /// <summary>
    /// Editor / pre-integration stub. Grants the reward immediately.
    /// Replace with a LevelPlay (ironSource) implementation once the account + app id
    /// + ad-unit ids are available — see docs/EXTERNAL_SETUP.md.
    /// </summary>
    public sealed class StubAdsService : IAdsService
    {
        public bool IsRewardedReady(AdPlacement placement) => true;

        public void ShowRewarded(AdPlacement placement, Action<bool> onComplete)
        {
            UnityEngine.Debug.Log($"[Ads:STUB] rewarded {placement} -> granted");
            ServiceLocator.TryGet<IAnalyticsService>(out var a);
            a?.Log("ad_impression", ("placement", placement.ToString()), ("result", "granted_stub"));
            onComplete?.Invoke(true);
        }

        public void Preload() { }
    }
}
