using UnityEngine;

namespace NeonHorde
{
    /// <summary>Global camera trauma. CameraFollow reads Offset() after positioning.</summary>
    public static class ScreenShake
    {
        static float _trauma;
        static float _seed;

        public static void Add(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public static Vector2 Tick(float dt)
        {
            if (_trauma <= 0f) return Vector2.zero;
            _seed += dt * 30f;
            float shake = _trauma * _trauma;
            _trauma = Mathf.Max(0f, _trauma - dt * 1.6f);
            float x = (Mathf.PerlinNoise(_seed, 0f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0f, _seed) - 0.5f) * 2f;
            return new Vector2(x, y) * (shake * 0.6f);
        }
    }
}
