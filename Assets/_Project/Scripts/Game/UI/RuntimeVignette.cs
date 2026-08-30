using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>Builds a full-screen vignette Image at runtime (procedural sprite can't be baked into the scene).</summary>
    public sealed class RuntimeVignette : MonoBehaviour
    {
        void Start()
        {
            var go = new GameObject("Vignette", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.sprite = NeonArt.Vignette(256, 0.55f, 0.30f);
            img.color = Color.white;
            img.raycastTarget = false;
        }
    }
}
