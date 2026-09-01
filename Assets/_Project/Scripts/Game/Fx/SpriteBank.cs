using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Real-art lookup. PNGs live in Assets/_Project/Resources/gfx/ (produced from the
    /// AI images by the de-checker pipeline). Keys: "char_pulse", "enemy_walker",
    /// "boss", "wpn_bolt", ... Missing key -> null, caller falls back to procedural.
    /// </summary>
    public static class SpriteBank
    {
        static readonly Dictionary<string, Sprite> _cache = new();

        public static Sprite Get(string key)
        {
            if (_cache.TryGetValue(key, out var s)) return s;
            s = Resources.Load<Sprite>("gfx/" + key);
            _cache[key] = s;
            return s;
        }

        public static bool Has(string key) => Get(key) != null;
    }
}
