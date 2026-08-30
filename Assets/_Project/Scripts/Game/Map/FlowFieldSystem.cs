using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Maintains a flow-field toward the player over the arena nav grid. Rebuilds when
    /// the player crosses a cell boundary or after a max interval. Enemies sample it
    /// for obstacle-aware movement.
    /// </summary>
    public sealed class FlowFieldSystem : MonoBehaviour
    {
        const float MaxRebuildInterval = 0.4f;

        FlowField _field;
        NavGrid _grid;
        PlayerController _player;
        Arena _arena;

        int _lastCellX = int.MinValue, _lastCellY = int.MinValue;
        float _timer;
        public bool Ready { get; private set; }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            _arena = FindFirstObjectByType<Arena>();
        }

        void EnsureField()
        {
            if (_arena == null || _arena.Grid == null) return;
            if (_field == null || _grid != _arena.Grid)
            {
                _grid = _arena.Grid;
                _field = new FlowField(_grid);
                _lastCellX = int.MinValue;
            }
        }

        public void SimTick(float dt)
        {
            EnsureField();
            if (_field == null || _player == null) return;

            _timer += dt;
            _grid.CellOf(_player.transform.position, out int cx, out int cy);
            if (cx != _lastCellX || cy != _lastCellY || _timer >= MaxRebuildInterval)
            {
                _field.Rebuild(cx, cy);
                _lastCellX = cx;
                _lastCellY = cy;
                _timer = 0f;
                Ready = true;
            }
        }

        public Vector2 SampleDir(Vector2 worldPos)
            => _field != null ? _field.SampleDir(worldPos) : Vector2.zero;

        public bool HasPath(Vector2 worldPos) => _field != null && _field.HasPath(worldPos);
    }
}
