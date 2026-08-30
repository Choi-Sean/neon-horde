namespace NeonHorde
{
    public enum PowerId
    {
        StartHp, Damage, MoveSpeed, Magnet, GoldGain, Revive, XpGain, Armor
    }

    public struct PowerDef
    {
        public PowerId id;
        public string name;
        public string desc;
        public int maxLevel;
        public int baseCost;
        public float costGrowth;
        public float perLevel;   // effect meaning depends on id

        public long CostForLevel(int currentLevel)
        {
            double c = baseCost;
            for (int i = 0; i < currentLevel; i++) c *= costGrowth;
            return (long)c;
        }
    }

    /// <summary>Permanent gold-bought upgrades applied to every run.</summary>
    public static class PowerCatalog
    {
        public static readonly PowerDef[] All =
        {
            P(PowerId.StartHp,   "시작 체력",   "최대 HP +8%/레벨",     8, 50,  1.6f, 0.08f),
            P(PowerId.Damage,    "화력 코어",   "데미지 +5%/레벨",       8, 80,  1.7f, 0.05f),
            P(PowerId.MoveSpeed, "부스터",     "이동속도 +3%/레벨",     6, 70,  1.7f, 0.03f),
            P(PowerId.Magnet,    "자력 코일",   "자석 범위 +10%/레벨",   6, 60,  1.6f, 0.10f),
            P(PowerId.GoldGain,  "탐지기",     "골드 획득 +8%/레벨",    6, 100, 1.8f, 0.08f),
            P(PowerId.Revive,    "예비 코어",   "부활 +1 (최대 2)",      2, 400, 3.0f, 1f),
            P(PowerId.XpGain,    "학습 모듈",   "경험치 +6%/레벨",       6, 90,  1.7f, 0.06f),
            P(PowerId.Armor,     "장갑",       "받는 피해 -1/레벨",      5, 120, 1.9f, 1f),
        };

        static PowerDef P(PowerId id, string name, string desc, int max, int cost, float growth, float per)
            => new PowerDef { id = id, name = name, desc = desc, maxLevel = max, baseCost = cost, costGrowth = growth, perLevel = per };

        public static PowerDef Get(PowerId id) => All[(int)id];
    }
}
