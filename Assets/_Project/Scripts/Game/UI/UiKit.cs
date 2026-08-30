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
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.12f, 0.18f, 0.26f, 1f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors; cb.highlightedColor = new Color(0.2f, 0.3f, 0.44f, 1f);
            cb.pressedColor = new Color(0.1f, 0.14f, 0.2f, 1f); btn.colors = cb;
            labelText = Text("Label", rt, label, size);
            Stretch((RectTransform)labelText.transform);
            return btn;
        }
    }
}
