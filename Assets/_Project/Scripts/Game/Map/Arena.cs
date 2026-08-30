using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Runtime owner of the current MapPlan. Generates the arena on run start, renders
    /// terrain (instanced), applies theme colours, and handles crate destruction.
    /// </summary>
    public sealed class Arena : MonoBehaviour
    {
        public MapPlan Plan { get; private set; }
        public NavGrid Grid => Plan?.navGrid;

        readonly List<TerrainPiece> _crates = new List<TerrainPiece>();

        SpritePool _wallPool;   // blockers + crates, above the ground
        SpritePool _zonePool;   // slow / hazard fields, on the ground
        Sprite _sprBlocker, _sprCrate, _sprSlow, _sprHazard;
        Color _colBlocker, _colCrate;
        static readonly Color ColSlow = new Color(0.30f, 0.55f, 1.1f, 0.9f);
        static readonly Color ColHazard = new Color(2.6f, 0.5f, 0.18f, 1f);

        void Awake()
        {
            _sprBlocker = NeonArt.Sprite(NeonShapeKind.Square, 96, 0.30f, 0.55f);
            _sprCrate = NeonArt.Sprite(NeonShapeKind.Ring, 96, 0.35f, 0.30f);
            _sprSlow = NeonArt.GlowSprite(96, 3f);
            _sprHazard = NeonArt.Sprite(NeonShapeKind.Spark, 96, 0.55f, 0.4f);
            _wallPool = new SpritePool("TerrainWalls", 8, transform);
            _zonePool = new SpritePool("TerrainZones", -20, transform);
        }

        public void Build(int seed)
        {
            var seedRng = new SeededRng((ulong)(uint)seed);
            int best = GameBootstrap.Meta != null ? GameBootstrap.Meta.bestTimeSec : 0;
            float bias = Mathf.Clamp01(best / 1200f);
            MapThemeId theme = MapGenerator.RollTheme(seedRng, bias);
            MapModifier[] mods = MapGenerator.RollModifiers(seedRng);

            Plan = MapGenerator.Generate(seed, theme, mods);
            ApplyTheme(MapThemeCatalog.Get(theme));

            _crates.Clear();
            foreach (var p in Plan.terrain)
                if (p.kind == TerrainKind.Crate) _crates.Add(p);
        }

        void ApplyTheme(MapThemeDef t)
        {
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = t.background;
            _colBlocker = t.terrainColor * 0.85f; _colBlocker.a = 1f;
            _colCrate = t.terrainColor * 1.3f; _colCrate.a = 1f;
        }

        /// <summary>Damage the nearest crate within radius; returns true if one broke.</summary>
        public bool DamageCrate(Vector2 pos, float radius, float dmg)
        {
            for (int i = 0; i < _crates.Count; i++)
            {
                var c = _crates[i];
                if ((c.center - pos).sqrMagnitude > (radius + 0.6f) * (radius + 0.6f)) continue;
                c.hp -= dmg;
                if (c.hp <= 0f)
                {
                    Plan.navGrid.SetFlag(c.cellX, c.cellY, NavGrid.Flag.Blocked, false);
                    _crates.RemoveAt(i);
                    return true;
                }
                _crates[i] = c;
                return false;
            }
            return false;
        }

        void LateUpdate()
        {
            if (Plan == null || _wallPool == null) return;

            _zonePool.Begin();
            _wallPool.Begin();
            foreach (var p in Plan.terrain)
            {
                var scale = new Vector2(p.size.x * 0.92f, p.size.y * 0.92f);
                switch (p.kind)
                {
                    case TerrainKind.Blocker:
                        _wallPool.Draw(_sprBlocker, p.center, 0.5f, _colBlocker, scale);
                        break;
                    case TerrainKind.Slow:
                        _zonePool.Draw(_sprSlow, p.center, 0.6f, ColSlow, scale);
                        break;
                    case TerrainKind.Hazard:
                        _zonePool.Draw(_sprHazard, p.center, 0.6f, ColHazard, scale);
                        break;
                }
            }
            foreach (var c in _crates)
                _wallPool.Draw(_sprCrate, c.center, 0.5f, _colCrate,
                    new Vector2(c.size.x * 0.92f, c.size.y * 0.92f));

            _zonePool.End();
            _wallPool.End();
        }
    }
}
