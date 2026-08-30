using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Composition root. Registers services and loads the profile before the first
    /// scene, then routes into the Boot / MainMenu flow.
    /// </summary>
    public static class GameBootstrap
    {
        public static MetaState Meta { get; private set; }
        public static MetaController MetaCtl { get; private set; }
        public static QuestService Quests { get; private set; }
        public static bool Initialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (Initialized) return;
            Initialized = true;

            var save = new JsonSaveService();
            Meta = save.Load();
            Loc.Current = (Lang)Mathf.Clamp(Meta.settings.language, 0, 1);
            ISaveService saveSvc = save;
            ServiceLocator.Register(saveSvc);

            IAnalyticsService analytics = new DebugAnalyticsService();
            ServiceLocator.Register(analytics);

            MetaCtl = new MetaController(Meta, saveSvc);
            ServiceLocator.Register(MetaCtl);

            Quests = new QuestService(MetaCtl);
            ServiceLocator.Register(Quests);

            IAudioService audio = new NullAudioService();
            audio.SetVolumes(Meta.settings.bgm, Meta.settings.sfx);
            ServiceLocator.Register(audio);

            IConsentService consent = new StubConsentService();
            ServiceLocator.Register(consent);

            IAdsService ads = new StubAdsService();
            ServiceLocator.Register(ads);

            IIapService iap = new StubIapService();
            ServiceLocator.Register(iap);

            IAccountService account = new GuestAccountService(MetaCtl);
            ServiceLocator.Register(account);

            consent.Request(() => analytics.SetConsent(true));

            analytics.Log("app_open", ("runs", Meta.totalRuns), ("linked", Meta.accountLinked));
            Debug.Log($"[NeonHorde] boot — gold={Meta.gold} cores={Meta.cores} runs={Meta.totalRuns} " +
                      $"char={MetaCtl.SelectedCharacter} linked={Meta.accountLinked} save={JsonSaveService.Path}");
        }
    }
}
