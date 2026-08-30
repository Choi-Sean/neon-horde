using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// IMGUI diagnostic overlay (no font asset needed). Confirms the sim is actually
    /// running on device: entity counts, run time, HP, fps, paused state.
    /// Toggle off in Systems once art/HUD land.
    /// </summary>
    public sealed class DevHud : MonoBehaviour
    {
        public bool show = true;

        EnemyManager _enemies;
        ProjectileManager _proj;
        PickupManager _pick;
        PlayerController _player;
        Arena _arena;

        float _fps;
        GUIStyle _style;

        void Start()
        {
            _enemies = FindFirstObjectByType<EnemyManager>();
            _proj = FindFirstObjectByType<ProjectileManager>();
            _pick = FindFirstObjectByType<PickupManager>();
            _player = FindFirstObjectByType<PlayerController>();
            _arena = FindFirstObjectByType<Arena>();
        }

        void Update()
        {
            float f = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _fps = Mathf.Lerp(_fps, f, 0.1f);
        }

        void OnGUI()
        {
            if (!show) return;
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 34, normal = { textColor = Color.white } };

            var rm = RunManager.Instance;
            string s =
                $"fps {_fps:0}\n" +
                $"scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}\n" +
                $"run {(rm != null ? rm.State.time.ToString("0.0") : "-")}  paused {(rm != null && rm.Paused)}\n" +
                $"enemies {(_enemies != null ? _enemies.Count : -1)}\n" +
                $"proj {(_proj != null ? "ok" : "null")}  pick {(_pick != null ? "ok" : "null")}\n" +
                $"arena {(_arena != null && _arena.Grid != null ? $"{_arena.Grid.width}x{_arena.Grid.height} terrain {_arena.Plan.terrain.Count}" : "null")}\n" +
                $"hp {(_player != null ? $"{_player.Hp}/{_player.MaxHp}" : "-")}  " +
                $"lvl {(rm != null ? rm.State.level : 0)}  kills {(rm != null ? rm.State.kills : 0)}";

            GUI.Label(new Rect(24, 24, 900, 500), s, _style);
        }
    }
}
