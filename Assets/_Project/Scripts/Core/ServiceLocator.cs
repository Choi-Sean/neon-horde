using System;
using System.Collections.Generic;

namespace NeonHorde
{
    /// <summary>
    /// Tiny service registry (save, audio, ads, analytics ...). Deliberately simple
    /// for a solo project; can be swapped for a DI container later without touching call sites.
    /// </summary>
    public static class ServiceLocator
    {
        static readonly Dictionary<Type, object> Map = new();

        public static void Register<T>(T impl) => Map[typeof(T)] = impl;

        public static T Get<T>() => (T)Map[typeof(T)];

        public static bool TryGet<T>(out T value)
        {
            if (Map.TryGetValue(typeof(T), out var o))
            {
                value = (T)o;
                return true;
            }
            value = default;
            return false;
        }

        public static void Clear() => Map.Clear();
    }
}
