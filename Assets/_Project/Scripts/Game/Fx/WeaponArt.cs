using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Procedural held-weapon silhouettes. Points +X; pivot near the grip so it rotates
    /// to aim. Shape reads by archetype (gun / orb-staff / fork / blade / launcher),
    /// grows with weapon level, and jumps in size/detail on evolution.
    /// </summary>
    public static class WeaponArt
    {
        const int Size = 72;
        static readonly Dictionary<int, Sprite> _cache = new();

        public static Sprite Get(WeaponId id, int level)
        {
            int lv = Mathf.Clamp(level, 1, 8);
            int key = (int)id * 16 + lv;
            if (_cache.TryGetValue(key, out var s)) return s;
            s = Build(SpecFor(id, lv));
            _cache[key] = s;
            return s;
        }

        enum St { Gun, Orb, Fork, Blade, Tube }

        struct Spec { public St style; public float len, thick, tip; public int prongs, coils; public float tilt; public bool dbl, evo; }

        static Spec SpecFor(WeaponId id, int lv)
        {
            float g = lv * 0.022f;  // length growth
            float tg = lv * 0.008f; // tip growth
            switch (id)
            {
                case WeaponId.Bolt:      return new Spec { style = St.Gun, len = .5f + g, thick = .12f, coils = lv / 4 };
                case WeaponId.Railgun:   return new Spec { style = St.Gun, len = .8f + g, thick = .2f, coils = 2 + lv / 3, evo = true };
                case WeaponId.Aura:      return new Spec { style = St.Orb, len = .3f + g * .5f, thick = .1f, tip = .22f + tg };
                case WeaponId.Nova:      return new Spec { style = St.Orb, len = .34f, thick = .13f, tip = .38f + tg, prongs = 3, evo = true };
                case WeaponId.Chain:     return new Spec { style = St.Orb, len = .42f + g * .5f, thick = .09f, tip = .14f + tg, prongs = 2 };
                case WeaponId.Tesla:     return new Spec { style = St.Orb, len = .46f, thick = .12f, tip = .2f + tg, prongs = 3, coils = 3, evo = true };
                case WeaponId.Orbit:     return new Spec { style = St.Fork, len = .5f + g, thick = .1f, prongs = 2 };
                case WeaponId.Halo:      return new Spec { style = St.Fork, len = .48f + g, thick = .12f, tip = .3f + tg, prongs = 0, evo = true };
                case WeaponId.Whip:      return new Spec { style = St.Blade, len = .68f + g, thick = .07f };
                case WeaponId.Reaper:    return new Spec { style = St.Blade, len = .58f + g, thick = .1f, tip = .42f + tg, evo = true };
                case WeaponId.Boomerang: return new Spec { style = St.Blade, len = .4f + g, thick = .12f, tip = .3f, dbl = false };
                case WeaponId.Cyclone:   return new Spec { style = St.Blade, len = .44f, thick = .13f, tip = .38f + tg, dbl = true, evo = true };
                case WeaponId.Seeker:    return new Spec { style = St.Tube, len = .5f + g, thick = .2f, coils = 1 };
                case WeaponId.Swarm:     return new Spec { style = St.Tube, len = .52f, thick = .26f, coils = 3, evo = true };
                case WeaponId.Lob:       return new Spec { style = St.Tube, len = .42f + g, thick = .24f, tilt = 22f, tip = .12f };
                case WeaponId.Meteor:    return new Spec { style = St.Tube, len = .5f, thick = .32f, tilt = 24f, tip = .24f + tg, evo = true };
                default:                 return new Spec { style = St.Gun, len = .55f, thick = .12f };
            }
        }

        static Sprite Build(Spec s)
        {
            var px = new float[Size * Size];
            var val = new float[Size * Size];

            float baseX = -0.55f;                        // grip end
            float tilt = s.tilt * Mathf.Deg2Rad;
            float dx = Mathf.Cos(tilt), dy = Mathf.Sin(tilt);
            float mx = baseX + dx * s.len, my = dy * s.len;   // muzzle

            // grip (down from near the pivot)
            Limb(px, val, baseX + 0.05f, 0f, baseX + 0.05f, -0.32f, 0.09f);
            // main body / barrel
            Limb(px, val, baseX, 0f, mx, my, s.thick);

            switch (s.style)
            {
                case St.Gun:
                    Rect(px, val, baseX + 0.12f, -0.05f, 0.14f, 0.16f);   // receiver
                    for (int c = 0; c < s.coils; c++)
                    {
                        float t = (c + 1) / (float)(s.coils + 1);
                        float cxp = Mathf.Lerp(baseX, mx, t), cyp = Mathf.Lerp(0f, my, t);
                        Rect(px, val, cxp, cyp, s.thick * 1.7f, s.thick * 1.7f);
                    }
                    if (s.evo) Disc(px, val, mx, my, s.thick * 1.6f);
                    break;

                case St.Orb:
                    Disc(px, val, mx, my, s.tip);
                    if (s.tip > 0.02f) Disc(px, val, mx, my, s.tip * 0.5f, 1f);
                    for (int p = 0; p < s.prongs; p++)
                    {
                        float a = (-0.5f + p / Mathf.Max(1f, s.prongs - 1f)) * 1.4f;
                        Limb(px, val, mx, my, mx + Mathf.Cos(a) * (s.tip + 0.18f), my + Mathf.Sin(a) * (s.tip + 0.18f), 0.05f);
                    }
                    for (int c = 0; c < s.coils; c++)
                        Ring(px, val, Mathf.Lerp(baseX, mx, (c + 1) / (float)(s.coils + 1)), 0f, s.thick * 2.2f, 0.04f);
                    break;

                case St.Fork:
                    if (s.tip > 0.02f) Ring(px, val, mx, my, s.tip, 0.07f);
                    for (int p = 0; p < s.prongs; p++)
                    {
                        float off = (p - (s.prongs - 1) * 0.5f) * 0.22f;
                        Limb(px, val, mx - 0.1f, my, mx + 0.24f, my + off, 0.05f);
                    }
                    break;

                case St.Blade:
                    // blade = triangle past the muzzle
                    Tri(px, val, mx, my - s.tip * 0.5f, mx, my + s.tip * 0.5f, mx + s.tip * 1.3f, my);
                    if (s.dbl) Tri(px, val, baseX, -s.tip * 0.5f, baseX, s.tip * 0.5f, baseX - s.tip * 1.1f, 0f);
                    break;

                case St.Tube:
                    Rect(px, val, (baseX + mx) * 0.5f, (0f + my) * 0.5f, s.len * 0.5f, s.thick);
                    for (int b = 0; b < Mathf.Max(1, s.coils); b++)
                    {
                        float off = (b - (Mathf.Max(1, s.coils) - 1) * 0.5f) * s.thick * 0.9f;
                        Ring(px, val, mx, my + off, s.thick * 0.5f, 0.05f);
                    }
                    if (s.tip > 0.02f) Disc(px, val, mx + dx * 0.1f, my + dy * 0.1f, s.tip);
                    break;
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var o = new Color32[Size * Size];
            for (int i = 0; i < px.Length; i++)
            {
                float a = Mathf.Clamp01(px[i]);
                byte c = (byte)(Mathf.Clamp(a > 0f ? val[i] : 1f, 0.45f, 1f) * 255);
                o[i] = new Color32(c, c, c, (byte)(a * 255));
            }
            tex.SetPixels32(o); tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.22f, 0.5f), Size);
            sp.name = "weapon";
            return sp;
        }

        // ---- primitives: normalized [-1,1], x right / y up ----

        static void Put(float[] px, float[] val, int i, float a, float b)
        { if (a > 0f && a > px[i]) { px[i] = a; val[i] = b; } }

        static void Disc(float[] px, float[] val, float cx, float cy, float r, float bright = 0.85f)
        {
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float nx = (x + 0.5f) / Size * 2f - 1f, ny = 1f - (y + 0.5f) / Size * 2f;
                float d = Mathf.Sqrt(Sq(nx - cx) + Sq(ny - cy)) / Mathf.Max(r, 1e-4f);
                if (d > 1.15f) continue;
                Put(px, val, y * Size + x, Mathf.Clamp01((1f - d) / 0.2f), bright);
            }
        }

        static void Ring(float[] px, float[] val, float cx, float cy, float r, float w, float bright = 0.9f)
        {
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float nx = (x + 0.5f) / Size * 2f - 1f, ny = 1f - (y + 0.5f) / Size * 2f;
                float d = Mathf.Abs(Mathf.Sqrt(Sq(nx - cx) + Sq(ny - cy)) - r);
                if (d > w * 1.5f) continue;
                Put(px, val, y * Size + x, Mathf.Clamp01(1f - d / w), bright);
            }
        }

        static void Rect(float[] px, float[] val, float cx, float cy, float hw, float hh, float bright = 0.8f)
        {
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float nx = (x + 0.5f) / Size * 2f - 1f, ny = 1f - (y + 0.5f) / Size * 2f;
                float ex = Mathf.Abs(nx - cx) - hw, ey = Mathf.Abs(ny - cy) - hh;
                if (ex > 0.03f || ey > 0.03f) continue;
                Put(px, val, y * Size + x, Mathf.Clamp01(1f - Mathf.Max(ex, ey) / 0.03f), bright);
            }
        }

        static void Limb(float[] px, float[] val, float ax, float ay, float bx, float by, float thick)
        {
            int steps = 16;
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                Disc(px, val, Mathf.Lerp(ax, bx, t), Mathf.Lerp(ay, by, t), thick, 0.75f);
            }
        }

        static void Tri(float[] px, float[] val, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float minX = Mathf.Min(ax, Mathf.Min(bx, cx)), maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
            float minY = Mathf.Min(ay, Mathf.Min(by, cy)), maxY = Mathf.Max(ay, Mathf.Max(by, cy));
            int x0 = Mathf.Max(0, Px(minX)), x1 = Mathf.Min(Size - 1, Px(maxX));
            int y0 = Mathf.Max(0, Py(maxY)), y1 = Mathf.Min(Size - 1, Py(minY));
            float d = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy);
            if (Mathf.Abs(d) < 1e-5f) return;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float nx = (x + 0.5f) / Size * 2f - 1f, ny = 1f - (y + 0.5f) / Size * 2f;
                float w0 = ((by - cy) * (nx - cx) + (cx - bx) * (ny - cy)) / d;
                float w1 = ((cy - ay) * (nx - cx) + (ax - cx) * (ny - cy)) / d;
                float w2 = 1f - w0 - w1;
                if (w0 >= -0.02f && w1 >= -0.02f && w2 >= -0.02f)
                    Put(px, val, y * Size + x, 1f, 0.85f);
            }
        }

        static int Px(float nx) => Mathf.RoundToInt((nx + 1f) * 0.5f * Size - 0.5f);
        static int Py(float ny) => Mathf.RoundToInt((1f - ny) * 0.5f * Size - 0.5f);
        static float Sq(float v) => v * v;
    }
}
