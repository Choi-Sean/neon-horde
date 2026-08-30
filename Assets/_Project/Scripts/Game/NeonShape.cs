using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Player avatar visual: a glowing disc core + a soft halo + a slowly rotating
    /// ring, with an idle pulse. Colour comes from the selected character.
    /// </summary>
    [ExecuteAlways]
    public sealed class NeonShape : MonoBehaviour
    {
        public enum Kind { Circle, Square } // kept for the editor bootstrapper API

        public Kind kind = Kind.Circle;
        [ColorUsage(true, true)] public Color color = new Color(0.3f, 1.7f, 2.3f, 1f);
        public float size = 0.8f;
        public int sortingOrder = 10;

        SpriteRenderer _halo;
        SpriteRenderer _core;
        SpriteRenderer _ring;
        float _t;

        void OnEnable() => Build();

        void OnValidate()
        {
            if (isActiveAndEnabled) Build();
        }

        void Build()
        {
            _halo = Child("Halo", 0);
            _core = Child("Core", 2);
            _ring = Child("Ring", 1);

            _halo.sprite = NeonArt.GlowSprite(128, 2.2f);
            _core.sprite = NeonArt.Sprite(NeonShapeKind.Disc, 128, 0.4f, 0.55f);
            _ring.sprite = NeonArt.Sprite(NeonShapeKind.Ring, 128, 0.35f, 0.2f);

            var c = ColorFromCharacter();
            _halo.color = c * 0.55f;
            _core.color = c;
            _ring.color = c * 1.3f;

            _halo.transform.localScale = Vector3.one * (size * 2.7f);
            _core.transform.localScale = Vector3.one * (size * 1.15f);
            _ring.transform.localScale = Vector3.one * (size * 1.9f);
        }

        Color ColorFromCharacter()
        {
            if (Application.isPlaying && RunManager.Instance != null)
                return CharacterCatalog.Get(RunManager.Instance.State.characterId).color * 1.2f;
            return color;
        }

        SpriteRenderer Child(string n, int order)
        {
            var tr = transform.Find(n);
            var go = tr != null ? tr.gameObject : new GameObject(n);
            go.transform.SetParent(transform, false);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder + order;
            return sr;
        }

        void Update()
        {
            if (_core == null) return;
            _t += Time.deltaTime;
            float pulse = 1f + 0.06f * Mathf.Sin(_t * 4f);
            _core.transform.localScale = Vector3.one * (size * 1.15f * pulse);
            _halo.transform.localScale = Vector3.one * (size * 2.7f * (1f + 0.10f * Mathf.Sin(_t * 2.3f)));
            _ring.transform.localRotation = Quaternion.Euler(0, 0, _t * 40f);
        }
    }
}
