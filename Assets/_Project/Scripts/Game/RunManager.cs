using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Owns the fixed-timestep simulation loop, RunState, and the arena. Ticks each
    /// system in a fixed order; pausing (level-up / chest / game-over) stops the sim
    /// without touching Time.timeScale. Banks run rewards to MetaState on end.
    /// </summary>
    public sealed class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        public RunState State { get; private set; }
        public SeededRng Rng { get; private set; }
        public bool Paused { get; set; }
        public int Seed { get; private set; }

        public event Action OnLevelUp;
        public event Action OnGameOver;
        public event Action OnRunEnded;
        public event Action OnBossSpawn;
        public event Action OnDeathPrompt;

        public void RaiseBossSpawn() => OnBossSpawn?.Invoke();

        bool _awaitingDeath;
        bool _adReviveUsed;
        bool _finalized;
        long _goldToMeta;

        public bool AdReviveAvailable => !_adReviveUsed;

        float _accumulator;

        PlayerController _player;
        DirectorSystem _director;
        EnemyManager _enemies;
        WeaponSystem _weapons;
        ProjectileManager _projectiles;
        HostileProjectileManager _hostile;
        PickupManager _pickups;
        Arena _arena;
        FlowFieldSystem _flow;

        List<UpgradeOption> _pendingOptions;

        void Awake()
        {
            Instance = this;
            State = new RunState();

            Seed = unchecked((int)DateTime.UtcNow.Ticks);
            State.characterId = GameBootstrap.MetaCtl != null
                ? GameBootstrap.MetaCtl.SelectedCharacter
                : CharacterId.Pulse;

            if (RunConfig.HasOverride) { Seed = RunConfig.Seed; State.characterId = RunConfig.Character; }
            Rng = new SeededRng((ulong)(uint)Seed);

            if (GameBootstrap.MetaCtl != null)
            {
                State.metaMods = GameBootstrap.MetaCtl.BuildMods();
                State.freeRevives = GameBootstrap.MetaCtl.FreeRevives;
            }

            GameConfig.ApplyFromSettings(GameBootstrap.Meta != null ? GameBootstrap.Meta.settings.quality : 1);
        }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            _director = FindFirstObjectByType<DirectorSystem>();
            _enemies = FindFirstObjectByType<EnemyManager>();
            _weapons = FindFirstObjectByType<WeaponSystem>();
            _projectiles = FindFirstObjectByType<ProjectileManager>();
            _hostile = FindFirstObjectByType<HostileProjectileManager>();
            _pickups = FindFirstObjectByType<PickupManager>();
            _arena = FindFirstObjectByType<Arena>();
            _flow = FindFirstObjectByType<FlowFieldSystem>();

            if (_arena != null) _arena.Build(Seed);
            State.map = _arena != null ? _arena.Plan : null;

            // starting loadout from character
            CharacterDef cdef = CharacterCatalog.Get(State.characterId);
            State.RecomputeStats(cdef.mods);
            State.AddOrLevelWeapon(cdef.startWeapon);
            if (_player != null) _player.RecalcMaxHp(true);

            if (ServiceLocator.TryGet<IAnalyticsService>(out var a))
                a.Log("run_start", ("char", State.characterId.ToString()),
                      ("daily", RunConfig.IsDaily), ("seed", Seed));
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (Paused || State.gameOver) return;

            _accumulator += Mathf.Min(Time.deltaTime, Balance.MaxFrameCatchUp);
            while (_accumulator >= Balance.FixedDt)
            {
                SimTick(Balance.FixedDt);
                _accumulator -= Balance.FixedDt;
                if (Paused || State.gameOver) { _accumulator = 0f; break; }
            }
        }

        void SimTick(float dt)
        {
            State.time += dt;

            if (_player != null) _player.SimTick(dt);
            if (_flow != null) _flow.SimTick(dt);
            if (_director != null) _director.SimTick(dt);
            if (_enemies != null) { _enemies.SimTick(dt); _enemies.RebuildHash(); }
            if (_weapons != null) _weapons.SimTick(dt);
            if (_projectiles != null) _projectiles.SimTick(dt);
            if (_hostile != null) _hostile.SimTick(dt);
            if (_pickups != null) _pickups.SimTick(dt);

            if ((State.pendingLevelUps > 0 || State.pendingChests > 0) && !Paused)
            {
                Paused = true;
                RaiseUpgrade();
            }

            if (State.time >= 1200f && !State.gameOver) EndRun(true);

            if (_player != null && !_player.Alive && !State.gameOver && !_awaitingDeath)
            {
                if (State.freeRevives > 0)
                {
                    State.freeRevives--;
                    _player.Revive();
                }
                else
                {
                    _awaitingDeath = true;
                    Paused = true;
                    OnDeathPrompt?.Invoke();
                }
            }
        }

        public void AdRevive()
        {
            _awaitingDeath = false;
            _adReviveUsed = true;
            Paused = false;
            _player?.Revive();
            if (ServiceLocator.TryGet<IAnalyticsService>(out var a))
                a.Log("ad_revive", ("time", Mathf.FloorToInt(State.time)));
        }

        public void DoubleGoldReward()
        {
            GameBootstrap.MetaCtl?.AddGold(State.goldBanked);
            State.goldBanked *= 2;
            _goldToMeta = State.goldBanked;
        }

        // ---- upgrade / chest flow ----

        void RaiseUpgrade()
        {
            int count = State.pendingChests > 0 ? 3 : 3;
            _pendingOptions = UpgradeGenerator.Generate(State, Rng, count);
            OnLevelUp?.Invoke();
        }

        public IReadOnlyList<UpgradeOption> CurrentOptions => _pendingOptions;
        public int Rerolls => State.rerolls;
        public int Banishes => State.banishes;

        public bool RerollOptions()
        {
            if (State.rerolls <= 0) return false;
            State.rerolls--;
            _pendingOptions = UpgradeGenerator.Generate(State, Rng, 3);
            OnLevelUp?.Invoke();
            return true;
        }

        public bool BanishOption(int index)
        {
            if (State.banishes <= 0 || _pendingOptions == null || index < 0 || index >= _pendingOptions.Count)
                return false;
            State.banishes--;
            State.banned.Add(_pendingOptions[index].key);
            _pendingOptions = UpgradeGenerator.Generate(State, Rng, 3);
            OnLevelUp?.Invoke();
            return true;
        }

        public void ChooseUpgrade(int index)
        {
            if (_pendingOptions != null && index >= 0 && index < _pendingOptions.Count)
            {
                if (ServiceLocator.TryGet<IAnalyticsService>(out var a))
                    a.Log("level_up", ("level", State.level), ("pick", _pendingOptions[index].key));
                _pendingOptions[index].apply?.Invoke();
            }

            State.RecomputeStats(CharacterCatalog.Get(State.characterId).mods);
            if (_player != null) _player.RecalcMaxHp();

            if (State.pendingLevelUps > 0) State.pendingLevelUps--;
            else if (State.pendingChests > 0) State.pendingChests--;

            if (State.pendingLevelUps > 0 || State.pendingChests > 0) RaiseUpgrade();
            else { _pendingOptions = null; Paused = false; }
        }

        public int BossKills { get; private set; }

        public void OnBossDefeated()
        {
            BossKills++;
        }

        // ---- run end ----

        public void EndRun(bool victory)
        {
            if (State.gameOver) return;
            _awaitingDeath = false;

            State.gameOver = true;
            State.victory = victory;
            Paused = true;

            int secs = Mathf.FloorToInt(State.time);
            var meta = GameBootstrap.Meta;
            var metaCtl = GameBootstrap.MetaCtl;

            if (!_finalized)
            {
                _finalized = true;
                if (meta != null)
                {
                    meta.totalRuns++;
                    meta.totalKills += State.kills;
                    meta.bossKills += BossKills;
                    if (secs > meta.bestTimeSec) meta.bestTimeSec = secs;
                }
                if (ServiceLocator.TryGet<QuestService>(out var quests))
                    quests.OnRunEnded(new RunEndSummary
                    {
                        kills = State.kills, timeSec = secs, level = State.level,
                        bossKills = BossKills, goldBanked = State.goldBanked
                    });
                if (ServiceLocator.TryGet<IAnalyticsService>(out var a))
                    a.Log("run_end", ("time", secs), ("kills", State.kills), ("level", State.level),
                          ("victory", victory), ("gold", State.goldBanked),
                          ("theme", State.map != null ? State.map.themeId.ToString() : "?"),
                          ("daily", RunConfig.IsDaily));
            }

            metaCtl?.AddGold(State.goldBanked - _goldToMeta);
            _goldToMeta = State.goldBanked;

            OnGameOver?.Invoke();
            OnRunEnded?.Invoke();
        }
    }
}
