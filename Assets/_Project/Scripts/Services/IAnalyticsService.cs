using System.Text;
using UnityEngine;

namespace NeonHorde
{
    public interface IAnalyticsService
    {
        void Log(string evt, params (string key, object value)[] parameters);
        void SetConsent(bool granted);
    }

    /// <summary>
    /// Writes events to the console. Swap for Unity Gaming Services Analytics (or
    /// Firebase) once the project is linked — see docs/EXTERNAL_SETUP.md.
    /// </summary>
    public sealed class DebugAnalyticsService : IAnalyticsService
    {
        bool _consent = true;

        public void SetConsent(bool granted) => _consent = granted;

        public void Log(string evt, params (string key, object value)[] parameters)
        {
            if (!_consent) return;
            var sb = new StringBuilder("[Analytics] ").Append(evt);
            if (parameters != null)
                foreach (var p in parameters) sb.Append("  ").Append(p.key).Append('=').Append(p.value);
            Debug.Log(sb.ToString());
        }
    }
}
