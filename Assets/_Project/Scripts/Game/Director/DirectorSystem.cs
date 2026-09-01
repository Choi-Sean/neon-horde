using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Time-based spawn director. Rate ramps with run time; enemy types come from the
    /// map roster; elites appear on an interval; bosses at fixed minute marks.
    /// Spawn points are on a ring around the player, clamped inside the arena and off
    /// blocked cells.
    /// </summary>
    public sealed class DirectorSystem : MonoBehaviour
    {
        public static readonly float[] BossTimes = { 300f, 600f, 900f, 1200f };

        EnemyManager _enemies;
        PlayerController _player;
        RunManager _run;
        Arena _arena;

        float _accum;
        float _eliteTimer = 25f;
        int _nextBoss;

        void Start()
        {
            _enemies = FindFirstObjectByType<EnemyManager>();
            _player = FindFirstObjectByType<PlayerController>();
            _run = RunManager.Instance;
            _arena = FindFirstObjectByType<Arena>();
        }

        public void SimTick(float dt)
        {
            if (_enemies == null || _player == null || _run == null) return;
            RunState st = _run.State;
            float t = st.time;

            float rate = Mathf.Min(2f + t * 0.16f, 24f) * st.stats.enemyRateMul;
            _accum += rate * dt;
            int n = Mathf.FloorToInt(_accum);
            _accum -= n;
            for (int i = 0; i < n; i++)
            {
                if (_enemies.Count >= GameConfig.MaxEnemiesOnScreen) { _accum = 0f; break; }
                _enemies.Spawn(st.map != null ? st.map.RollEnemy(_run.Rng) : EnemyId.Walker, SpawnPos(9f, 13f));
            }

            _eliteTimer -= dt;
            float eliteInterval = 22f / (st.map != null ? Mathf.Max(0.25f, st.map.eliteRateMul) : 1f);
            if (t > 20f && _eliteTimer <= 0f)
            {
                _eliteTimer = eliteInterval;
                _enemies.Spawn(st.map != null ? st.map.RollEnemy(_run.Rng) : EnemyId.Tank, SpawnPos(10f, 13f), elite: true);
            }

            while (_nextBoss < BossTimes.Length && t >= BossTimes[_nextBoss])
            {
                _enemies.Spawn(EnemyId.Tank, SpawnPos(9f, 11f), boss: true);
                _nextBoss++;
            }
        }

        Vector2 SpawnPos(float minR, float maxR)
        {
            SeededRng rng = _run.Rng;
            Vector2 pp = _player.transform.position;
            NavGrid g = _arena != null ? _arena.Grid : null;
            MapPlan map = _run.State.map;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                float ang = rng.NextFloat() * Mathf.PI * 2f;
                float r = Mathf.Lerp(minR, maxR, rng.NextFloat());
                Vector2 p = pp + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                if (map != null) p = map.ClampToArena(p, 1.5f);
                if (g == null || !g.IsBlockedWorld(p)) return p;
            }
            return map != null ? map.ClampToArena(pp + Vector2.right * minR, 1.5f) : pp + Vector2.right * minR;
        }
    }
}
