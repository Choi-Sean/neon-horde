using UnityEngine;

namespace NeonHorde
{
    public enum ProjKind : byte { Straight, Homing, Boomerang, Lob, Orbit }

    /// <summary>
    /// Data-oriented friendly-projectile pool covering the linear / homing / boomerang /
    /// lob / orbit weapon behaviours. Single instanced batch (neon-yellow) for speed.
    /// </summary>
    public sealed class ProjectileManager : MonoBehaviour
    {
        public const int Capacity = 4000;

        struct P
        {
            public Vector2 pos;
            public Vector2 vel;
            public float speed;
            public float life;
            public float dmg;
            public int pierce;
            public byte kind;
            public float t;
            public float area;
            public float param;
            public float phase;
        }

        readonly P[] _p = new P[Capacity];
        int _count;
        public int Count => _count;

        SpritePool _pool;
        Sprite _sprite;
        static readonly Color Tint = Palette.Projectile * 1.7f;

        EnemyManager _enemies;
        Arena _arena;
        Transform _player;

        void Awake()
        {
            _sprite = NeonArt.GlowSprite(96, 1.7f);
            _pool = new SpritePool("FriendlyProjSprites", 14, transform);
        }

        void Start()
        {
            _enemies = FindFirstObjectByType<EnemyManager>();
            _arena = FindFirstObjectByType<Arena>();
            var pc = FindFirstObjectByType<PlayerController>();
            _player = pc != null ? pc.transform : null;
        }

        public void Fire(ProjKind kind, Vector2 pos, Vector2 dir, float speed, float dmg,
                         float life, int pierce, float area, float param, float phase0 = 0f)
        {
            if (_count >= Capacity) return;
            ref P p = ref _p[_count++];
            p.kind = (byte)kind;
            p.pos = pos;
            p.speed = speed;
            p.vel = dir.sqrMagnitude > 0.0001f ? dir.normalized * speed : Vector2.right * speed;
            p.dmg = dmg;
            p.life = life;
            p.pierce = pierce;
            p.t = 0f;
            p.area = area;
            p.param = param;   // orbit: angular speed (deg/s)
            p.phase = phase0;  // orbit: initial angle (rad)
        }

        void RemoveAt(int i) { _count--; if (i != _count) _p[i] = _p[_count]; }

        public void SimTick(float dt)
        {
            Vector2 pp = _player != null ? (Vector2)_player.position : Vector2.zero;

            for (int i = _count - 1; i >= 0; i--)
            {
                ref P p = ref _p[i];
                p.t += dt;
                p.life -= dt;
                bool remove = p.life <= 0f;

                switch ((ProjKind)p.kind)
                {
                    case ProjKind.Straight:
                        p.pos += p.vel * dt;
                        if (_enemies != null && _enemies.DamageFirstInRadius(p.pos, 0.2f, p.dmg))
                            if (--p.pierce < 0) remove = true;
                        if (_arena != null && _arena.Grid != null && _arena.Grid.IsBlockedWorld(p.pos))
                        {
                            _arena.DamageCrate(p.pos, 0.2f, p.dmg);
                            remove = true;
                        }
                        break;

                    case ProjKind.Homing:
                        if (_enemies != null)
                        {
                            int tgt = _enemies.NearestEnemy(p.pos, 8f);
                            if (tgt >= 0)
                            {
                                Vector2 want = (_enemies.EnemyPos(tgt) - p.pos).normalized * p.speed;
                                p.vel = Vector2.Lerp(p.vel, want, 0.15f);
                            }
                        }
                        p.pos += p.vel * dt;
                        if (_enemies != null && _enemies.DamageFirstInRadius(p.pos, 0.25f, p.dmg))
                            if (--p.pierce < 0) remove = true;
                        break;

                    case ProjKind.Boomerang:
                        if (p.t < p.param) p.pos += p.vel * dt;
                        else
                        {
                            Vector2 back = pp - p.pos;
                            if (back.sqrMagnitude > 0.01f) p.vel = back.normalized * p.speed;
                            p.pos += p.vel * dt;
                            if (back.sqrMagnitude < 0.6f) remove = true;
                        }
                        if (_enemies != null) _enemies.DamageFirstInRadius(p.pos, 0.28f, p.dmg * dt * 6f);
                        break;

                    case ProjKind.Lob:
                        p.pos += p.vel * dt;
                        if (p.t >= p.param)
                        {
                            if (_enemies != null) _enemies.DamageAllInRadius(p.pos, p.area, p.dmg);
                            if (_arena != null) _arena.DamageCrate(p.pos, p.area, p.dmg);
                            remove = true;
                        }
                        break;

                    case ProjKind.Orbit:
                        p.phase += p.param * Mathf.Deg2Rad * dt;
                        p.pos = pp + new Vector2(Mathf.Cos(p.phase), Mathf.Sin(p.phase)) * p.area;
                        if (_enemies != null) _enemies.DamageFirstInRadius(p.pos, 0.3f, p.dmg * dt * 8f);
                        break;
                }

                if (remove) RemoveAt(i);
            }
        }

        void LateUpdate()
        {
            if (_pool == null) return;
            _pool.Begin();
            for (int i = 0; i < _count; i++)
            {
                ref P p = ref _p[i];
                float ang = Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg;
                Vector2 scale = (ProjKind)p.kind switch
                {
                    ProjKind.Straight => new Vector2(1.0f, 0.32f),
                    ProjKind.Lob => new Vector2(0.55f, 0.55f),
                    ProjKind.Orbit => new Vector2(0.6f, 0.6f),
                    _ => new Vector2(0.5f, 0.34f)
                };
                _pool.Draw(_sprite, p.pos, -0.1f, Tint, scale, ang);
            }
            _pool.End();
        }
    }
}
