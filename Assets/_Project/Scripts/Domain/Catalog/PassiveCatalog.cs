namespace NeonHorde
{
    public enum PassiveId
    {
        Might,            // +damage %
        Cooldown,         // -cooldown %
        Area,             // +area %
        ProjectileCount,  // +1 projectile
        Duration,         // +duration %
        ProjectileSpeed,  // +projectile speed %
        MoveSpeed,        // +move speed %
        MaxHp,            // +max hp %
        Regen,            // +hp/sec
        Armor,            // flat damage reduction
        Magnet,           // +pickup radius %
        Xp,               // +xp gain %
        // M4
        Damage,           // stronger Might tier used for Bolt evolution gate
        Luck,             // +chest / crit chance
        Greed             // +gold, +enemies (curse-lite)
    }

    public struct PassiveDef
    {
        public PassiveId id;
        public string name;
        public float perLevel;   // meaning depends on id
        public int maxLevel;
    }

    public static class PassiveCatalog
    {
        static readonly PassiveDef[] Defs =
        {
            P(PassiveId.Might,           "화력",       0.10f, 8),
            P(PassiveId.Cooldown,        "속사",       0.08f, 8),
            P(PassiveId.Area,            "범위",       0.12f, 8),
            P(PassiveId.ProjectileCount, "다발",       1f,    4),
            P(PassiveId.Duration,        "지속",       0.15f, 6),
            P(PassiveId.ProjectileSpeed, "탄속",       0.12f, 6),
            P(PassiveId.MoveSpeed,       "기동",       0.08f, 8),
            P(PassiveId.MaxHp,           "체력",       0.12f, 8),
            P(PassiveId.Regen,           "재생",       0.4f,  6),
            P(PassiveId.Armor,           "방어",       1f,    6),
            P(PassiveId.Magnet,          "자력",       0.20f, 6),
            P(PassiveId.Xp,              "지식",       0.10f, 6),
            P(PassiveId.Damage,          "폭발력",     0.14f, 6),
            P(PassiveId.Luck,            "행운",       0.08f, 6),
            P(PassiveId.Greed,           "탐욕",       0.15f, 5),
        };

        static PassiveDef P(PassiveId id, string name, float perLevel, int maxLevel)
            => new PassiveDef { id = id, name = name, perLevel = perLevel, maxLevel = maxLevel };

        public static PassiveDef Get(PassiveId id) => Defs[(int)id];
        public static int BasePassiveCount => 12; // Might..Xp offered as normal drops
    }
}
