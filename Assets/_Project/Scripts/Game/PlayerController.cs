using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Player avatar. Movement + damage + terrain interaction driven from RunManager's
    /// fixed tick. Applies derived stats, arena collide-and-slide, slow/hazard zones,
    /// regen, and the selected character's signature ability.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerController : MonoBehaviour
    {
        const float InvulnSeconds = 0.6f;

        PlayerInputReader _input;
        RunManager _run;
        Arena _arena;
        EnemyManager _enemies;

        float _invuln;
        float _slowTimer, _slowStatusMul = 1f;
        float _regenAcc, _hazardAcc;
        float _abilityTimer;
        int _baseMaxHp = Balance.PlayerMaxHp;

        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool Alive => Hp > 0;
        public Vector2 Position => transform.position;

        void Awake() => _input = GetComponent<PlayerInputReader>();

        void Start()
        {
            _run = RunManager.Instance;
            _arena = FindFirstObjectByType<Arena>();
            _enemies = FindFirstObjectByType<EnemyManager>();
            RecalcMaxHp(true);
            if (_run != null) _run.OnLevelUp += OnLevelUp;
        }

        void OnDestroy()
        {
            if (_run != null) _run.OnLevelUp -= OnLevelUp;
        }

        public void RecalcMaxHp(bool fill = false)
        {
            float mul = _run != null ? _run.State.stats.maxHpMul : 1f;
            int newMax = Mathf.Max(1, Mathf.RoundToInt(_baseMaxHp * mul));
            int delta = newMax - MaxHp;
            MaxHp = newMax;
            if (fill) Hp = MaxHp;
            else if (delta > 0) Hp = Mathf.Min(MaxHp, Hp + delta);
            Hp = Mathf.Clamp(Hp, fill ? 1 : 0, MaxHp);
            if (fill && Hp <= 0) Hp = MaxHp;
        }

        public void SimTick(float dt)
        {
            if (_invuln > 0f) _invuln -= dt;
            if (_slowTimer > 0f) { _slowTimer -= dt; if (_slowTimer <= 0f) _slowStatusMul = 1f; }

            DerivedStats ds = _run != null ? _run.State.stats : DerivedStats.Identity;

            if (ds.regenPerSec > 0f && Hp < MaxHp)
            {
                _regenAcc += ds.regenPerSec * dt;
                if (_regenAcc >= 1f) { int h = Mathf.FloorToInt(_regenAcc); _regenAcc -= h; Hp = Mathf.Min(MaxHp, Hp + h); }
            }

            float slow = 1f;
            NavGrid g = _arena != null ? _arena.Grid : null;
            if (g != null)
            {
                byte f = g.FlagsAt(transform.position);
                if ((f & (byte)NavGrid.Flag.Slow) != 0) slow = 0.6f;
                if ((f & (byte)NavGrid.Flag.Hazard) != 0)
                {
                    _hazardAcc += 8f * dt;
                    if (_hazardAcc >= 1f) { int d = Mathf.FloorToInt(_hazardAcc); _hazardAcc -= d; TakeDamageRaw(d); }
                }
            }
            if (_slowTimer > 0f) slow = Mathf.Min(slow, _slowStatusMul);

            Vector2 move = _input.Move;
            if (move.sqrMagnitude > 0f)
            {
                float speed = Balance.PlayerMoveSpeed * ds.moveSpeedMul * slow;
                MoveWithCollision(move * (speed * dt), g);
            }

            if (_run != null && _run.State.map != null)
                transform.position = _run.State.map.ClampToArena(transform.position, 0.5f);

            TickAbility(dt, ds);
        }

        void MoveWithCollision(Vector2 delta, NavGrid g)
        {
            Vector2 p = transform.position;
            if (g == null) { transform.position = p + delta; return; }
            Vector2 nx = new Vector2(p.x + delta.x, p.y);
            if (!g.IsBlockedWorld(nx)) p.x = nx.x;
            Vector2 ny = new Vector2(p.x, p.y + delta.y);
            if (!g.IsBlockedWorld(ny)) p.y = ny.y;
            transform.position = p;
        }

        void TickAbility(float dt, DerivedStats ds)
        {
            if (_run == null) return;
            _abilityTimer -= dt;
            CharacterAbility ability = CharacterCatalog.Get(_run.State.characterId).ability;

            switch (ability)
            {
                case CharacterAbility.Shockwave:
                    if (_abilityTimer <= 0f)
                    {
                        _abilityTimer = 5f;
                        if (_enemies != null) _enemies.DamageAllInRadius(transform.position, 2.6f, 8f * ds.damageMul);
                    }
                    break;
                case CharacterAbility.LowHpInvuln:
                    if (_abilityTimer <= 0f && Hp > 0 && Hp / (float)MaxHp <= 0.30f)
                    {
                        _abilityTimer = 20f;
                        _invuln = 3f;
                    }
                    break;
            }
        }

        void OnLevelUp()
        {
            if (NeonVfx.Instance != null)
            {
                var c = CharacterCatalog.Get(_run != null ? _run.State.characterId : CharacterId.Pulse).color;
                NeonVfx.Instance.Ring(transform.position, c * 1.4f, 3.5f, 0.5f);
                NeonVfx.Instance.Flash(transform.position, c * 1.6f, 2f, 0.25f);
            }
            ScreenShake.Add(0.35f);

            if (_run != null && CharacterCatalog.Get(_run.State.characterId).ability == CharacterAbility.LevelUpBlast
                && _enemies != null)
                _enemies.DamageAllInRadius(transform.position, 3.2f, 30f);
        }

        public void TakeContactDamage(int amount)
        {
            if (_invuln > 0f || !Alive) return;
            DerivedStats ds = _run != null ? _run.State.stats : DerivedStats.Identity;
            int dealt = Mathf.Max(1, Mathf.Abs(amount) - ds.armor);
            Hp = Mathf.Max(0, Hp - dealt);
            _invuln = InvulnSeconds;
        }

        void TakeDamageRaw(int amount)
        {
            if (!Alive) return;
            Hp = Mathf.Max(0, Hp - Mathf.Abs(amount));
        }

        public void ApplySlow(float mul, float seconds)
        {
            _slowStatusMul = Mathf.Min(_slowStatusMul, mul);
            _slowTimer = Mathf.Max(_slowTimer, seconds);
        }

        public void Revive()
        {
            Hp = MaxHp;
            _invuln = 2.5f;
            if (_enemies != null) _enemies.DamageAllInRadius(transform.position, 5f, 9999f);
        }
    }
}
