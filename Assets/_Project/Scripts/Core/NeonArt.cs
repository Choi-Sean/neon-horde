using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    public enum NeonShapeKind { Disc, Ring, Triangle, Diamond, Hexagon, Square, Bolt, Spark, Star }

    /// <summary>
    /// Procedural neon art: soft-glow textures for entities, VFX, and UI. Everything is
    /// a white RGBA texture (tint applied by material/SpriteRenderer). Cached per key.
    /// Bright core + falloff halo so it reads as "emissive" under Bloom.
    /// </summary>
    public static class NeonArt
    {
        static readonly Dictionary<string, Texture2D> TexCache = new();
        static readonly Dictionary<string, Sprite> SpriteCache = new();

        public static Texture2D Tex(NeonShapeKind kind, int size = 128, float glow = 0.55f, float core = 0.42f)
        {
            string key = $"{kind}:{size}:{glow:0.00}:{core:0.00}";
            if (TexCache.TryGetValue(key, out var t)) return t;
            t = Build(kind, size, glow, core);
            TexCache[key] = t;
            return t;
        }

        public static Sprite Sprite(NeonShapeKind kind, int size = 128, float glow = 0.55f, float core = 0.42f)
        {
            string key = $"S:{kind}:{size}:{glow:0.00}:{core:0.00}";
            if (SpriteCache.TryGetValue(key, out var s)) return s;
            var tex = Tex(kind, size, glow, core);
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            s.name = "neon_" + kind;
            SpriteCache[key] = s;
            return s;
        }

        // ---- capsule / stadium (limbs, torso for the stick figure) ----
        // pivotY: 0 = bottom, 0.5 = centre, ~0.9 = top (limbs swing from the joint).
        public static Sprite Capsule(int w = 24, int h = 96, float glow = 0.14f, float pivotY = 0.5f)
        {
            string key = $"cap:{w}:{h}:{glow:0.00}:{pivotY:0.00}";
            if (SpriteCache.TryGetValue(key, out var s)) return s;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float r = w * 0.5f;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cy = Mathf.Clamp(y + 0.5f, r, h - r);     // nearest point on the core segment
                float d = Mathf.Sqrt(Sq(x + 0.5f - r) + Sq(y + 0.5f - cy)) - (r - 1f);
                float fill = Mathf.Clamp01(0.5f - d);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(0f, d) / (r * (0.4f + glow))), 2.2f);
                float a = Mathf.Max(fill, halo * 0.7f);
                float bright = Mathf.Lerp(0.8f, 1f, fill);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * bright * 255));
            }
            t.SetPixels32(px); t.Apply();
            s = UnityEngine.Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, Mathf.Clamp01(pivotY)), h);
            s.name = "neon_capsule";
            SpriteCache[key] = s;
            return s;
        }

        // ---- radial glow only (halo, muzzle, explosion) ----
        public static Texture2D Glow(int size = 128, float exponent = 2.2f)
        {
            string key = $"glow:{size}:{exponent:0.0}";
            if (TexCache.TryGetValue(key, out var t)) return t;
            t = NewTex(size);
            float r = size * 0.5f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt(Sq(x + 0.5f - r) + Sq(y + 0.5f - r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, exponent);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply();
            TexCache[key] = t;
            return t;
        }

        public static Sprite GlowSprite(int size = 128, float exponent = 2.2f)
        {
            string key = $"Sglow:{size}:{exponent:0.0}";
            if (SpriteCache.TryGetValue(key, out var s)) return s;
            var tex = Glow(size, exponent);
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            SpriteCache[key] = s;
            return s;
        }

        // ---- horizontal 1D gradient (backgrounds, bars) ----
        public static Texture2D VerticalFade(int h = 256, Color top = default, Color bottom = default)
        {
            if (top == default) top = new Color(0.03f, 0.04f, 0.08f, 1f);
            if (bottom == default) bottom = new Color(0.01f, 0.01f, 0.03f, 1f);
            var t = new Texture2D(4, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[4 * h];
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(bottom, top, y / (float)(h - 1));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        /// <summary>Black texture, transparent centre, dark corners — a screen vignette.</summary>
        public static Sprite Vignette(int size = 256, float strength = 0.72f, float inner = 0.35f)
        {
            string key = $"Svig:{size}:{strength:0.00}:{inner:0.00}";
            if (SpriteCache.TryGetValue(key, out var s)) return s;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt(Sq(x + 0.5f - r) + Sq(y + 0.5f - r)) / r; // 0 centre .. ~1.41 corner
                float a = Mathf.Clamp01((d - inner) / (1.42f - inner));
                a = Mathf.Pow(a, 1.6f) * strength;
                px[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply();
            s = UnityEngine.Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            SpriteCache[key] = s;
            return s;
        }

        /// <summary>
        /// Rounded-rect sprite for UI: translucent fill + a bright glowing border.
        /// Returned sprite has a 4-px border set so it 9-slices cleanly.
        /// </summary>
        public static Sprite Panel(int size = 64, int radius = 18, float border = 3f, float fillAlpha = 0.55f, float borderGlow = 6f)
        {
            string key = $"panel:{size}:{radius}:{border}:{fillAlpha:0.00}";
            if (SpriteCache.TryGetValue(key, out var cached)) return cached;

            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float sd = RoundRectSD(x + 0.5f, y + 0.5f, size, radius);
                float fill = sd < 0f ? fillAlpha : 0f;
                float edge = Mathf.Clamp01(1f - Mathf.Abs(sd) / border);
                float glow = Mathf.Exp(-Mathf.Max(0f, sd) / borderGlow) * 0.5f;
                float a = Mathf.Clamp01(Mathf.Max(fill, Mathf.Max(edge, glow)));
                byte br = (byte)(Mathf.Lerp(0.35f, 1f, Mathf.Max(edge, glow)) * 255);
                px[y * size + x] = new Color32(br, br, br, (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply();
            int b = Mathf.Clamp(radius, 6, size / 2 - 2);
            var s = UnityEngine.Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            s.name = "neon_panel";
            SpriteCache[key] = s;
            return s;
        }

        static float RoundRectSD(float x, float y, float size, float radius)
        {
            float hx = size * 0.5f, hy = size * 0.5f;
            float qx = Mathf.Abs(x - hx) - (hx - radius);
            float qy = Mathf.Abs(y - hy) - (hy - radius);
            float outside = Mathf.Sqrt(Sq(Mathf.Max(qx, 0f)) + Sq(Mathf.Max(qy, 0f)));
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - radius;
        }

        static Texture2D Build(NeonShapeKind kind, int size, float glow, float core)
        {
            var t = NewTex(size);
            float r = size * 0.5f;
            var px = new Color32[size * size];

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - r) / r; // -1..1
                float ny = (y + 0.5f - r) / r;
                float sd = SignedDist(kind, nx, ny);           // <0 inside, >0 outside (in normalized units)
                float edge = 1.5f / size;

                float fill = Mathf.Clamp01(0.5f - sd / edge);   // crisp AA edge
                float halo = Mathf.Clamp01(1f - Mathf.Max(0f, sd) / glow);
                halo = Mathf.Pow(halo, 2.4f);

                // bright inner core
                float inner = Mathf.Clamp01((-sd) / core);
                inner = Mathf.Pow(inner, 0.6f);

                float a = Mathf.Max(fill, halo * 0.85f);
                byte rgb = (byte)(255);
                byte alpha = (byte)(Mathf.Clamp01(a) * 255);
                // push the core whiter/brighter (helps Bloom)
                float bright = Mathf.Lerp(0.75f, 1f, Mathf.Max(inner, fill));
                px[y * size + x] = new Color32(rgb, rgb, rgb, (byte)(alpha * bright));
            }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        // signed distance for each shape in normalized [-1,1] space; ~0.72 "radius" so glow fits
        static float SignedDist(NeonShapeKind k, float x, float y)
        {
            const float R = 0.66f;
            switch (k)
            {
                case NeonShapeKind.Disc:    return Mathf.Sqrt(x * x + y * y) - R;
                case NeonShapeKind.Ring:    return Mathf.Abs(Mathf.Sqrt(x * x + y * y) - R) - 0.16f;
                case NeonShapeKind.Square:  return Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) - R * 0.92f;
                case NeonShapeKind.Diamond: return (Mathf.Abs(x) + Mathf.Abs(y)) - R;
                case NeonShapeKind.Triangle: return Tri(x, y, R);
                case NeonShapeKind.Hexagon: return Hex(x, y, R);
                case NeonShapeKind.Bolt:    return Bolt(x, y);
                case NeonShapeKind.Spark:   return Spark(x, y, R);
                case NeonShapeKind.Star:    return Star(x, y, R);
                default:                    return Mathf.Sqrt(x * x + y * y) - R;
            }
        }

        static float Tri(float x, float y, float r)
        {
            const float k = 1.7320508f; // sqrt(3)
            x = Mathf.Abs(x) - r;
            y = y + r / k;
            if (x + k * y > 0f) { float nx = (x - k * y) / 2f; float ny = (-k * x - y) / 2f; x = nx; y = ny; }
            x -= Mathf.Clamp(x, -2f * r, 0f);
            return -Mathf.Sqrt(x * x + y * y) * Mathf.Sign(y);
        }

        static float Hex(float x, float y, float r)
        {
            x = Mathf.Abs(x); y = Mathf.Abs(y);
            float d = Mathf.Max(x * 0.8660254f + y * 0.5f, y) - r;
            return d;
        }

        static float Bolt(float x, float y)
        {
            // lightning-ish: thin diagonal bar with a kink
            float bar = Mathf.Abs(x * 0.7f + y * 0.7f) - 0.12f;
            float len = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) - 0.7f;
            return Mathf.Max(bar, len);
        }

        static float Spark(float x, float y, float r)
        {
            // 4-point spike
            float a = Mathf.Atan2(y, x);
            float rr = Mathf.Sqrt(x * x + y * y);
            float spike = r * (0.35f + 0.65f * Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * a)), 0.5f));
            return rr - spike;
        }

        static float Star(float x, float y, float r)
        {
            float a = Mathf.Atan2(y, x);
            float rr = Mathf.Sqrt(x * x + y * y);
            float pts = 5f;
            float m = Mathf.Cos((Mathf.PI / pts) * Mathf.Floor((pts * a) / Mathf.PI + 0.5f) - a);
            float star = r * Mathf.Lerp(0.42f, 1f, 0.5f + 0.5f * Mathf.Cos(pts * a));
            return rr - star * (0.7f + 0.3f * m);
        }

        static float Sq(float v) => v * v;

        static Texture2D NewTex(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }
    }
}
