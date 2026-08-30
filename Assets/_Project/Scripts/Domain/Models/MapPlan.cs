using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    public enum TerrainKind { Blocker, Crate, Slow, Hazard, Impassable }

    public struct TerrainPiece
    {
        public TerrainKind kind;
        public Vector2 center;
        public Vector2 size;
        public float hp;      // crates only
        public int cellX, cellY; // crates: cell to reopen on destruction
    }

    /// <summary>Everything a single run's arena needs: theme, modifiers, terrain, nav grid, roster.</summary>
    public sealed class MapPlan
    {
        public MapThemeId themeId;
        public MapModifier[] modifiers;
        public Vector2 arenaSize;
        public Vector2 arenaMin;     // world-space bottom-left of the play area (inside walls)
        public Vector2 arenaMax;
        public NavGrid navGrid;
        public List<TerrainPiece> terrain = new List<TerrainPiece>();

        // cumulative spawn weights over EnemyId
        public float[] enemyCumWeights;
        public EnemyId[] enemyIds;

        public float goldMul = 1f;
        public float enemySpeedMul = 1f;
        public float enemyHpMul = 1f;
        public float eliteRateMul = 1f;
        public float cooldownMul = 1f;

        public EnemyId RollEnemy(SeededRng rng)
        {
            if (enemyCumWeights == null || enemyCumWeights.Length == 0) return EnemyId.Walker;
            float total = enemyCumWeights[enemyCumWeights.Length - 1];
            float r = rng.NextFloat() * total;
            for (int i = 0; i < enemyCumWeights.Length; i++)
                if (r <= enemyCumWeights[i]) return enemyIds[i];
            return enemyIds[enemyIds.Length - 1];
        }

        public Vector2 ClampToArena(Vector2 p, float margin = 0.4f)
        {
            return new Vector2(
                Mathf.Clamp(p.x, arenaMin.x + margin, arenaMax.x - margin),
                Mathf.Clamp(p.y, arenaMin.y + margin, arenaMax.y - margin));
        }
    }
}
