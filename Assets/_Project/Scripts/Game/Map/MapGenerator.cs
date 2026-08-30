using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Seed-driven finite arena generator. Scatters blockers / crates / slow / hazard
    /// zones per theme, keeps the spawn point clear, prunes unreachable pockets, and
    /// builds a weighted enemy roster.
    /// </summary>
    public static class MapGenerator
    {
        public static MapPlan Generate(int seed, MapThemeId themeId, MapModifier[] mods)
        {
            var rng = new SeededRng((ulong)seed * 2654435761u + 1u);
            MapThemeDef theme = MapThemeCatalog.Get(themeId);

            float densityMul = 1f, goldMul = 1f, speedMul = 1f, hpMul = 1f, eliteMul = 1f, cdMul = 1f;
            if (mods != null)
                foreach (var m in mods)
                {
                    densityMul *= m.terrainDensityMul <= 0 ? 1f : m.terrainDensityMul;
                    goldMul *= m.goldMul <= 0 ? 1f : m.goldMul;
                    speedMul *= m.enemySpeedMul <= 0 ? 1f : m.enemySpeedMul;
                    hpMul *= m.enemyHpMul <= 0 ? 1f : m.enemyHpMul;
                    eliteMul *= m.eliteRateMul <= 0 ? 1f : m.eliteRateMul;
                    cdMul *= m.cooldownMul <= 0 ? 1f : m.cooldownMul;
                }

            int size = 80 + rng.NextInt(0, 31);          // square arena, 80..110
            const float cell = 1f;
            var origin = new Vector2(-size * 0.5f, -size * 0.5f);
            var grid = new NavGrid(size, size, cell, origin);

            var plan = new MapPlan
            {
                themeId = themeId,
                modifiers = mods,
                arenaSize = new Vector2(size, size),
                arenaMin = origin + new Vector2(2f, 2f),
                arenaMax = origin + new Vector2(size - 2f, size - 2f),
                navGrid = grid,
                goldMul = goldMul, enemySpeedMul = speedMul, enemyHpMul = hpMul,
                eliteRateMul = eliteMul, cooldownMul = cdMul
            };

            // border walls (2 cells thick)
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (x < 2 || y < 2 || x >= size - 2 || y >= size - 2)
                    grid.SetFlag(x, y, NavGrid.Flag.Blocked, true);

            int cx = size / 2, cy = size / 2;
            const int safeR = 7;

            Scatter(grid, plan, rng, theme.blockerDensity * densityMul, cx, cy, safeR, NavGrid.Flag.Blocked, TerrainKind.Blocker, theme, 1, 4);
            Scatter(grid, plan, rng, theme.crateDensity * densityMul, cx, cy, safeR, NavGrid.Flag.Blocked, TerrainKind.Crate, theme, 1, 1);
            Scatter(grid, plan, rng, theme.slowDensity, cx, cy, safeR, NavGrid.Flag.Slow, TerrainKind.Slow, theme, 2, 6);
            Scatter(grid, plan, rng, theme.hazardDensity, cx, cy, safeR, NavGrid.Flag.Hazard, TerrainKind.Hazard, theme, 1, 4);

            PruneUnreachable(grid, cx, cy);
            BuildRoster(plan, themeId);
            return plan;
        }

        static void Scatter(NavGrid grid, MapPlan plan, SeededRng rng, float density,
                            int cx, int cy, int safeR, NavGrid.Flag flag, TerrainKind kind,
                            MapThemeDef theme, int blobMin, int blobMax)
        {
            if (density <= 0f) return;
            int size = grid.width;
            for (int x = 2; x < size - 2; x++)
            for (int y = 2; y < size - 2; y++)
            {
                if (Mathf.Abs(x - cx) < safeR && Mathf.Abs(y - cy) < safeR) continue;
                if (!rng.Chance(density)) continue;

                int blob = rng.NextInt(blobMin, blobMax + 1);
                int px = x, py = y;
                for (int b = 0; b < blob; b++)
                {
                    if (grid.InBounds(px, py))
                    {
                        grid.SetFlag(px, py, flag, true);
                        if (kind == TerrainKind.Crate || kind == TerrainKind.Blocker)
                        {
                            plan.terrain.Add(new TerrainPiece
                            {
                                kind = kind,
                                center = grid.CellCenter(px, py),
                                size = Vector2.one * grid.cellSize,
                                hp = kind == TerrainKind.Crate ? 12f : 0f,
                                cellX = px, cellY = py
                            });
                        }
                    }
                    px += rng.NextInt(-1, 2);
                    py += rng.NextInt(-1, 2);
                }
                // slow/hazard: record one piece for the blob for rendering
                if (kind == TerrainKind.Slow || kind == TerrainKind.Hazard)
                {
                    plan.terrain.Add(new TerrainPiece
                    {
                        kind = kind,
                        center = grid.CellCenter(x, y),
                        size = Vector2.one * (grid.cellSize * blob),
                        hp = 0f, cellX = x, cellY = y
                    });
                }
            }
        }

        static void PruneUnreachable(NavGrid grid, int startX, int startY)
        {
            int w = grid.width, h = grid.height, n = w * h;
            var seen = new bool[n];
            var stack = new Stack<int>();
            int s = grid.Index(startX, startY);
            seen[s] = true;
            stack.Push(s);
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            while (stack.Count > 0)
            {
                int c = stack.Pop();
                int cx = c % w, cy = c / w;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d], ny = cy + dy[d];
                    if (!grid.InBounds(nx, ny) || grid.IsBlocked(nx, ny)) continue;
                    int ni = ny * w + nx;
                    if (seen[ni]) continue;
                    seen[ni] = true;
                    stack.Push(ni);
                }
            }
            for (int i = 0; i < n; i++)
                if (!seen[i] && (grid.flags[i] & (byte)NavGrid.Flag.Blocked) == 0)
                    grid.flags[i] |= (byte)NavGrid.Flag.Blocked;
        }

        static void BuildRoster(MapPlan plan, MapThemeId themeId)
        {
            int t = (int)themeId;
            var ids = new List<EnemyId>();
            var cum = new List<float>();
            float acc = 0f;
            for (int i = 0; i < EnemyCatalog.Count; i++)
            {
                var def = EnemyCatalog.Get((EnemyId)i);
                if (def.themeWeights == null || t >= def.themeWeights.Length) continue;
                float w = def.themeWeights[t];
                if (w <= 0f) continue;
                acc += w;
                ids.Add((EnemyId)i);
                cum.Add(acc);
            }
            if (ids.Count == 0) { ids.Add(EnemyId.Walker); cum.Add(1f); }
            plan.enemyIds = ids.ToArray();
            plan.enemyCumWeights = cum.ToArray();
        }

        public static MapThemeId RollTheme(SeededRng rng, float survivalBias01)
        {
            // early runs favour tier 0; later runs weight higher tiers
            var weights = new float[MapThemeCatalog.Themes.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                int tier = MapThemeCatalog.Themes[i].difficultyTier;
                weights[i] = Mathf.Lerp(tier == 0 ? 1f : 0.35f, tier == 0 ? 0.4f : 1f, survivalBias01);
            }
            float total = 0f;
            foreach (var w in weights) total += w;
            float r = rng.NextFloat() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                r -= weights[i];
                if (r <= 0f) return (MapThemeId)i;
            }
            return MapThemeId.Grid;
        }

        public static MapModifier[] RollModifiers(SeededRng rng)
        {
            int count = rng.NextInt(1, 3);
            var pool = new List<MapModifier>(MapThemeCatalog.Modifiers);
            var picked = new List<MapModifier>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = rng.NextInt(0, pool.Count);
                picked.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return picked.ToArray();
        }
    }
}
