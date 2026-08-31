using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Data-oriented enemy pool. Struct entries in a packed array, one manager loop
    /// (no per-enemy MonoBehaviour), per-kind GPU-instanced draw, swap-remove on death.
    /// M2: catalog-driven types + behaviours, flow-field movement, arena collision,
    /// elite/boss, hostile projectiles.
    /// </summary>
    public sealed class EnemyManager : MonoBehaviour
    {
        public const int Capacity = 8000;

        [System.Flags] enum EFlag : byte { None = 0, Elite = 1, Boss = 2 }

        struct E
        {
            public Vector2 pos;
            public Vector2 vel;
            public float hp;
            public float maxHp;
            public byte id;
            public byte behavior;
            public byte flags;
            public float timer;      // ranged fire / exploder fuse / boss phase
            public float radius;
            public float hitFlash;
            public byte seed;        // per-enemy visual jitter (face rot / flip / scale)
        }

        readonly E[] _e = new E[Capacity];
        int _count;

        readonly SpatialHash _hash = new SpatialHash(1.0f);
        readonly List<int> _neighbors = new List<int>(64);

        SpritePool _pool;
        Sprite[] _spriteByKind;
        Color[] _colorByKind;
        Sprite _spriteElite, _spriteBoss;
        static readonly Color EliteTint = new Color(2.6f, 2.1f, 0.7f, 1f);
        static readonly Color BossTint = new Color(2.8f, 0.35f, 1.8f, 1f);

        // Face art: normal soldiers (mon1/mon2 …) on a coloured glow disc, plus a
        // mid-boss face (Tank/FrostTank/elite) and a boss face. Kind still readable by
        // glow colour + size.
        Sprite[] _faceNormals, _faceNormalsHurt;
        Sprite _faceMid, _faceBoss, _glowSprite;
        static readonly Color HurtTint = new Color(2.2f, 0.6f, 0.6f, 1f);

        static readonly string[] ShoutSpawn = { "WAAAGH!", "WRONG!", "NOT FAIR!", "SAD!", "BELIEVE ME!" };
        static readonly string[] ShoutHit = { "OW!", "OUCH!", "AARGH!", "NO!", "RIGGED!" };
        static readonly Color ShoutColor = new Color(2.6f, 2.2f, 0.5f, 1f);

        PlayerController _player;
        RunManager _run;
        PickupManager _pickups;
        Arena _arena;
        FlowFieldSystem _flow;
        HostileProjectileManager _hostile;
        DamageNumberSystem _dmgNumbers;

        public int Count => _count;
        public bool BossActive { get; private set; }
        public float BossHp01 { get; private set; }

        void Awake()
        {
            int kinds = EnemyCatalog.Count;
            _spriteByKind = new Sprite[kinds];
            _colorByKind = new Color[kinds];
            for (int i = 0; i < kinds; i++)
            {
                var def = EnemyCatalog.Get((EnemyId)i);
                _spriteByKind[i] = NeonArt.Sprite(ShapeFor((EnemyId)i), 128, 0.5f, 0.4f);
                _colorByKind[i] = def.color * 1.4f;
            }
            _spriteElite = NeonArt.Sprite(NeonShapeKind.Star, 160, 0.55f, 0.45f);
            _spriteBoss = NeonArt.Sprite(NeonShapeKind.Star, 192, 0.6f, 0.5f);
            _pool = new SpritePool("EnemySprites", 6, transform);

            // Joke build: every enemy wears a face. Real PNG if dropped in
            // Resources/art/enemy_face.png, otherwise the built-in cartoon face.
            var norm = new List<Sprite>();
            foreach (var k in new[] { "mon1", "mon2", "mon3", "enemy_face", "enemy_face_2" })
            {
                var sp = SpriteBank.Get(k);
                if (sp != null) norm.Add(sp);
            }
            if (norm.Count == 0) norm.Add(FaceArt.AngryFace());
            _faceNormals = norm.ToArray();
            _faceNormalsHurt = new Sprite[_faceNormals.Length];
            for (int i = 0; i < _faceNormals.Length; i++)
                _faceNormalsHurt[i] = FaceArt.DeriveHurt(_faceNormals[i]);

            _faceMid = SpriteBank.Get("mid-boss") ?? SpriteBank.Get("enemy_face_big") ?? _faceNormals[0];
            _faceBoss = SpriteBank.Get("boss") ?? _faceMid;
            _glowSprite = NeonArt.GlowSprite(96, 1.7f);
        }

        static NeonShapeKind ShapeFor(EnemyId id) => id switch
        {
            EnemyId.Walker => NeonShapeKind.Disc,
            EnemyId.Swarmer => NeonShapeKind.Triangle,
            EnemyId.Tank => NeonShapeKind.Hexagon,
            EnemyId.Spitter => NeonShapeKind.Diamond,
            EnemyId.Exploder => NeonShapeKind.Spark,
            EnemyId.Splitter => NeonShapeKind.Square,
            EnemyId.Splitterling => NeonShapeKind.Disc,
            EnemyId.Phantom => NeonShapeKind.Ring,
            EnemyId.FrostTank => NeonShapeKind.Hexagon,
            _ => NeonShapeKind.Disc,
        };

        void Start()
        {
            _run = RunManager.Instance;
            _player = FindFirstObjectByType<PlayerController>();
            _pickups = FindFirstObjectByType<PickupManager>();
            _arena = FindFirstObjectByType<Arena>();
            _flow = FindFirstObjectByType<FlowFieldSystem>();
            _hostile = FindFirstObjectByType<HostileProjectileManager>();
            _dmgNumbers = FindFirstObjectByType<DamageNumberSystem>();
        }

        public void ClearAll() { _count = 0; BossActive = false; }

        public void Spawn(EnemyId id, Vector2 pos, bool elite = false, bool boss = false)
        {
            if (_count >= Capacity) return;
            EnemyDef def = EnemyCatalog.Get(id);
            float t = _run != null ? _run.State.time : 0f;
            float hpMul = _run != null && _run.State.map != null ? _run.State.map.enemyHpMul : 1f;
            float hp = (def.hpBase + def.hpPer10s * (t / 10f)) * hpMul;
            float radius = def.radius;
            if (elite) { hp *= 8f; radius *= 1.8f; }
            if (boss) { hp *= 60f; radius *= 3.2f; }

            ref E e = ref _e[_count++];
            e.pos = pos;
            e.vel = Vector2.zero;
            e.hp = e.maxHp = hp;
            e.id = (byte)id;
            e.behavior = (byte)def.behavior;
            e.flags = (byte)((elite ? EFlag.Elite : 0) | (boss ? EFlag.Boss : 0));
            e.timer = 0f;
            e.radius = radius;
            e.hitFlash = 0f;
            e.seed = (byte)UnityEngine.Random.Range(0, 256);
            if (boss)
            {
                BossActive = true;
                if (_run != null) _run.RaiseBossSpawn();
                if (NeonVfx.Instance != null) NeonVfx.Instance.Ring(pos, new Color(2.6f, 0.4f, 1.6f, 1f), 4f, 0.7f);
            }

            if (_dmgNumbers != null && (boss || Random.value < 0.5f))
                _dmgNumbers.Shout(pos, boss ? "WAAAGH!!!" : ShoutSpawn[Random.Range(0, ShoutSpawn.Length)],
                    ShoutColor, boss ? 64 : 40, 0.9f);
        }

        void RemoveAt(int i)
        {
            bool wasBoss = (_e[i].flags & (byte)EFlag.Boss) != 0;
            _count--;
            if (i != _count) _e[i] = _e[_count];
            if (wasBoss) BossActive = false;
        }

        public void BuildHash()
        {
            _hash.Clear();
            for (int i = 0; i < _count; i++) _hash.Insert(i, _e[i].pos);
        }
        public void RebuildHash() => BuildHash();

        public void SimTick(float dt)
        {
            if (_player == null) return;
            Vector2 target = _player.transform.position;
            SeededRng rng = _run != null ? _run.Rng : null;
            float speedMul = _run != null && _run.State.map != null ? _run.State.map.enemySpeedMul : 1f;
            NavGrid grid = _arena != null ? _arena.Grid : null;

            BuildHash();
            BossActive = false;

            for (int i = 0; i < _count; i++)
            {
                ref E e = ref _e[i];
                EnemyDef def = EnemyCatalog.Get((EnemyId)e.id);
                bool boss = (e.flags & (byte)EFlag.Boss) != 0;
                if (boss) { BossActive = true; BossHp01 = Mathf.Clamp01(e.hp / e.maxHp); }

                Vector2 toTarget = target - e.pos;
                float dist = toTarget.magnitude;

                // desired direction: flow field, fallback to direct
                Vector2 desired;
                if (_flow != null && _flow.Ready)
                {
                    Vector2 ff = _flow.SampleDir(e.pos);
                    desired = ff.sqrMagnitude > 0.01f ? ff : (dist > 0.001f ? toTarget / dist : Vector2.zero);
                }
                else desired = dist > 0.001f ? toTarget / dist : Vector2.zero;

                float spd = def.speedBase * speedMul;
                float behaviorScale = 1f;

                switch ((EnemyBehavior)e.behavior)
                {
                    case EnemyBehavior.Phantom:
                        behaviorScale = 1.15f;
                        break;
                    case EnemyBehavior.Charger:
                        e.timer -= dt;
                        if (e.timer <= 0f) { e.timer = 2.5f; }
                        behaviorScale = e.timer < 0.8f ? 3.0f : 0.9f; // wind-up then dash
                        break;
                    case EnemyBehavior.Ranged:
                        e.timer -= dt;
                        if (dist < 9f && e.timer <= 0f && _hostile != null)
                        {
                            _hostile.Fire(e.pos, (target - e.pos).normalized, 7f, 8, 3f);
                            e.timer = 2.2f;
                        }
                        if (dist < 6f) behaviorScale = -0.6f; // kite
                        break;
                    case EnemyBehavior.Exploder:
                        if (dist < e.radius + 0.7f)
                        {
                            if (_player != null) _player.TakeContactDamage(def.contactDamage);
                            DamageAllInRadius(e.pos, 1.8f, e.maxHp); // also hurt other enemies
                            RemoveAt(i); i--; continue;
                        }
                        behaviorScale = 1.1f;
                        break;
                }

                Vector2 sep = Vector2.zero;
                _hash.Query(e.pos, 0.8f, _neighbors);
                for (int n = 0; n < _neighbors.Count; n++)
                {
                    int j = _neighbors[n];
                    if (j == i || j >= _count) continue;
                    Vector2 d = e.pos - _e[j].pos;
                    float m = d.sqrMagnitude;
                    if (m > 0.0001f && m < 0.64f) sep += d / m;
                }

                Vector2 steer = desired * (spd * behaviorScale) + sep * 2.4f;
                e.vel = Vector2.Lerp(e.vel, steer, 0.25f);

                Vector2 next = e.pos + e.vel * dt;
                if (grid != null && grid.IsBlockedWorld(next))
                {
                    Vector2 nx = new Vector2(next.x, e.pos.y);
                    Vector2 ny = new Vector2(e.pos.x, next.y);
                    if (!grid.IsBlockedWorld(nx)) next = nx;
                    else if (!grid.IsBlockedWorld(ny)) next = ny;
                    else next = e.pos;
                }
                e.pos = next;

                if (e.hitFlash > 0f) e.hitFlash -= dt;

                float touch = e.radius + 0.35f;
                if (dist < touch && (EnemyBehavior)e.behavior != EnemyBehavior.Exploder)
                    _player.TakeContactDamage(def.contactDamage);
            }
        }

        // ---- damage API used by weapons / projectiles ----

        public int NearestEnemy(Vector2 pos, float maxRange)
        {
            _hash.Query(pos, maxRange, _neighbors);
            float best = maxRange * maxRange;
            int bi = -1;
            for (int n = 0; n < _neighbors.Count; n++)
            {
                int j = _neighbors[n];
                if (j >= _count) continue;
                float d = (_e[j].pos - pos).sqrMagnitude;
                if (d < best) { best = d; bi = j; }
            }
            return bi;
        }

        public Vector2 EnemyPos(int i) => (uint)i < (uint)_count ? _e[i].pos : Vector2.zero;

        public bool DamageFirstInRadius(Vector2 pos, float radius, float dmg)
        {
            _hash.Query(pos, radius + 0.7f, _neighbors);
            for (int n = 0; n < _neighbors.Count; n++)
            {
                int j = _neighbors[n];
                if (j >= _count) continue;
                float rr = radius + _e[j].radius;
                if ((_e[j].pos - pos).sqrMagnitude <= rr * rr)
                {
                    DamageIndex(j, dmg);
                    return true;
                }
            }
            return false;
        }

        public int DamageAllInRadius(Vector2 pos, float radius, float dmg)
        {
            _hash.Query(pos, radius + 0.7f, _neighbors);
            int hits = 0;
            for (int n = 0; n < _neighbors.Count; n++)
            {
                int j = _neighbors[n];
                if (j >= _count) continue;
                float rr = radius + _e[j].radius;
                if ((_e[j].pos - pos).sqrMagnitude <= rr * rr)
                {
                    if (DamageIndex(j, dmg)) { n--; }
                    hits++;
                }
            }
            return hits;
        }

        public int DamageArc(Vector2 origin, Vector2 dir, float range, float halfAngleDeg, float dmg)
        {
            dir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
            float cosHalf = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
            _hash.Query(origin, range + 0.7f, _neighbors);
            int hits = 0;
            for (int n = 0; n < _neighbors.Count; n++)
            {
                int j = _neighbors[n];
                if (j >= _count) continue;
                Vector2 to = _e[j].pos - origin;
                if (to.sqrMagnitude > (range + _e[j].radius) * (range + _e[j].radius)) continue;
                if (Vector2.Dot(to.normalized, dir) < cosHalf) continue;
                if (DamageIndex(j, dmg)) n--;
                hits++;
            }
            return hits;
        }

        /// <returns>true if the enemy died (was removed)</returns>
        bool DamageIndex(int i, float dmg)
        {
            ref E e = ref _e[i];
            e.hp -= dmg;
            e.hitFlash = 0.08f;
            if (_dmgNumbers != null) _dmgNumbers.Report(e.pos, dmg);
            if (NeonVfx.Instance != null && Random.value < 0.35f)
                NeonVfx.Instance.Spark(e.pos, EnemyCatalog.Get((EnemyId)e.id).color, 0.5f);
            if (e.hp > 0f)
            {
                if (_dmgNumbers != null && Random.value < 0.3f)
                    _dmgNumbers.Shout(e.pos, ShoutHit[Random.Range(0, ShoutHit.Length)], ShoutColor, 36, 0.6f);
                return false;
            }

            EnemyDef def = EnemyCatalog.Get((EnemyId)e.id);
            bool elite = (e.flags & (byte)EFlag.Elite) != 0;
            bool boss = (e.flags & (byte)EFlag.Boss) != 0;
            Vector2 at = e.pos;
            if (NeonVfx.Instance != null)
                NeonVfx.Instance.Burst(at, def.color * 1.3f, boss ? 3.5f : elite ? 1.8f : 0.9f, boss ? 24 : elite ? 14 : 8);
            SeededRng rng = _run != null ? _run.Rng : null;

            if (_pickups != null)
            {
                _pickups.SpawnGem(at, def.xpValue * (elite ? 6f : 1f) * (boss ? 40f : 1f));
                float goldChance = def.goldChance + (elite ? 0.6f : 0f) + (boss ? 5f : 0f);
                if (rng != null && rng.Chance(Mathf.Min(goldChance, 1f)))
                    _pickups.SpawnGold(at, elite || boss ? 5 : 1);
                if (elite || boss) _pickups.SpawnChest(at);
            }

            if (def.behavior == EnemyBehavior.Splitter)
                for (int s = 0; s < 3; s++)
                {
                    Vector2 off = rng != null
                        ? new Vector2(rng.Range(-0.6f, 0.6f), rng.Range(-0.6f, 0.6f))
                        : Vector2.zero;
                    Spawn(EnemyId.Splitterling, at + off);
                }

            if (def.behavior == EnemyBehavior.FrostTank && _player != null)
                _player.ApplySlow(0.5f, 1.2f);

            if (_run != null)
            {
                _run.State.kills++;
                if (boss) _run.OnBossDefeated();
            }

            ScreenShake.Add(boss ? 0.6f : elite ? 0.22f : 0.045f);

            RemoveAt(i);
            return true;
        }

        void LateUpdate()
        {
            if (_pool == null) return;
            _pool.Begin();
            for (int i = 0; i < _count; i++)
            {
                ref E e = ref _e[i];
                bool boss = (e.flags & (byte)EFlag.Boss) != 0;
                bool elite = !boss && (e.flags & (byte)EFlag.Elite) != 0;
                Color kindCol = boss ? BossTint : elite ? EliteTint : _colorByKind[e.id];

                if (_faceNormals != null)
                {
                    bool hit = e.hitFlash > 0f;
                    bool isMid = !boss && (elite
                                 || e.id == (byte)EnemyId.Tank || e.id == (byte)EnemyId.FrostTank);

                    // per-enemy jitter so a horde isn't identical clones
                    int s = e.seed;
                    float flip = (s & 1) == 0 ? 1f : -1f;
                    float baseRot = ((s >> 1) % 15) - 7f;                  // -7..+7 deg
                    float sizeJit = 0.92f + ((s >> 4) & 7) / 7f * 0.16f;   // 0.92..1.08

                    // "적당히": modest face scale, hard cap per tier (~70% of the last pass)
                    float cap = boss ? 1.26f : isMid ? 0.88f : 0.56f;
                    float sc = Mathf.Min(e.radius * 0.95f * sizeJit, cap);

                    _pool.Draw(_glowSprite, e.pos, 0f, kindCol * 0.9f, sc * 1.15f);

                    Sprite faceBase, faceHurt;
                    if (boss) { faceBase = _faceBoss; faceHurt = null; }
                    else if (isMid) { faceBase = _faceMid; faceHurt = null; }
                    else
                    {
                        int vi = s % _faceNormals.Length;
                        faceBase = _faceNormals[vi];
                        faceHurt = _faceNormalsHurt[vi];
                    }

                    Sprite fs = hit && faceHurt != null ? faceHurt : faceBase;
                    Color faceCol = hit && faceHurt == null ? HurtTint : Color.white;
                    float rot = baseRot + (hit ? 14f * Mathf.Sin(Time.time * 80f + s) : 0f);
                    float scShown = hit ? sc * 1.1f : sc;
                    _pool.Draw(fs, e.pos, -0.1f, faceCol, new Vector2(flip * scShown, scShown), rot);
                    continue;
                }

                Sprite sprite = boss ? _spriteBoss : elite ? _spriteElite : _spriteByKind[e.id];
                Color col = kindCol;
                if (e.hitFlash > 0f) col = Color.Lerp(col, Color.white * 2.2f, 0.7f);
                _pool.Draw(sprite, e.pos, 0f, col, e.radius * 2.8f);
            }
            _pool.End();
        }
    }
}
