using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Pooled additive-sprite VFX: bursts, sparks, expanding rings, flashes.
    /// One instanced-ish batch via many pooled SpriteRenderers (cheap enough for the
    /// counts a run produces; capped).
    /// </summary>
    public sealed class NeonVfx : MonoBehaviour
    {
        public static NeonVfx Instance { get; private set; }

        const int Pool = 700;

        struct P
        {
            public Transform tr;
            public SpriteRenderer sr;
            public Vector2 vel;
            public float t, life, startScale, endScale;
            public Color color;
            public bool spin;
            public bool active;
        }

        P[] _p;
        int _cursor;
        Sprite _glow, _ring, _spark;

        void Awake()
        {
            Instance = this;
            _glow = NeonArt.GlowSprite(96, 2f);
            _ring = NeonArt.Sprite(NeonShapeKind.Ring, 96, 0.3f, 0.15f);
            _spark = NeonArt.Sprite(NeonShapeKind.Diamond, 64, 0.5f, 0.35f);

            _p = new P[Pool];
            for (int i = 0; i < Pool; i++)
            {
                var go = new GameObject("vfx");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 40;
                go.SetActive(false);
                _p[i] = new P { tr = go.transform, sr = sr };
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        ref P Next()
        {
            for (int n = 0; n < Pool; n++)
            {
                _cursor = (_cursor + 1) % Pool;
                if (!_p[_cursor].active) return ref _p[_cursor];
            }
            return ref _p[_cursor]; // steal oldest
        }

        void Emit(Sprite s, Vector3 pos, Vector2 vel, float life, float s0, float s1, Color col, bool spin)
        {
            ref P p = ref Next();
            p.active = true;
            p.t = 0f; p.life = life;
            p.startScale = s0; p.endScale = s1;
            p.color = col; p.vel = vel; p.spin = spin;
            p.sr.sprite = s;
            p.sr.color = col;
            p.tr.position = pos;
            p.tr.localScale = Vector3.one * s0;
            p.tr.localRotation = spin ? Quaternion.Euler(0, 0, Random.value * 360f) : Quaternion.identity;
            p.tr.gameObject.SetActive(true);
        }

        // ---- public API ----

        public void Burst(Vector3 pos, Color color, float scale = 1f, int count = 8)
        {
            Emit(_glow, pos, Vector2.zero, 0.28f, scale * 1.2f, scale * 2.6f, color * 1.4f, false); // flash
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + Random.value;
                float spd = Random.Range(3f, 8f) * scale;
                Emit(_spark, pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spd,
                    Random.Range(0.35f, 0.6f), scale * 0.5f, 0.01f, color * 1.6f, true);
            }
        }

        public void Spark(Vector3 pos, Color color, float scale = 1f)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 v = Random.insideUnitCircle.normalized * Random.Range(2f, 5f) * scale;
                Emit(_spark, pos, v, 0.22f, scale * 0.35f, 0.01f, color * 1.5f, true);
            }
        }

        public void Ring(Vector3 pos, Color color, float radius, float life = 0.4f)
        {
            Emit(_ring, pos, Vector2.zero, life, 0.2f, radius * 2f, color * 1.5f, false);
        }

        public void Flash(Vector3 pos, Color color, float size = 1f, float life = 0.15f)
        {
            Emit(_glow, pos, Vector2.zero, life, size * 0.4f, size * 1.6f, color * 1.8f, false);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < Pool; i++)
            {
                ref P p = ref _p[i];
                if (!p.active) continue;
                p.t += dt;
                float k = p.t / p.life;
                if (k >= 1f) { p.active = false; p.tr.gameObject.SetActive(false); continue; }

                p.tr.position += (Vector3)(p.vel * dt);
                p.vel *= (1f - 3f * dt);
                float sc = Mathf.Lerp(p.startScale, p.endScale, k);
                p.tr.localScale = Vector3.one * sc;
                var c = p.color; c.a = 1f - k; p.sr.color = c;
                if (p.spin) p.tr.localRotation *= Quaternion.Euler(0, 0, 360f * dt);
            }
        }
    }
}
