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

        Mesh _quad;
        Material _matBlocker, _matCrate, _matSlow, _matHazard;
        Matrix4x4[] _blockerM, _crateM, _slowM, _hazardM;
        int _blockerN, _crateN, _slowN, _hazardN;

        void Awake()
        {
            _quad = NeonMesh.Quad;
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
            BuildRenderBuffers();
        }

        void ApplyTheme(MapThemeDef t)
        {
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = t.background;
            _matBlocker = NeonMesh.NewUnlit(t.terrainColor * 0.5f);
            _matCrate = NeonMesh.NewUnlit(t.terrainColor);
            _matSlow = NeonMesh.NewUnlit(new Color(0.4f, 0.7f, 1.4f, 0.5f));
            _matHazard = NeonMesh.NewUnlit(new Color(2.4f, 0.4f, 0.2f, 0.7f));
        }

        void BuildRenderBuffers()
        {
            int blockers = 0, crates = 0, slows = 0, hazards = 0;
            foreach (var p in Plan.terrain)
                switch (p.kind)
                {
                    case TerrainKind.Blocker: blockers++; break;
                    case TerrainKind.Crate: crates++; break;
                    case TerrainKind.Slow: slows++; break;
                    case TerrainKind.Hazard: hazards++; break;
                }

            _blockerM = new Matrix4x4[Mathf.Max(1, blockers)];
            _crateM = new Matrix4x4[Mathf.Max(1, crates)];
            _slowM = new Matrix4x4[Mathf.Max(1, slows)];
            _hazardM = new Matrix4x4[Mathf.Max(1, hazards)];
            _crates.Clear();

            _blockerN = _crateN = _slowN = _hazardN = 0;
            foreach (var p in Plan.terrain)
            {
                var m = Matrix4x4.TRS(new Vector3(p.center.x, p.center.y, 0.5f), Quaternion.identity,
                    new Vector3(p.size.x * 0.92f, p.size.y * 0.92f, 1f));
                switch (p.kind)
                {
                    case TerrainKind.Blocker: _blockerM[_blockerN++] = m; break;
                    case TerrainKind.Crate: _crateM[_crateN++] = m; _crates.Add(p); break;
                    case TerrainKind.Slow: _slowM[_slowN++] = m; break;
                    case TerrainKind.Hazard: _hazardM[_hazardN++] = m; break;
                }
            }
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
                    RebuildCrateBuffer();
                    return true;
                }
                _crates[i] = c;
                return false;
            }
            return false;
        }

        void RebuildCrateBuffer()
        {
            _crateN = 0;
            foreach (var c in _crates)
                _crateM[_crateN++] = Matrix4x4.TRS(new Vector3(c.center.x, c.center.y, 0.5f),
                    Quaternion.identity, new Vector3(c.size.x * 0.92f, c.size.y * 0.92f, 1f));
        }

        void LateUpdate()
        {
            if (Plan == null) return;
            NeonMesh.RenderInstanced(_matBlocker, _quad, _blockerM, _blockerN);
            NeonMesh.RenderInstanced(_matCrate, _quad, _crateM, _crateN);
            NeonMesh.RenderInstanced(_matSlow, _quad, _slowM, _slowN);
            NeonMesh.RenderInstanced(_matHazard, _quad, _hazardM, _hazardN);
        }
    }
}
