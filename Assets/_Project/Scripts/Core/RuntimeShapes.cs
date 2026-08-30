using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Generates the handful of primitive sprites the geometric/neon art style needs,
    /// so there is no art import pipeline. Sprites are cached for the session.
    /// </summary>
    public static class RuntimeShapes
    {
        static Sprite _circle;
        static Sprite _square;

        public static Sprite Circle(int diameter = 64, float pixelsPerUnit = 64f)
        {
            if (_circle != null) return _circle;

            var tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float r = diameter * 0.5f;
            var px = new Color32[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            for (int x = 0; x < diameter; x++)
            {
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - dist); // 1px anti-aliased edge
                px[y * diameter + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            _circle.name = "shape_circle";
            return _circle;
        }

        public static Sprite Square(int size = 16, float pixelsPerUnit = 16f)
        {
            if (_square != null) return _square;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            _square.name = "shape_square";
            return _square;
        }
    }
}
