using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>Runtime uGUI builders for the code-constructed menus.
    /// Style: printed political-poster / newsprint — cream paper, ink text, a red
    /// accent, hard letterpress shadows. (Was neon.)</summary>
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

        // A 4x4 white sprite. uGUI Images with a null sprite don't render in this
        // Unity/URP build — every Image we make gets this so it actually draws.
        static Sprite _solid;
        public static Sprite Solid
        {
            get
            {
                if (_solid == null)
                {
                    var t = new Texture2D(4, 4) { name = "uikit_solid" };
                    var px = new Color32[16];
                    for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                    t.SetPixels32(px); t.Apply();
                    _solid = Sprite.Create(t, new UnityEngine.Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                }
                return _solid;
            }
        }

        public static readonly Color Ink    = new Color(0.10f, 0.10f, 0.11f, 1f);
        public static readonly Color Paper  = new Color(0.93f, 0.90f, 0.83f, 1f);
        public static readonly Color Panel  = new Color(0.92f, 0.89f, 0.81f, 0.98f);
        public static readonly Color Card   = new Color(0.86f, 0.83f, 0.74f, 1f);
        public static readonly Color Accent = new Color(0.80f, 0.13f, 0.14f, 1f); // agitprop red
        public static readonly Color Gold   = new Color(0.72f, 0.55f, 0.15f, 1f); // flat mustard
        public static readonly Color Core   = new Color(0.16f, 0.24f, 0.46f, 1f); // flat navy
        public static readonly Color Dim    = new Color(0f, 0f, 0f, 0.7f);

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
            rt.pivot = anchor;                    // pos = position of the anchored corner
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        /// <summary>Stretch horizontally within the parent (leftPad/rightPad insets),
        /// fixed height, positioned yFromTop below the parent's top edge. Keeps panel
        /// content on-screen on any phone width.</summary>
        public static void RowStretch(RectTransform rt, float leftPad, float rightPad, float height, float yFromTop)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(leftPad, -yFromTop - height);
            rt.offsetMax = new Vector2(-rightPad, -yFromTop);
        }

        public static Image Image(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Solid;
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

        /// <summary>Left-aligned text stretched across a row, leaving `rightReserve` px
        /// clear on the right for a button. `top`/`height` measured from the row's top.</summary>
        public static Text Label(string name, RectTransform row, string text, int size,
                                 float top, float height, float rightReserve,
                                 TextAnchor a = TextAnchor.UpperLeft)
        {
            var t = Text(name, row, text, size, a);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = new Vector2(24f, -top - height);
            rt.offsetMax = new Vector2(-rightReserve, -top);
            return t;
        }

        /// <summary>Button pinned to the right edge of a row.</summary>
        public static Button RightButton(string name, RectTransform row, string label, int size,
                                         out Text lbl, float w, float h, float yOff = 0f)
        {
            var b = Button(name, row, label, size, out lbl);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-16f, yOff);
            return b;
        }

        public static Button Button(string name, Transform parent, string label, int size, out Text labelText)
            => Button(name, parent, label, size, out labelText, Paper);

        public static Button Button(string name, Transform parent, string label, int size, out Text labelText, Color tint)
        {
            var rt = Rect(name, parent);
            var border = rt.gameObject.AddComponent<Image>();  // outer ink box
            border.sprite = Solid;
            border.color = Ink;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;

            var fillRt = Rect("Fill", rt);
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(5, 5); fillRt.offsetMax = new Vector2(-5, -5);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = Solid;
            fill.color = Color.Lerp(tint, Paper, 0.35f); // printed-stock, not neon
            fill.raycastTarget = false;

            btn.targetGraphic = fill;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            cb.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.55f);
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            labelText = Text("Label", rt, label, size);
            Stretch((RectTransform)labelText.transform);
            labelText.color = Ink;
            labelText.fontStyle = FontStyle.Bold;
            var sh = labelText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(1f, 1f, 1f, 0.5f); // paper-white letterpress
            sh.effectDistance = new Vector2(1, -2);
            return btn;
        }

        /// <summary>A stamped tag: icon + text on flat stock with an ink edge.</summary>
        public static Text Chip(string name, Transform parent, string text, Color tint, NeonShapeKind glyph)
        {
            var rt = Rect(name, parent);
            var border = rt.gameObject.AddComponent<Image>();
            border.sprite = Solid;
            border.color = Ink;
            border.raycastTarget = false;

            var fillRt = Rect("Fill", rt);
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(4, 4); fillRt.offsetMax = new Vector2(-4, -4);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = Solid;
            fill.color = Color.Lerp(tint, Paper, 0.6f);
            fill.raycastTarget = false;

            var icon = Rect("icon", rt);
            icon.anchorMin = new Vector2(0f, 0.5f); icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(36, 36);
            icon.anchoredPosition = new Vector2(16, 0);
            var ig = icon.gameObject.AddComponent<Image>();
            ig.sprite = NeonArt.Sprite(glyph, 64, 0.12f, 0.5f);
            ig.color = tint;
            ig.raycastTarget = false;

            var t = Text("t", rt, text, 32);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(56, 0); trt.offsetMax = new Vector2(-14, 0);
            t.alignment = TextAnchor.MiddleLeft;
            t.fontStyle = FontStyle.Bold;
            return t;
        }

        /// <summary>Headline placard: heavy ink text, slight tilt, a solid colour bar under it.</summary>
        public static void Title(string text, Transform parent, Vector2 anchor, Vector2 pos, int size, Color tint)
        {
            var holder = Rect("TitleHolder", parent);
            holder.anchorMin = holder.anchorMax = anchor;
            holder.pivot = new Vector2(0.5f, 0.5f);
            holder.sizeDelta = new Vector2(1400, size * 2f);
            holder.anchoredPosition = pos;
            holder.localRotation = Quaternion.Euler(0, 0, -2.5f);

            var bar = Rect("Bar", holder);
            bar.anchorMin = new Vector2(0.5f, 0.5f); bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.pivot = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(text.Length * size * 0.62f + 40f, size * 0.34f);
            bar.anchoredPosition = new Vector2(0, -size * 0.62f);
            var bi = bar.gameObject.AddComponent<Image>();
            bi.sprite = Solid;
            bi.color = tint;
            bi.raycastTarget = false;

            var t = Text("Title", holder, text, size);
            Stretch((RectTransform)t.transform);
            t.color = Ink;
            t.fontStyle = FontStyle.Bold;
            var sh = t.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            sh.effectColor = new Color(1f, 1f, 1f, 0.6f);
            sh.effectDistance = new Vector2(2, -3);
        }
    }
}
