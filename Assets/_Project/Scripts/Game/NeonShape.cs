using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Assigns a generated primitive sprite + HDR tint to a SpriteRenderer so entities
    /// render as glowing neon shapes with no art assets.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class NeonShape : MonoBehaviour
    {
        public enum Kind { Circle, Square }

        public Kind kind = Kind.Circle;
        [ColorUsage(true, true)] public Color color = Color.cyan;
        public float size = 1f;
        public int sortingOrder;

        void OnEnable() => Apply();

        void OnValidate()
        {
            if (isActiveAndEnabled) Apply();
        }

        public void Apply()
        {
            var sr = GetComponent<SpriteRenderer>();
            sr.sprite = kind == Kind.Circle ? RuntimeShapes.Circle() : RuntimeShapes.Square();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            transform.localScale = Vector3.one * size;
        }
    }
}
