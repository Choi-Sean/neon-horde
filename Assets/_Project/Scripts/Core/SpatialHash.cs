using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Uniform-grid spatial hash rebuilt every simulation tick. Broadphase for
    /// projectile-vs-enemy, weapon targeting, player contact and enemy separation.
    /// Cell lists are pooled to keep Clear() allocation-free.
    /// </summary>
    public sealed class SpatialHash
    {
        readonly float _cellSize;
        readonly Dictionary<long, List<int>> _cells = new();
        readonly Stack<List<int>> _listPool = new();

        public SpatialHash(float cellSize) => _cellSize = Mathf.Max(0.01f, cellSize);

        static long Key(int cx, int cy) => ((long)cx << 32) ^ (uint)cy;

        int ToCell(float v) => Mathf.FloorToInt(v / _cellSize);

        public void Clear()
        {
            foreach (var kv in _cells)
            {
                kv.Value.Clear();
                _listPool.Push(kv.Value);
            }
            _cells.Clear();
        }

        public void Insert(int id, Vector2 p)
        {
            long k = Key(ToCell(p.x), ToCell(p.y));
            if (!_cells.TryGetValue(k, out var list))
            {
                list = _listPool.Count > 0 ? _listPool.Pop() : new List<int>(8);
                _cells[k] = list;
            }
            list.Add(id);
        }

        /// <summary>Appends every id whose cell overlaps the query circle's AABB into <paramref name="results"/>.</summary>
        public void Query(Vector2 center, float radius, List<int> results)
        {
            results.Clear();
            int minX = ToCell(center.x - radius), maxX = ToCell(center.x + radius);
            int minY = ToCell(center.y - radius), maxY = ToCell(center.y + radius);
            for (int cx = minX; cx <= maxX; cx++)
            for (int cy = minY; cy <= maxY; cy++)
                if (_cells.TryGetValue(Key(cx, cy), out var list))
                    results.AddRange(list);
        }
    }
}
