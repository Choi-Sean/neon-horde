using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// IMGUI diagnostic overlay (no font asset needed). Confirms the sim is actually
    /// running on device: entity counts, run time, HP, fps, paused state, plus the
    /// render-path facts that matter (sprite counts, view size, nearest enemy).
    /// Toggle off in Systems once art/HUD land.
    /// </summary>
    public sealed class DevHud : MonoBehaviour
    {
        public bool show = true;

        EnemyManager _enemies;
        ProjectileManager _proj;
        HostileProjectileManager _hproj;
        PickupManager _pick;
        PlayerController _player;
        Arena _arena;

        float _fps;
        GUIStyle _style;

        void Start()
        {
            _enemies = FindFirstObjectByType<EnemyManager>();
            _proj = FindFirstObjectByType<ProjectileManager>();
            _hproj = FindFirstObjectByType<HostileProjectileManager>();
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
                _style = new GUIStyle(GUI.skin.label) { fontSize = 30, normal = { textColor = Color.white } };

            var rm = RunManager.Instance;
            var cam = Camera.main;
            string view = "-";
            if (cam != null)
                view = $"{cam.orthographicSize * 2f * cam.aspect:0.0}w x {cam.orthographicSize * 2f:0.0}h";

            string s =
                $"fps {_fps:0}   scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}\n" +
                $"run {(rm != null ? rm.State.time.ToString("0.0") : "-")}   paused {(rm != null && rm.Paused)}   over {(rm != null && rm.State.gameOver)}\n" +
                $"enemies {(_enemies != null ? _enemies.Count : -1)}   proj {(_proj != null ? _proj.Count : -1)}   hproj {(_hproj != null ? _hproj.Count : -1)}   pick {(_pick != null ? _pick.Count : -1)}\n" +
                $"arena {(_arena != null && _arena.Grid != null ? $"{_arena.Grid.width}x{_arena.Grid.height} terrain {_arena.Plan.terrain.Count}" : "null")}\n" +
                $"view {view}   playerPos {(_player != null ? ((Vector2)_player.transform.position).ToString("0.0") : "-")}\n" +
                $"hp {(_player != null ? $"{_player.Hp:0}/{_player.MaxHp:0}" : "-")}   lvl {(rm != null ? rm.State.level : 0)}   kills {(rm != null ? rm.State.kills : 0)}\n" +
                $"instancing {SystemInfo.supportsInstancing}   unlit {(Shader.Find("Universal Render Pipeline/Unlit") != null)}";

            GUI.Label(new Rect(24, 24, 1000, 520), s, _style);
        }
    }
}
