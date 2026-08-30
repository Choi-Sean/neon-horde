using UnityEngine;

namespace NeonHorde
{
    public enum EnemyId : byte
    {
        Walker = 0, Swarmer = 1, Tank = 2,
        Spitter = 3, Exploder = 4, Splitter = 5, Splitterling = 6,
        Phantom = 7, FrostTank = 8
    }

    public enum EnemyBehavior : byte { Seek, Charger, Ranged, Exploder, Splitter, Phantom, FrostTank }

    public struct EnemyDef
    {
        public EnemyId id;
        public EnemyBehavior behavior;
        public Color color;
        public float radius;
        public float hpBase;
        public float hpPer10s;     // linear hp scaling per 10 run-seconds
        public float speedBase;
        public int contactDamage;
        public float xpValue;
        public float goldChance;
        public float[] themeWeights; // index by (int)MapThemeId; 0 == never spawns there
    }

    public static class EnemyCatalog
    {
        // theme order: GRID, VOID, FURNACE, CRYO
        static EnemyDef[] _defs;

        public static EnemyDef Get(EnemyId id) => _defs[(int)id];
        public static int Count => _defs.Length;

        static EnemyCatalog() => Build();

        static void Build()
        {
            _defs = new EnemyDef[System.Enum.GetValues(typeof(EnemyId)).Length];

            Set(new EnemyDef
            {
                id = EnemyId.Walker, behavior = EnemyBehavior.Seek, color = new Color(2.2f, 0.30f, 1.10f, 1f),
                radius = 0.35f, hpBase = 10f, hpPer10s = 6f, speedBase = 2.2f, contactDamage = 6, xpValue = 1f,
                goldChance = 0.05f, themeWeights = new[] { 1.0f, 0.8f, 0.5f, 0.6f }
            });
            Set(new EnemyDef
            {
                id = EnemyId.Swarmer, behavior = EnemyBehavior.Seek, color = new Color(2.4f, 1.40f, 0.20f, 1f),
                radius = 0.26f, hpBase = 5f, hpPer10s = 3f, speedBase = 3.6f, contactDamage = 4, xpValue = 1f,
                goldChance = 0.03f, themeWeights = new[] { 0.8f, 0.4f, 0.3f, 1.0f }
            });
            Set(new EnemyDef
            {
                id = EnemyId.Tank, behavior = EnemyBehavior.Seek, color = new Color(0.4f, 0.90f, 2.40f, 1f),
                radius = 0.60f, hpBase = 45f, hpPer10s = 26f, speedBase = 1.4f, contactDamage = 12, xpValue = 4f,
                goldChance = 0.15f, themeWeights = new[] { 0.5f, 0.2f, 1.0f, 0.3f }
            });
            Set(new EnemyDef
            {
                id = EnemyId.Spitter, behavior = EnemyBehavior.Ranged, color = new Color(0.9f, 2.2f, 0.5f, 1f),
                radius = 0.34f, hpBase = 14f, hpPer10s = 8f, speedBase = 1.7f, contactDamage = 6, xpValue = 3f,
                goldChance = 0.1f, themeWeights = new[] { 0f, 0.3f, 1.0f, 0.8f }
            });
            Set(new EnemyDef
            {
                id = EnemyId.Exploder, behavior = EnemyBehavior.Exploder, color = new Color(2.6f, 0.6f, 0.2f, 1f),
                radius = 0.38f, hpBase = 12f, hpPer10s = 7f, speedBase = 2.6f, contactDamage = 20, xpValue = 3f,
                goldChance = 0.08f, themeWeights = new[] { 0f, 0f, 1.0f, 0f }
            });
            Set(new EnemyDef
            {
                id = EnemyId.Splitter, behavior = EnemyBehavior.Splitter, color = new Color(1.6f, 0.5f, 2.4f, 1f),
                radius = 0.44f, hpBase = 18f, hpPer10s = 10f, speedBase = 2.0f, contactDamage = 8, xpValue = 3f,
                goldChance = 0.1f, themeWeights = new[] { 0f, 1.0f, 0f, 0f }
            });
            Set(new EnemyDef
            {
                id = EnemyId.Splitterling, behavior = EnemyBehavior.Seek, color = new Color(1.4f, 0.6f, 2.0f, 1f),
                radius = 0.22f, hpBase = 5f, hpPer10s = 3f, speedBase = 3.2f, contactDamage = 5, xpValue = 1f,
                goldChance = 0.02f, themeWeights = new[] { 0f, 0f, 0f, 0f } // only spawned by Splitter death
            });
            Set(new EnemyDef
            {
                id = EnemyId.Phantom, behavior = EnemyBehavior.Phantom, color = new Color(0.7f, 1.6f, 2.2f, 0.85f),
                radius = 0.30f, hpBase = 7f, hpPer10s = 4f, speedBase = 4.4f, contactDamage = 7, xpValue = 2f,
                goldChance = 0.05f, themeWeights = new[] { 0f, 1.0f, 0f, 0f }
            });
            Set(new EnemyDef
            {
                id = EnemyId.FrostTank, behavior = EnemyBehavior.FrostTank, color = new Color(0.6f, 1.8f, 2.4f, 1f),
                radius = 0.62f, hpBase = 50f, hpPer10s = 28f, speedBase = 1.3f, contactDamage = 12, xpValue = 5f,
                goldChance = 0.18f, themeWeights = new[] { 0f, 0f, 0f, 1.0f }
            });
        }

        static void Set(EnemyDef d) => _defs[(int)d.id] = d;
    }
}
