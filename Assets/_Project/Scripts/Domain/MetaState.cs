using System;
using System.Collections.Generic;

namespace NeonHorde
{
    /// <summary>
    /// Persistent player profile. Plain serializable class so Unity's JsonUtility can
    /// round-trip it. Grows with permanent upgrades / unlocks in M3.
    /// </summary>
    [Serializable]
    public class MetaState
    {
        public int version = 2;
        public long gold;
        public int cores;                       // character-unlock currency (quests / IAP)
        public int bestTimeSec;
        public int totalRuns;
        public int totalKills;
        public int bossKills;

        public List<string> unlockedCharacters = new() { "Pulse" };
        public string selectedCharacter = "Pulse";
        public List<string> ownedProducts = new();   // IAP product ids
        public bool adsRemoved;

        // account (guest by default; link = Unity Authentication + Cloud Save later)
        public bool accountLinked;
        public string accountId = "";
        public string accountName = "";
        public bool signupNudgeShown;

        public List<PowerEntry> powerLevels = new();       // permanent upgrades
        public List<QuestEntry> dailyQuests = new();
        public string dailyDateIso = "";
        public List<QuestEntry> milestoneQuests = new();

        public Settings settings = new();

        [Serializable] public class PowerEntry { public string id; public int level; }
        [Serializable] public class QuestEntry { public string id; public int progress; public bool claimed; }

        [Serializable]
        public class Settings
        {
            public float bgm = 1f;
            public float sfx = 1f;
            public bool haptics = true;
            public int quality = 1;
            public int language;   // 0 = Ko, 1 = En
        }

        public static MetaState CreateDefault() => new();
    }
}
