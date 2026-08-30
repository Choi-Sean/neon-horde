using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Drives every owned WeaponInstance. Each weapon ticks its cooldown and, on fire,
    /// dispatches to its behaviour (linear / aura / orbit / arc / homing / lob / chain /
    /// boomerang) applying derived stats.
    /// </summary>
    public sealed class WeaponSystem : MonoBehaviour
    {
        EnemyManager _enemies;
        ProjectileManager _projectiles;
        PlayerController _player;
        PlayerInputReader _input;
        RunManager _run;

        Vector2 _facing = Vector2.right;

        void Start()
        {
            _enemies = FindFirstObjectByType<EnemyManager>();
            _projectiles = FindFirstObjectByType<ProjectileManager>();
            _player = FindFirstObjectByType<PlayerController>();
            _input = FindFirstObjectByType<PlayerInputReader>();
            _run = RunManager.Instance;
        }

        public void SimTick(float dt)
        {
            if (_run == null || _player == null || _enemies == null || _projectiles == null) return;

            RunState st = _run.State;
            DerivedStats ds = st.stats;
            Vector2 origin = _player.transform.position;

            if (_input != null && _input.Move.sqrMagnitude > 0.01f)
                _facing = _input.Move.normalized;

            float mapCd = st.map != null ? st.map.cooldownMul : 1f;

            for (int i = 0; i < st.weapons.Count; i++)
            {
                WeaponInstance w = st.weapons[i];
                WeaponDef def = w.Def;
                WeaponLevel lv = w.Level;

                w.cooldownTimer -= dt;
                if (w.cooldownTimer > 0f) continue;

                float dmg = lv.damage * ds.damageMul;
                if (_run.Rng.Chance(ds.critChance)) dmg *= ds.critMult;
                int count = Mathf.Max(1, lv.count + ds.projectileBonus);
                float area = lv.area * ds.areaMul;
                float pspeed = lv.speed * ds.projectileSpeedMul;
                float dur = lv.duration * ds.durationMul;

                w.cooldownTimer = Mathf.Max(0.05f, lv.cooldown * ds.cooldownMul * mapCd);

                switch (def.behavior)
                {
                    case WeaponBehavior.Linear:
                        FireLinear(origin, count, pspeed, dmg, dur, lv.pierce);
                        break;
                    case WeaponBehavior.Aura:
                        _enemies.DamageAllInRadius(origin, area, dmg);
                        break;
                    case WeaponBehavior.Orbit:
                        for (int k = 0; k < count; k++)
                        {
                            float ang0 = k / (float)count * Mathf.PI * 2f;
                            _projectiles.Fire(ProjKind.Orbit, origin, Vector2.right, 0f, dmg * 3f,
                                dur, 999, Mathf.Max(1.2f, area), 220f, ang0);
                        }
                        break;
                    case WeaponBehavior.Arc:
                        for (int k = 0; k < count; k++)
                        {
                            Vector2 d = k == 0 ? _facing : -_facing;
                            _enemies.DamageArc(origin, d, area, 45f, dmg);
                        }
                        break;
                    case WeaponBehavior.Homing:
                        for (int k = 0; k < count; k++)
                        {
                            Vector2 d = Random.insideUnitCircle.normalized;
                            _projectiles.Fire(ProjKind.Homing, origin, d, pspeed, dmg, dur, lv.pierce, 0f, 0f);
                        }
                        break;
                    case WeaponBehavior.Lob:
                        for (int k = 0; k < count; k++)
                        {
                            Vector2 tp = PickLobTarget(origin);
                            Vector2 d = (tp - origin);
                            float flight = Mathf.Clamp(d.magnitude / Mathf.Max(1f, pspeed), 0.2f, 1.2f);
                            _projectiles.Fire(ProjKind.Lob, origin, d, pspeed, dmg, flight + 0.05f, 0, area, flight);
                        }
                        break;
                    case WeaponBehavior.Chain:
                        FireChain(origin, count, dmg, area);
                        break;
                    case WeaponBehavior.Boomerang:
                        for (int k = 0; k < count; k++)
                        {
                            Vector2 d = Rotate(_facing, (k - (count - 1) * 0.5f) * 18f);
                            _projectiles.Fire(ProjKind.Boomerang, origin, d, pspeed, dmg, dur, 999, 0f, dur * 0.45f);
                        }
                        break;
                }
            }
        }

        void FireLinear(Vector2 origin, int count, float speed, float dmg, float dur, int pierce)
        {
            int tgt = _enemies.NearestEnemy(origin, 11f);
            Vector2 baseDir = tgt >= 0 ? (_enemies.EnemyPos(tgt) - origin).normalized : _facing;
            for (int k = 0; k < count; k++)
            {
                float spread = (k - (count - 1) * 0.5f) * 8f;
                _projectiles.Fire(ProjKind.Straight, origin, Rotate(baseDir, spread), speed, dmg, dur, pierce, 0f, 0f);
            }
        }

        void FireChain(Vector2 origin, int jumps, float dmg, float radius)
        {
            int tgt = _enemies.NearestEnemy(origin, 10f);
            if (tgt < 0) return;
            Vector2 at = _enemies.EnemyPos(tgt);
            for (int j = 0; j < jumps; j++)
            {
                _enemies.DamageAllInRadius(at, 0.6f, dmg);
                int next = _enemies.NearestEnemy(at + Random.insideUnitCircle * radius, radius);
                if (next < 0) break;
                at = _enemies.EnemyPos(next);
            }
        }

        Vector2 PickLobTarget(Vector2 origin)
        {
            int tgt = _enemies.NearestEnemy(origin, 12f);
            if (tgt >= 0) return _enemies.EnemyPos(tgt);
            return origin + (Vector2)(Random.insideUnitCircle * 6f);
        }

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
