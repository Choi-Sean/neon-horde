using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonHorde
{
    /// <summary>Editor/dev helper: P spawns 300, O spawns 1000, B spawns a boss, K adds a level-up.</summary>
    public sealed class DebugSpawner : MonoBehaviour
    {
        EnemyManager _enemies;
        PlayerController _player;
        RunManager _run;

        void Start()
        {
            _enemies = FindFirstObjectByType<EnemyManager>();
            _player = FindFirstObjectByType<PlayerController>();
            _run = RunManager.Instance;
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k == null || _enemies == null || _player == null) return;

            if (k.pKey.wasPressedThisFrame) SpawnBurst(300, false);
            if (k.oKey.wasPressedThisFrame) SpawnBurst(1000, false);
            if (k.bKey.wasPressedThisFrame) _enemies.Spawn(EnemyId.Tank, (Vector2)_player.transform.position + Vector2.up * 6f, boss: true);
            if (k.kKey.wasPressedThisFrame && _run != null) _run.State.pendingLevelUps++;
        }

        void SpawnBurst(int n, bool elite)
        {
            Vector2 c = _player.transform.position;
            int kinds = System.Enum.GetValues(typeof(EnemyId)).Length;
            for (int i = 0; i < n; i++)
            {
                Vector2 pos = c + Random.insideUnitCircle.normalized * Random.Range(6f, 14f);
                _enemies.Spawn((EnemyId)(i % kinds), pos, elite);
            }
            Debug.Log($"[Debug] spawned {n} (total {_enemies.Count})");
        }
    }
}
