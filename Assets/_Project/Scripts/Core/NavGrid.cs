using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Rasterized static terrain for one run. Cell flags feed pathfinding, player
    /// collide-and-slide, and hazard/slow zone effects.
    /// </summary>
    public sealed class NavGrid
    {
        [System.Flags]
        public enum Flag : byte { Open = 0, Blocked = 1, Slow = 2, Hazard = 4 }

        public readonly int width;
        public readonly int height;
        public readonly float cellSize;
        public readonly Vector2 origin; // world position of cell (0,0) corner
        public readonly byte[] flags;

        public NavGrid(int width, int height, float cellSize, Vector2 origin)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.origin = origin;
            flags = new byte[width * height];
        }

        public int Index(int x, int y) => y * width + x;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public void CellOf(Vector2 world, out int x, out int y)
        {
            x = Mathf.FloorToInt((world.x - origin.x) / cellSize);
            y = Mathf.FloorToInt((world.y - origin.y) / cellSize);
        }

        public Vector2 CellCenter(int x, int y)
            => new Vector2(origin.x + (x + 0.5f) * cellSize, origin.y + (y + 0.5f) * cellSize);

        public byte FlagsAt(Vector2 world)
        {
            CellOf(world, out int x, out int y);
            return InBounds(x, y) ? flags[Index(x, y)] : (byte)Flag.Blocked;
        }

        public bool IsBlocked(int x, int y)
            => !InBounds(x, y) || (flags[Index(x, y)] & (byte)Flag.Blocked) != 0;

        public bool IsBlockedWorld(Vector2 world)
        {
            CellOf(world, out int x, out int y);
            return IsBlocked(x, y);
        }

        public void SetFlag(int x, int y, Flag f, bool on)
        {
            if (!InBounds(x, y)) return;
            int i = Index(x, y);
            if (on) flags[i] |= (byte)f;
            else flags[i] &= (byte)~(byte)f;
        }

        public void ClearBlockedWorld(Vector2 world)
        {
            CellOf(world, out int x, out int y);
            SetFlag(x, y, Flag.Blocked, false);
        }

        public Vector2 Min => origin;
        public Vector2 Max => origin + new Vector2(width * cellSize, height * cellSize);
    }
}
