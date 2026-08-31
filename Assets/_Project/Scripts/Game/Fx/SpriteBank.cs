using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Optional real-art lookup. Drop PNGs (imported as Sprite) into
    /// Assets/_Project/Resources/art/ and they override the procedural shapes by key,
    /// e.g. "enemy_face", "player_pulse", "ground_grid". Missing key -> null, caller
    /// falls back to NeonArt.
    /// </summary>
    public static class SpriteBank
    {
        static readonly Dictionary<string, Sprite> _cache = new();

        public static Sprite Get(string key)
        {
            if (_cache.TryGetValue(key, out var s)) return s;
            s = Resources.Load<Sprite>("art/" + key);
            _cache[key] = s;
            return s;
        }

        public static bool Has(string key) => Get(key) != null;
    }
}
