using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Pooled floating damage numbers (TextMesh, no canvas). Rate-capped per frame so a
    /// dense horde can't flood it. Disabled by the low-quality toggle.
    /// </summary>
    public sealed class DamageNumberSystem : MonoBehaviour
    {
        const int PoolSize = 80;
        const int MaxSpawnsPerFrame = 6;
        const int ShoutPoolSize = 28;

        struct N { public Transform tr; public TextMesh tm; public float t; public float life; public Vector3 vel; public bool active; }

        N[] _pool;
        N[] _shouts;
        int _spawnsThisFrame;
        int _shoutsThisFrame;
        public bool Enabled = true;

        void Start() => Enabled = GameConfig.DamageNumbers;

        void Awake()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _pool = MakePool("dmg", PoolSize, font, 48);
            _shouts = MakePool("shout", ShoutPoolSize, font, 40);
        }

        N[] MakePool(string name, int size, Font font, int fontSize)
        {
            var arr = new N[size];
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform, false);
                var tm = go.AddComponent<TextMesh>();
                tm.font = font;
                if (font != null) go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                tm.fontSize = fontSize;
                tm.characterSize = 0.06f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = Color.white;
                go.SetActive(false);
                arr[i] = new N { tr = go.transform, tm = tm };
            }
            return arr;
        }

        void LateUpdate()
        {
            _spawnsThisFrame = 0;
            _shoutsThisFrame = 0;
            float dt = Time.deltaTime;
            Tick(_pool, dt);
            Tick(_shouts, dt);
        }

        static void Tick(N[] pool, float dt)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                ref N n = ref pool[i];
                if (!n.active) continue;
                n.t += dt;
                n.tr.position += n.vel * dt;
                n.vel *= (1f - 2f * dt);
                float k = 1f - n.t / n.life;
                var c = n.tm.color; c.a = Mathf.Clamp01(k); n.tm.color = c;
                if (n.t >= n.life) { n.active = false; n.tr.gameObject.SetActive(false); }
            }
        }

        /// <summary>A short spoken line above an enemy ("WAAAGH!", "OW!"). Independent of
        /// the damage-number toggle; hard rate-capped so a horde can't flood it.</summary>
        public void Shout(Vector3 worldPos, string text, Color color, int fontSize = 40, float life = 0.8f)
        {
            if (_shoutsThisFrame >= 3) return;
            for (int i = 0; i < _shouts.Length; i++)
            {
                ref N n = ref _shouts[i];
                if (n.active) continue;
                n.active = true;
                n.t = 0f;
                n.life = life;
                n.vel = new Vector3(Random.Range(-0.3f, 0.3f), 1.3f, 0f);
                n.tr.position = worldPos + Vector3.up * 0.7f;
                n.tr.gameObject.SetActive(true);
                n.tm.text = text;
                n.tm.color = color;
                n.tm.fontSize = fontSize;
                _shoutsThisFrame++;
                return;
            }
        }

        public void Report(Vector3 worldPos, float amount, bool crit = false)
        {
            if (!Enabled || _spawnsThisFrame >= MaxSpawnsPerFrame) return;
            for (int i = 0; i < _pool.Length; i++)
            {
                ref N n = ref _pool[i];
                if (n.active) continue;
                n.active = true;
                n.t = 0f;
                n.life = crit ? 0.75f : 0.55f;
                n.vel = new Vector3(Random.Range(-0.6f, 0.6f), 2.4f, 0f);
                n.tr.position = worldPos + Vector3.up * 0.4f;
                n.tr.gameObject.SetActive(true);
                n.tm.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
                n.tm.color = crit ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(1f, 1f, 1f, 1f);
                n.tm.fontSize = crit ? 64 : 48;
                _spawnsThisFrame++;
                return;
            }
        }
    }
}
