using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// BFS flow-field toward a target cell over a NavGrid. Each open cell stores the
    /// direction (index 0..7) of its neighbour that is closer to the target.
    /// Rebuilt periodically as the player moves.
    /// </summary>
    public sealed class FlowField
    {
        static readonly int[] DX = { 1, 1, 0, -1, -1, -1, 0, 1 };
        static readonly int[] DY = { 0, 1, 1, 1, 0, -1, -1, -1 };
        const byte NONE = 255;

        readonly NavGrid _grid;
        readonly int[] _dist;
        readonly byte[] _dir;
        readonly int[] _queue;

        public FlowField(NavGrid grid)
        {
            _grid = grid;
            int n = grid.width * grid.height;
            _dist = new int[n];
            _dir = new byte[n];
            _queue = new int[n];
        }

        public void Rebuild(int targetX, int targetY)
        {
            int w = _grid.width, h = _grid.height, n = w * h;
            for (int i = 0; i < n; i++) { _dist[i] = int.MaxValue; _dir[i] = NONE; }

            if (!_grid.InBounds(targetX, targetY) || _grid.IsBlocked(targetX, targetY))
            {
                FindNearestOpen(ref targetX, ref targetY);
                if (targetX < 0) return;
            }

            int head = 0, tail = 0;
            int start = _grid.Index(targetX, targetY);
            _dist[start] = 0;
            _queue[tail++] = start;

            while (head < tail)
            {
                int cur = _queue[head++];
                int cx = cur % w, cy = cur / w;
                int nd = _dist[cur] + 1;
                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + DX[d], ny = cy + DY[d];
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (_grid.IsBlocked(nx, ny)) continue;
                    // prevent diagonal squeeze through two blockers
                    if ((d & 1) == 1 && (_grid.IsBlocked(cx + DX[d], cy) && _grid.IsBlocked(cx, cy + DY[d])))
                        continue;
                    int ni = ny * w + nx;
                    if (nd < _dist[ni])
                    {
                        _dist[ni] = nd;
                        // direction from neighbour back toward current (i.e. toward target)
                        _dir[ni] = (byte)((d + 4) & 7);
                        _queue[tail++] = ni;
                    }
                }
            }
        }

        void FindNearestOpen(ref int x, ref int y)
        {
            for (int r = 1; r < 8; r++)
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (_grid.InBounds(nx, ny) && !_grid.IsBlocked(nx, ny)) { x = nx; y = ny; return; }
            }
            x = -1; y = -1;
        }

        /// <summary>Unit direction to move from a world position toward the target. Zero if unreachable.</summary>
        public Vector2 SampleDir(Vector2 world)
        {
            _grid.CellOf(world, out int x, out int y);
            if (!_grid.InBounds(x, y)) return Vector2.zero;
            byte d = _dir[_grid.Index(x, y)];
            if (d == NONE) return Vector2.zero;
            var v = new Vector2(DX[d], DY[d]);
            return v.normalized;
        }

        public bool HasPath(Vector2 world)
        {
            _grid.CellOf(world, out int x, out int y);
            return _grid.InBounds(x, y) && _dir[_grid.Index(x, y)] != NONE;
        }
    }
}
