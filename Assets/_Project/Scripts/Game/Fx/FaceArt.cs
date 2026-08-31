using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Procedural cartoon "angry yelling guy" face — the built-in enemy face for the
    /// joke build when no Resources/art/enemy_face.png is supplied. Deliberately a
    /// generic caricature (orange face, blonde sweep, scowl, open mouth), not a photo.
    /// </summary>
    public static class FaceArt
    {
        static Sprite _cached;

        public static Sprite AngryFace(int size = 160)
        {
            if (_cached != null) return _cached;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

            var skin = new Color(0.98f, 0.58f, 0.28f);
            var skinDark = new Color(0.82f, 0.42f, 0.18f);
            var hair = new Color(0.97f, 0.87f, 0.55f);
            var white = Color.white;
            var dark = new Color(0.08f, 0.05f, 0.05f);
            var mouth = new Color(0.35f, 0.05f, 0.06f);
            var tie = new Color(0.85f, 0.1f, 0.12f);

            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - r) / r;     // -1..1
                float ny = (y + 0.5f - r) / r;
                Color c = new Color(0, 0, 0, 0);

                // head: slightly tall ellipse
                float head = (nx * nx) / (0.74f * 0.74f) + (ny * ny) / (0.86f * 0.86f);
                if (head <= 1f)
                {
                    c = Color.Lerp(skin, skinDark, Mathf.Clamp01(head - 0.35f));

                    // hair: top band with a side sweep + a jagged lower edge
                    float hairLine = 0.34f + 0.12f * Mathf.Sin(nx * 6f) - 0.18f * nx;
                    if (ny > hairLine) c = hair;

                    // eyes (whites) — small, set under the brows
                    float ex = 0.30f, ey = -0.05f, erx = 0.15f, ery = 0.10f;
                    float eL = ((nx + ex) * (nx + ex)) / (erx * erx) + ((ny - ey) * (ny - ey)) / (ery * ery);
                    float eR = ((nx - ex) * (nx - ex)) / (erx * erx) + ((ny - ey) * (ny - ey)) / (ery * ery);
                    if (eL <= 1f || eR <= 1f) c = white;
                    // pupils
                    float pL = ((nx + ex + 0.02f) * (nx + ex + 0.02f)) / 0.0036f + ((ny - ey - 0.01f) * (ny - ey - 0.01f)) / 0.0036f;
                    float pR = ((nx - ex + 0.02f) * (nx - ex + 0.02f)) / 0.0036f + ((ny - ey - 0.01f) * (ny - ey - 0.01f)) / 0.0036f;
                    if (pL <= 1f || pR <= 1f) c = dark;

                    // angry eyebrows (drawn on top of eyes)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float bx = nx * s; // mirror
                        float t0 = (bx - 0.12f) / 0.42f; // 0..1 across the brow
                        if (t0 >= 0f && t0 <= 1f)
                        {
                            float by = 0.12f + t0 * 0.16f; // slopes up toward the ear
                            if (Mathf.Abs(ny - by) < 0.05f) c = dark;
                        }
                    }

                    // mouth: open yell — dark ellipse low-centre
                    float mrx = 0.22f, mry = 0.16f, my = -0.44f;
                    float m = (nx * nx) / (mrx * mrx) + ((ny - my) * (ny - my)) / (mry * mry);
                    if (m <= 1f) c = mouth;
                    if (m <= 0.35f) c = Color.Lerp(mouth, dark, 0.5f);

                    // tie peeking at the very bottom
                    if (ny < -0.80f && Mathf.Abs(nx) < 0.10f - (ny + 0.80f) * 0.6f) c = tie;
                }

                px[y * size + x] = c;
            }
            t.SetPixels(px);
            t.Apply();
            _cached = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            _cached.name = "angry_face";
            return _cached;
        }

        static Sprite _hurtCache;

        /// <summary>
        /// A "hurt" variant of any face sprite, built once at load: radial pinch (scrunch)
        /// + red wince tint + X eyes + gritted mouth. Works on the supplied photo or on
        /// AngryFace(). Falls back to AngryFace() if the source texture isn't readable.
        /// </summary>
        public static Sprite DeriveHurt(Sprite src)
        {
            if (_hurtCache != null) return _hurtCache;
            if (src == null) return AngryFace();

            var st = src.texture;
            int w = Mathf.RoundToInt(src.rect.width);
            int h = Mathf.RoundToInt(src.rect.height);
            int ox = Mathf.RoundToInt(src.rect.x);
            int oy = Mathf.RoundToInt(src.rect.y);
            int tw = st.width;

            Color32[] sp;
            try { sp = st.GetPixels32(); }
            catch { return AngryFace(); }

            var outPx = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                float dx = u - 0.5f, dy = v - 0.5f;
                float len = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float k = Mathf.Pow(Mathf.Clamp01(len), 1.35f);
                float sxN = len > 1e-4f ? 0.5f + dx / len * k * 0.5f : 0.5f;
                float syN = len > 1e-4f ? 0.5f + dy / len * k * 0.5f : 0.5f;
                sxN = 0.5f + (sxN - 0.5f) * 0.88f; // extra horizontal squeeze

                int sx = Mathf.Clamp(Mathf.RoundToInt(sxN * w - 0.5f), 0, w - 1);
                int sy = Mathf.Clamp(Mathf.RoundToInt(syN * h - 0.5f), 0, h - 1);
                Color32 p = sp[(oy + sy) * tw + (ox + sx)];

                float r = Mathf.Min(255f, p.r * 1.18f);
                float g = p.g * 0.62f;
                float b = p.b * 0.55f;
                r = Mathf.Lerp(r, 32f, 0.18f);
                g = Mathf.Lerp(g, 20f, 0.18f);
                b = Mathf.Lerp(b, 20f, 0.18f);
                Color32 o = new Color32((byte)r, (byte)g, (byte)b, p.a);

                // X eyes
                for (int e = -1; e <= 1; e += 2)
                {
                    float ex = 0.5f + e * 0.19f, ey = 0.60f;
                    float ax = u - ex, ay = v - ey;
                    if (Mathf.Abs(ax) < 0.085f && Mathf.Abs(ay) < 0.085f &&
                        (Mathf.Abs(ax - ay) < 0.022f || Mathf.Abs(ax + ay) < 0.022f) && p.a > 8)
                        o = new Color32(15, 8, 8, p.a);
                }
                // gritted mouth
                if (v > 0.24f && v < 0.33f && Mathf.Abs(u - 0.5f) < 0.19f && p.a > 8)
                {
                    bool tooth = ((int)(u * 42f)) % 2 == 0;
                    o = tooth ? new Color32(225, 214, 200, p.a) : new Color32(45, 16, 16, p.a);
                }
                outPx[y * w + x] = o;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(outPx);
            tex.Apply();
            _hurtCache = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), src.pixelsPerUnit);
            _hurtCache.name = "face_hurt";
            return _hurtCache;
        }
    }
}
