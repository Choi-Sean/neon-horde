using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>Shows a one-line control hint on the player's very first run, then fades out.</summary>
    public sealed class FirstRunHint : MonoBehaviour
    {
        Text _label;
        float _t;
        bool _armed;

        void Start()
        {
            if (GameBootstrap.Meta == null || GameBootstrap.Meta.totalRuns > 0) { enabled = false; return; }

            var canvasGo = new GameObject("HintCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            _label = UiKit.Text("Hint", canvasGo.transform, Loc.T("hint_move"), 40);
            UiKit.Place(_label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(980, 120), new Vector2(0, -420));
            _armed = true;
        }

        void Update()
        {
            if (!_armed) return;
            _t += Time.deltaTime;
            float a = _t < 4f ? 1f : Mathf.Clamp01((5.5f - _t) / 1.5f);
            var c = _label.color; c.a = a; _label.color = c;
            if (_t >= 5.5f) { Destroy(_label.canvas.gameObject); _armed = false; }
        }
    }
}
