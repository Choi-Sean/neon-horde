using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>
    /// Menu background: a printed-poster / political-cartoon look — off-white paper with
    /// a halftone dot screen, a bold red diagonal banner, and a slow paper drift. Builds
    /// its own full-screen canvas behind everything. (Replaces the old neon grid.)
    /// </summary>
    public sealed class MenuBackdrop : MonoBehaviour
    {
        RawImage _halftone;
        RectTransform _banner;
        float _t;

        static readonly Color Paper = new Color(0.93f, 0.90f, 0.83f, 1f);
        static readonly Color Ink = new Color(0.09f, 0.09f, 0.10f, 1f);
        static readonly Color Red = new Color(0.80f, 0.13f, 0.14f, 1f);

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

            var bg = New("Paper", root);
            Stretch(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = Paper;
            bgImg.raycastTarget = false;

            // thin red diagonal banner behind the title
            _banner = New("Banner", root);
            _banner.anchorMin = _banner.anchorMax = new Vector2(0.5f, 0.9f);
            _banner.sizeDelta = new Vector2(2600, 150);
            _banner.localRotation = Quaternion.Euler(0, 0, -9f);
            var bnImg = _banner.gameObject.AddComponent<Image>();
            bnImg.color = Red;
            bnImg.raycastTarget = false;

            // halftone dot screen over everything (subtle)
            var ht = New("Halftone", root);
            Stretch(ht);
            _halftone = ht.gameObject.AddComponent<RawImage>();
            _halftone.texture = HalftoneTex();
            _halftone.color = new Color(Ink.r, Ink.g, Ink.b, 0.10f);
            _halftone.uvRect = new Rect(0, 0, 26, 46);
            _halftone.raycastTarget = false;

            // a faint second ink wash bottom for weight
            var vig = New("BottomWash", root);
            vig.anchorMin = new Vector2(0, 0); vig.anchorMax = new Vector2(1, 0.28f);
            vig.offsetMin = vig.offsetMax = Vector2.zero;
            var vg = vig.gameObject.AddComponent<Image>();
            vg.color = new Color(Ink.r, Ink.g, Ink.b, 0.06f);
            vg.raycastTarget = false;
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_halftone != null)
            {
                var r = _halftone.uvRect;
                r.x += Time.deltaTime * 0.015f;
                r.y -= Time.deltaTime * 0.010f;
                _halftone.uvRect = r;
            }
            if (_banner != null)
                _banner.anchoredPosition = new Vector2(Mathf.Sin(_t * 0.25f) * 30f, Mathf.Sin(_t * 0.4f) * 8f);
        }

        static Texture2D HalftoneTex()
        {
            const int c = 32;
            var t = new Texture2D(c, c, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[c * c];
            float r = c * 0.5f;
            for (int y = 0; y < c; y++)
            for (int x = 0; x < c; x++)
            {
                float d = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f));
                float a = d < c * 0.30f ? 1f : 0f;
                px[y * c + x] = new Color(1f, 1f, 1f, a);
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        static RectTransform New(string n, Transform p)
        {
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
