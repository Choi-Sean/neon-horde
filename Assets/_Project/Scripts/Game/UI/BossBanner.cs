using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>Flashes a "BOSS" banner when a boss spawns.</summary>
    public sealed class BossBanner : MonoBehaviour
    {
        public Text label;
        float _t = -1f;

        void Start()
        {
            if (RunManager.Instance != null) RunManager.Instance.OnBossSpawn += Trigger;
            if (label != null) label.color = new Color(1, 1, 1, 0);
        }

        void OnDestroy()
        {
            if (RunManager.Instance != null) RunManager.Instance.OnBossSpawn -= Trigger;
        }

        void Trigger() => _t = 0f;

        void Update()
        {
            if (_t < 0f || label == null) return;
            _t += Time.deltaTime;
            const float dur = 2.2f;
            float a = _t < 0.3f ? _t / 0.3f : Mathf.Clamp01((dur - _t) / 0.6f);
            label.color = new Color(2.4f, 0.3f, 0.3f, a);
            label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, Mathf.Clamp01(_t / 0.4f));
            if (_t >= dur) { _t = -1f; label.color = new Color(1, 1, 1, 0); }
        }
    }
}
