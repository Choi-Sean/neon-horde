using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Procedural creature sprites: a blobby body with scuttling legs, mandibles and
    /// back-spikes, generated as a short white-silhouette walk cycle (tinted per kind at
    /// draw time). One set of frames per EnemyId, cached. Enough motion that a horde of
    /// them reads as "bugs swarming", not "shapes sliding".
    /// </summary>
    public static class CreatureArt
    {
        public const int Frames = 6;
        const int Size = 96;

        struct Spec
        {
            public float bodyRx, bodyRy;   // body ellipse radii (normalized, ~0.35..0.7)
            public float bodyCy;           // body centre y offset (+ = hunched up)
            public int legs;               // 0,2,4,6
            public float legLen, legThick;
            public float legSpread;        // how far legs reach sideways
            public bool mandibles;
            public int spikes;             // back spikes
            public int eyes;               // 1..3
            public bool wisp;              // tapered ghost tail instead of legs
        }

        static readonly Dictionary<int, Sprite[]> _cache = new();

        public static Sprite[] Get(EnemyId id)
        {
            int k = (int)id;
            if (_cache.TryGetValue(k, out var f)) return f;
            f = Build(SpecFor(id));
            _cache[k] = f;
            return f;
        }

        static Spec SpecFor(EnemyId id) => id switch
        {
            EnemyId.Walker => new Spec { bodyRx = .42f, bodyRy = .5f, bodyCy = .12f, legs = 2, legLen = .5f, legThick = .12f, legSpread = .28f, eyes = 2 },
            EnemyId.Swarmer => new Spec { bodyRx = .5f, bodyRy = .34f, bodyCy = .06f, legs = 4, legLen = .42f, legThick = .1f, legSpread = .5f, mandibles = true, eyes = 2 },
            EnemyId.Tank => new Spec { bodyRx = .68f, bodyRy = .55f, bodyCy = .08f, legs = 4, legLen = .38f, legThick = .18f, legSpread = .62f, spikes = 3, eyes = 2 },
            EnemyId.Spitter => new Spec { bodyRx = .46f, bodyRy = .46f, bodyCy = .16f, legs = 4, legLen = .4f, legThick = .11f, legSpread = .48f, mandibles = true, spikes = 4, eyes = 1 },
            EnemyId.Exploder => new Spec { bodyRx = .58f, bodyRy = .58f, bodyCy = .1f, legs = 2, legLen = .3f, legThick = .13f, legSpread = .3f, eyes = 2 },
            EnemyId.Splitter => new Spec { bodyRx = .55f, bodyRy = .5f, bodyCy = .08f, legs = 4, legLen = .3f, legThick = .12f, legSpread = .45f, eyes = 3 },
            EnemyId.Splitterling => new Spec { bodyRx = .44f, bodyRy = .4f, bodyCy = .06f, legs = 4, legLen = .34f, legThick = .1f, legSpread = .44f, eyes = 1 },
            EnemyId.Phantom => new Spec { bodyRx = .44f, bodyRy = .52f, bodyCy = .14f, legs = 0, wisp = true, eyes = 2 },
            EnemyId.FrostTank => new Spec { bodyRx = .66f, bodyRy = .54f, bodyCy = .08f, legs = 4, legLen = .36f, legThick = .18f, legSpread = .6f, spikes = 6, eyes = 2 },
            _ => new Spec { bodyRx = .45f, bodyRy = .45f, legs = 4, legLen = .4f, legThick = .12f, legSpread = .45f, eyes = 2 },
        };

        static Sprite[] Build(Spec s)
        {
            var frames = new Sprite[Frames];
            for (int f = 0; f < Frames; f++)
            {
                float phase = f / (float)Frames * Mathf.PI * 2f;
                var tex = RenderFrame(s, phase);
                frames[f] = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
                frames[f].name = "creature";
            }
            return frames;
        }

        static Texture2D RenderFrame(Spec s, float phase)
        {
            var px = new float[Size * Size];      // coverage (alpha), max-composited
            var val = new float[Size * Size];     // brightness at that coverage (0.35..1)

            float bx = 0f, by = -s.bodyCy;        // body centre in normalized [-1,1] (y down = +)

            // legs first (behind body)
            if (s.wisp)
            {
                // tapered ghost tail
                for (int t = 0; t < 5; t++)
                {
                    float k = t / 5f;
                    float ty = by + s.bodyRy * 0.7f + k * 0.9f;
                    float tx = bx + Mathf.Sin(phase + k * 3f) * 0.18f * (1f - k);
                    Blob(px, val, tx, ty, (0.18f - k * 0.12f), 0.7f);
                }
            }
            else
            {
                int perSide = s.legs / 2;
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int li = 0; li < perSide; li++)
                    {
                        float t = perSide > 1 ? li / (float)(perSide - 1) : 0.5f;
                        float hipX = bx + side * s.bodyRx * 0.5f;
                        float hipY = by + Mathf.Lerp(-s.bodyRy * 0.3f, s.bodyRy * 0.5f, t);
                        // gait: opposite sides opposite phase, legs along a side staggered
                        float swing = Mathf.Sin(phase + li * 1.1f + (side > 0 ? Mathf.PI : 0f));
                        float footX = hipX + side * s.legSpread + swing * 0.16f;
                        float footY = hipY + s.legLen - Mathf.Abs(swing) * 0.14f;
                        Limb(px, val, hipX, hipY, footX, footY, s.legThick);
                        // little foot
                        Blob(px, val, footX, footY, s.legThick * 1.3f, 0.9f);
                    }
                }
            }

            // back spikes
            for (int i = 0; i < s.spikes; i++)
            {
                float t = s.spikes > 1 ? i / (float)(s.spikes - 1) : 0.5f;
                float sx = bx + Mathf.Lerp(-s.bodyRx * 0.7f, s.bodyRx * 0.7f, t);
                float sy = by - s.bodyRy * 0.75f;
                Limb(px, val, sx, sy, sx + (t - 0.5f) * 0.3f, sy - 0.34f, 0.07f);
            }

            // body
            EllipseFill(px, val, bx, by, s.bodyRx, s.bodyRy);

            // mandibles (front = +y here is "down/toward player"; draw at bottom-front)
            if (s.mandibles)
            {
                float my = by + s.bodyRy * 0.75f;
                Limb(px, val, bx - s.bodyRx * 0.35f, my, bx - s.bodyRx * 0.15f, my + 0.3f, 0.08f);
                Limb(px, val, bx + s.bodyRx * 0.35f, my, bx + s.bodyRx * 0.15f, my + 0.3f, 0.08f);
            }

            // eyes — darker punches so they read against the tint
            for (int i = 0; i < s.eyes; i++)
            {
                float ex = s.eyes == 1 ? bx : bx + (i - (s.eyes - 1) * 0.5f) * (s.bodyRx * 0.7f / Mathf.Max(1, s.eyes - 1));
                float ey = by + s.bodyRy * 0.15f;
                Punch(px, val, ex, ey, 0.11f);
            }

            // rasterize
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var out32 = new Color32[Size * Size];
            for (int i = 0; i < px.Length; i++)
            {
                float a = Mathf.Clamp01(px[i]);
                byte c = (byte)(Mathf.Clamp(a > 0f ? val[i] : 1f, 0.35f, 1f) * 255);
                out32[i] = new Color32(c, c, c, (byte)(a * 255));
            }
            tex.SetPixels32(out32);
            tex.Apply();
            return tex;
        }

        // ---- normalized-space primitives ([-1,1], y grows downward) ----

        static void EllipseFill(float[] px, float[] val, float cx, float cy, float rx, float ry)
        {
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float nx = (x + 0.5f) / Size * 2f - 1f;
                float ny = (y + 0.5f) / Size * 2f - 1f;
                float d = Sq((nx - cx) / rx) + Sq((ny - cy) / ry);
                if (d > 1.3f) continue;
                float edge = Mathf.Clamp01((1f - d) / 0.15f);
                float core = Mathf.Clamp01(1f - d * 0.9f);
                Put(px, val, y * Size + x, edge, Mathf.Lerp(0.6f, 1f, core));
            }
        }

        static void Blob(float[] px, float[] val, float cx, float cy, float r, float bright)
        {
            int x0 = Mathf.Max(0, (int)((cx - r * 1.6f + 1f) * 0.5f * Size));
            int x1 = Mathf.Min(Size - 1, (int)((cx + r * 1.6f + 1f) * 0.5f * Size));
            int y0 = Mathf.Max(0, (int)((cy - r * 1.6f + 1f) * 0.5f * Size));
            int y1 = Mathf.Min(Size - 1, (int)((cy + r * 1.6f + 1f) * 0.5f * Size));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float nx = (x + 0.5f) / Size * 2f - 1f;
                float ny = (y + 0.5f) / Size * 2f - 1f;
                float dd = Mathf.Sqrt(Sq(nx - cx) + Sq(ny - cy)) / r;
                if (dd > 1.2f) continue;
                Put(px, val, y * Size + x, Mathf.Clamp01(1f - dd), bright);
            }
        }

        static void Limb(float[] px, float[] val, float ax, float ay, float bx, float by, float thick)
        {
            int steps = 14;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Blob(px, val, Mathf.Lerp(ax, bx, t), Mathf.Lerp(ay, by, t), thick, 0.85f);
            }
        }

        static void Punch(float[] px, float[] val, float cx, float cy, float r)
        {
            int x0 = Mathf.Max(0, (int)((cx - r * 1.5f + 1f) * 0.5f * Size));
            int x1 = Mathf.Min(Size - 1, (int)((cx + r * 1.5f + 1f) * 0.5f * Size));
            int y0 = Mathf.Max(0, (int)((cy - r * 1.5f + 1f) * 0.5f * Size));
            int y1 = Mathf.Min(Size - 1, (int)((cy + r * 1.5f + 1f) * 0.5f * Size));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float nx = (x + 0.5f) / Size * 2f - 1f;
                float ny = (y + 0.5f) / Size * 2f - 1f;
                float dd = Mathf.Sqrt(Sq(nx - cx) + Sq(ny - cy)) / r;
                if (dd > 1f) continue;
                int idx = y * Size + x;
                if (px[idx] <= 0.01f) continue;           // only darken where there's body
                val[idx] = Mathf.Min(val[idx], 0.15f);    // eye socket
            }
        }

        static void Put(float[] px, float[] val, int i, float a, float bright)
        {
            if (a <= 0f) return;
            if (a > px[i]) { px[i] = a; val[i] = bright; }
        }

        static float Sq(float v) => v * v;
    }
}
