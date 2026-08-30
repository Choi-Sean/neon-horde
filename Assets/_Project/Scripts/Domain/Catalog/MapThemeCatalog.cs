using UnityEngine;

namespace NeonHorde
{
    public enum MapThemeId { Grid = 0, Void = 1, Furnace = 2, Cryo = 3 }

    public enum TerrainStyle { OpenPillars, FloatingIslands, TightRubble, IceFields }

    public struct MapThemeDef
    {
        public MapThemeId id;
        public string name;
        public Color background;
        public Color gridLine;
        public Color terrainColor;
        public TerrainStyle style;
        public float blockerDensity;    // fraction of cells that become blockers/impassable
        public float crateDensity;      // destructible cover
        public float slowDensity;       // mud/ice zones
        public float hazardDensity;     // damage zones
        public int difficultyTier;      // higher = weighted for later survival time
    }

    public struct MapModifier
    {
        public string id;
        public string description;
        public float enemySpeedMul;
        public float enemyHpMul;
        public float goldMul;
        public float eliteRateMul;
        public float cooldownMul;
        public float terrainDensityMul;
    }

    public static class MapThemeCatalog
    {
        public static readonly MapThemeDef[] Themes =
        {
            new MapThemeDef
            {
                id = MapThemeId.Grid, name = "GRID",
                background = new Color(0.02f, 0.02f, 0.035f, 1f),
                gridLine = new Color(0f, 0.55f, 0.85f, 1f) * 1.6f,
                terrainColor = new Color(0.2f, 1.1f, 1.8f, 1f),
                style = TerrainStyle.OpenPillars,
                blockerDensity = 0.020f, crateDensity = 0.010f, slowDensity = 0f, hazardDensity = 0f,
                difficultyTier = 0
            },
            new MapThemeDef
            {
                id = MapThemeId.Void, name = "VOID",
                background = new Color(0.015f, 0.01f, 0.03f, 1f),
                gridLine = new Color(0.4f, 0.2f, 0.7f, 1f) * 1.2f,
                terrainColor = new Color(1.4f, 0.5f, 2.2f, 1f),
                style = TerrainStyle.FloatingIslands,
                blockerDensity = 0.055f, crateDensity = 0.006f, slowDensity = 0f, hazardDensity = 0f,
                difficultyTier = 1
            },
            new MapThemeDef
            {
                id = MapThemeId.Furnace, name = "FURNACE",
                background = new Color(0.05f, 0.015f, 0.01f, 1f),
                gridLine = new Color(0.9f, 0.3f, 0.1f, 1f) * 1.4f,
                terrainColor = new Color(2.4f, 0.7f, 0.2f, 1f),
                style = TerrainStyle.TightRubble,
                blockerDensity = 0.045f, crateDensity = 0.05f, slowDensity = 0f, hazardDensity = 0.02f,
                difficultyTier = 2
            },
            new MapThemeDef
            {
                id = MapThemeId.Cryo, name = "CRYO",
                background = new Color(0.02f, 0.03f, 0.05f, 1f),
                gridLine = new Color(0.3f, 0.7f, 0.95f, 1f) * 1.4f,
                terrainColor = new Color(0.6f, 1.8f, 2.4f, 1f),
                style = TerrainStyle.IceFields,
                blockerDensity = 0.02f, crateDensity = 0.01f, slowDensity = 0.10f, hazardDensity = 0.015f,
                difficultyTier = 2
            },
        };

        public static MapThemeDef Get(MapThemeId id) => Themes[(int)id];

        public static readonly MapModifier[] Modifiers =
        {
            new MapModifier { id = "swift",   description = "적 이동속도 +15%, 골드 +20%", enemySpeedMul = 1.15f, enemyHpMul = 1f, goldMul = 1.2f, eliteRateMul = 1f, cooldownMul = 1f, terrainDensityMul = 1f },
            new MapModifier { id = "elite",   description = "엘리트 2배 출현",             enemySpeedMul = 1f, enemyHpMul = 1f, goldMul = 1f, eliteRateMul = 2f, cooldownMul = 1f, terrainDensityMul = 1f },
            new MapModifier { id = "dense",   description = "지형 밀도 ↑",                 enemySpeedMul = 1f, enemyHpMul = 1f, goldMul = 1f, eliteRateMul = 1f, cooldownMul = 1f, terrainDensityMul = 1.8f },
            new MapModifier { id = "brutal",  description = "무기 쿨다운 −10%, 적 HP +20%", enemySpeedMul = 1f, enemyHpMul = 1.2f, goldMul = 1f, eliteRateMul = 1f, cooldownMul = 0.9f, terrainDensityMul = 1f },
            new MapModifier { id = "frenzy",  description = "적 스폰 +25%, 골드 +15%",      enemySpeedMul = 1f, enemyHpMul = 1f, goldMul = 1.15f, eliteRateMul = 1f, cooldownMul = 1f, terrainDensityMul = 1f },
        };
    }
}
