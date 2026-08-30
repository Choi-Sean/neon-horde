using System;

namespace NeonHorde
{
    /// <summary>
    /// iOS ATT + GDPR/UMP consent. Stub returns "granted". Real impl uses
    /// ATTrackingStatusBinding (iOS) + the LevelPlay/UMP consent form.
    /// </summary>
    public interface IConsentService
    {
        bool Resolved { get; }
        bool PersonalisedAdsAllowed { get; }
        void Request(Action onResolved);
    }

    public sealed class StubConsentService : IConsentService
    {
        public bool Resolved { get; private set; }
        public bool PersonalisedAdsAllowed { get; private set; }

        public void Request(Action onResolved)
        {
            Resolved = true;
            PersonalisedAdsAllowed = false; // conservative default until real prompt
            UnityEngine.Debug.Log("[Consent:STUB] resolved (non-personalised)");
            onResolved?.Invoke();
        }
    }
}
