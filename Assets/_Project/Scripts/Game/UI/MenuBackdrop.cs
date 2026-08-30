using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>
    /// Animated menu background: a slowly scrolling neon grid + a few drifting glow
    /// blobs. Builds its own full-screen canvas behind everything.
    /// </summary>
    public sealed class MenuBackdrop : MonoBehaviour
    {
        RawImage _grid;
        RectTransform[] _blobs;
        float _t;

        void Start()
        {
            var canvasGo = new GameObject("BackdropCanvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            var root = (RectTransform)canvasGo.transform;

            var bg = New("BG", root, out var bgImg);
            Stretch(bg);
            bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.015f, 0.02f, 0.04f, 1f);
            bgImg.raycastTarget = false;

            var gridGo = New("Grid", root, out _);
            Stretch(gridGo);
            _grid = gridGo.gameObject.AddComponent<RawImage>();
            _grid.texture = GridTex();
            _grid.color = new Color(1f, 1f, 1f, 0.5f);
            _grid.uvRect = new Rect(0, 0, 12, 21);
            _grid.raycastTarget = false;

            _blobs = new RectTransform[3];
            var cols = new[]
            {
                new Color(0.2f, 1.2f, 2.2f, 0.5f),
                new Color(2.0f, 0.4f, 1.6f, 0.4f),
                new Color(0.4f, 2.0f, 1.4f, 0.35f)
            };
            var glow = NeonArt.GlowSprite(128, 2f);
            for (int i = 0; i < 3; i++)
            {
                var b = New("Blob" + i, root, out var img);
                img = b.gameObject.AddComponent<Image>();
                img.sprite = glow;
                img.color = cols[i];
                img.raycastTarget = false;
                b.sizeDelta = new Vector2(900 + i * 200, 900 + i * 200);
                _blobs[i] = b;
            }
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_grid != null)
            {
                var r = _grid.uvRect;
                r.x += Time.deltaTime * 0.06f;
                r.y += Time.deltaTime * 0.03f;
                _grid.uvRect = r;
            }
            for (int i = 0; i < _blobs.Length; i++)
            {
                float ph = _t * (0.12f + i * 0.05f) + i * 2f;
                _blobs[i].anchoredPosition = new Vector2(Mathf.Sin(ph) * 360f, Mathf.Cos(ph * 0.8f) * 620f);
            }
        }

        static Texture2D GridTex()
        {
            const int c = 64;
            var t = new Texture2D(c, c, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var line = new Color(0.15f, 0.5f, 0.9f, 1f);
            var px = new Color[c * c];
            for (int y = 0; y < c; y++)
            for (int x = 0; x < c; x++)
            {
                float d = Mathf.Min(Mathf.Min(x, c - x), Mathf.Min(y, c - y));
                float a = d < 1f ? 0.9f : Mathf.Exp(-d * 0.5f) * 0.4f;
                px[y * c + x] = new Color(line.r, line.g, line.b, a);
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        static RectTransform New(string n, Transform p, out Image _)
        {
            _ = null;
            var go = new GameObject(n, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(p, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
