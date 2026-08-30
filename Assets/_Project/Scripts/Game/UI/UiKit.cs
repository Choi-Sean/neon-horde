using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>Runtime uGUI builders for the code-constructed menus (flat neon, no sprites).</summary>
    public static class UiKit
    {
        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                return _font;
            }
        }

        public static readonly Color Ink = Color.white;
        public static readonly Color Panel = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        public static readonly Color Card = new Color(0.09f, 0.11f, 0.16f, 1f);
        public static readonly Color Accent = new Color(0.2f, 1.4f, 1.9f, 1f);
        public static readonly Color Gold = new Color(2.6f, 2.0f, 0.5f, 1f);
        public static readonly Color Core = new Color(2.4f, 0.7f, 2.4f, 1f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Text(string name, Transform parent, string content, int size,
                                TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = content;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Ink;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(string name, Transform parent, string label, int size, out Text labelText)
            => Button(name, parent, label, size, out labelText, Accent);

        public static Button Button(string name, Transform parent, string label, int size, out Text labelText, Color tint)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = NeonArt.Panel(64, 20, 3f, 0.30f, 7f);
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = tint;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = tint;
            cb.highlightedColor = tint * 1.5f;
            cb.pressedColor = tint * 0.7f;
            cb.selectedColor = tint;
            cb.disabledColor = new Color(0.25f, 0.25f, 0.3f, 0.5f);
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            labelText = Text("Label", rt, label, size);
            Stretch((RectTransform)labelText.transform);
            labelText.color = Color.white;
            var sh = labelText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
            sh.effectDistance = new Vector2(0, -2);
            return btn;
        }

        /// <summary>A small pill "chip" (icon + text), e.g. a currency readout.</summary>
        public static Text Chip(string name, Transform parent, string text, Color tint, NeonShapeKind glyph)
        {
            var rt = Rect(name, parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = NeonArt.Panel(48, 24, 2f, 0.35f, 5f);
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = tint * 0.9f;
            bg.raycastTarget = false;

            var icon = Rect("icon", rt);
            icon.anchorMin = new Vector2(0f, 0.5f); icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(40, 40);
            icon.anchoredPosition = new Vector2(18, 0);
            var ig = icon.gameObject.AddComponent<Image>();
            ig.sprite = NeonArt.Sprite(glyph, 64, 0.4f, 0.4f);
            ig.color = tint * 1.6f;
            ig.raycastTarget = false;

            var t = Text("t", rt, text, 32);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(56, 0); trt.offsetMax = new Vector2(-14, 0);
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        /// <summary>Big glowing title: a soft glow blob behind, bright text on top.</summary>
        public static void Title(string text, Transform parent, Vector2 anchor, Vector2 pos, int size, Color tint)
        {
            var glow = Rect("TitleGlow", parent);
            glow.anchorMin = glow.anchorMax = anchor; glow.pivot = new Vector2(0.5f, 0.5f);
            glow.sizeDelta = new Vector2(text.Length * size * 0.85f, size * 3f);
            glow.anchoredPosition = pos;
            var gi = glow.gameObject.AddComponent<Image>();
            gi.sprite = NeonArt.GlowSprite(128, 1.6f);
            gi.color = tint * 0.5f;
            gi.raycastTarget = false;

            var t = Text("Title", parent, text, size);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = trt.anchorMax = anchor; trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(1400, size * 2f);
            trt.anchoredPosition = pos;
            t.color = Color.white;
            t.fontStyle = FontStyle.Bold;
            var outline = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = tint;
            outline.effectDistance = new Vector2(3, 3);
        }
    }
}
