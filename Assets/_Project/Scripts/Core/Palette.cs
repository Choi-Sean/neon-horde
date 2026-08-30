using UnityEngine;

namespace NeonHorde
{
    /// <summary>Central neon palette. Values above 1.0 bloom.</summary>
    public static class Palette
    {
        public static readonly Color Background = new(0.02f, 0.02f, 0.035f, 1f);
        public static readonly Color GridLine   = new Color(0f, 0.55f, 0.85f, 1f) * 1.6f;

        public static readonly Color Player     = new(0.20f, 1.60f, 2.20f, 1f);
        public static readonly Color Enemy      = new(2.20f, 0.25f, 1.10f, 1f);
        public static readonly Color Projectile = new(2.40f, 2.00f, 0.40f, 1f);
        public static readonly Color XpGem      = new(0.35f, 2.20f, 0.70f, 1f);
        public static readonly Color Accent     = new(2.00f, 1.20f, 0.20f, 1f);

        public static Color Hdr(Color baseColor, float intensity) => baseColor * Mathf.Pow(2f, intensity);
    }
}
