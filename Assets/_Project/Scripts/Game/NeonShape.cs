using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Player avatar: a neon stick figure — head, torso, two arms, two legs, built from
    /// capsule sprites. Runs a walk cycle from its own velocity, bobs, leans and faces
    /// the way it moves; breathes when idle. Colour comes from the selected character.
    /// (Name kept for the editor bootstrapper API.)
    /// </summary>
    [ExecuteAlways]
    public sealed class NeonShape : MonoBehaviour
    {
        public enum Kind { Circle, Square } // kept for the bootstrapper API

        public Kind kind = Kind.Circle;
        [ColorUsage(true, true)] public Color color = new Color(0.3f, 1.7f, 2.3f, 1f);
        public float size = 0.8f;
        public int sortingOrder = 10;

        Transform _rig;         // flips + leans
        Transform _bob;         // vertical bob
        SpriteRenderer _head, _torso, _armL, _armR, _legL, _legR, _glow;
        Vector3 _lastPos;
        float _cycle, _speedSmooth, _facing = 1f;
        Vector2? _aim;

        /// <summary>World point the player is aiming at (nearest enemy). Null = not aiming.</summary>
        public void SetAim(Vector2? worldTarget) => _aim = worldTarget;
        public float Scale => size;
        public Vector3 ShoulderWorld =>
            transform.position + new Vector3(_facing * 0.13f * size, 0.5f * size, -0.05f);

        void OnEnable()
        {
            Build();
            if (Application.isPlaying && GetComponent<PlayerWeaponRig>() == null)
                gameObject.AddComponent<PlayerWeaponRig>();
        }
        void OnValidate() { if (isActiveAndEnabled) Build(); }

        void Build()
        {
            // scrub legacy children from the old halo/core/ring avatar the scene may
            // still have serialized (runtime only — DestroyImmediate is illegal in OnValidate)
            if (Application.isPlaying)
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    var ch = transform.GetChild(i);
                    if (ch.name != "Rig") Destroy(ch.gameObject);
                }

            var rigT = transform.Find("Rig");
            if (rigT == null)
            {
                _rig = new GameObject("Rig").transform;
                _rig.SetParent(transform, false);
            }
            else _rig = rigT;

            var bobT = _rig.Find("Bob");
            _bob = bobT != null ? bobT : new GameObject("Bob").transform;
            _bob.SetParent(_rig, false);

            var torsoCap = NeonArt.Capsule(24, 96, 0.16f, 0.5f);
            var limbCap = NeonArt.Capsule(20, 96, 0.16f, 0.9f);   // pivot near top → swings from joint
            var disc = NeonArt.Sprite(NeonShapeKind.Disc, 96, 0.35f, 0.5f);
            var glow = NeonArt.GlowSprite(128, 2.2f);

            _glow = Part("Glow", _bob, glow, 0);
            _legL = Part("LegL", _bob, limbCap, 1);
            _legR = Part("LegR", _bob, limbCap, 1);
            _torso = Part("Torso", _bob, torsoCap, 2);
            _armL = Part("ArmL", _bob, limbCap, 2);
            _armR = Part("ArmR", _bob, limbCap, 2);
            _head = Part("Head", _bob, disc, 3);

            var c = ColorFromCharacter();
            _glow.color = c * 0.4f;
            _head.color = _torso.color = c;
            _armL.color = _armR.color = c * 0.92f;
            _legL.color = _legR.color = c * 0.85f;

            float S = size;
            _glow.transform.localScale = Vector3.one * (S * 3.4f);

            _torso.transform.localScale = new Vector3(S * 0.34f, S * 1.05f, 1f);
            _torso.transform.localPosition = new Vector3(0f, S * 0.12f, 0f);

            _head.transform.localScale = Vector3.one * (S * 0.6f);
            _head.transform.localPosition = new Vector3(0f, S * 0.88f, 0f);

            // limbs: localPosition = joint, localScale = (thickness, length). They hang
            // straight down at rotation 0 (top-pivoted capsule) and swing from the joint.
            _armL.transform.localPosition = new Vector3(-S * 0.13f, S * 0.5f, 0f);
            _armR.transform.localPosition = new Vector3(S * 0.13f, S * 0.5f, 0f);
            _legL.transform.localPosition = new Vector3(-S * 0.09f, -S * 0.18f, 0f);
            _legR.transform.localPosition = new Vector3(S * 0.09f, -S * 0.18f, 0f);
            _armL.transform.localScale = _armR.transform.localScale = new Vector3(S * 0.2f, S * 0.8f, 1f);
            _legL.transform.localScale = _legR.transform.localScale = new Vector3(S * 0.22f, S * 0.9f, 1f);

            _lastPos = transform.position;
        }

        SpriteRenderer Part(string n, Transform parent, Sprite sprite, int order)
        {
            var t = parent.Find(n);
            var go = t != null ? t.gameObject : new GameObject(n);
            go.transform.SetParent(parent, false);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder + order;
            return sr;
        }

        Color ColorFromCharacter()
        {
            if (Application.isPlaying && RunManager.Instance != null)
                return CharacterCatalog.Get(RunManager.Instance.State.characterId).color * 1.25f;
            return color;
        }

        void Update()
        {
            if (_bob == null || _rig == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            Vector3 p = transform.position;
            Vector2 vel = (p - _lastPos) / dt;
            _lastPos = p;

            float spd = vel.magnitude;
            _speedSmooth = Mathf.Lerp(_speedSmooth, spd, 1f - Mathf.Exp(-10f * dt));
            bool moving = _speedSmooth > 0.4f;

            // face the aim if aiming, else the travel direction
            float dirX = _aim.HasValue ? (_aim.Value.x - transform.position.x) : vel.x;
            if (Mathf.Abs(dirX) > 0.12f) _facing = Mathf.Sign(dirX);
            _rig.localScale = new Vector3(_facing, 1f, 1f);

            float t = Time.time;
            bool aiming = _aim.HasValue;

            // legs: walk cycle when moving, planted otherwise
            if (moving)
            {
                _cycle += dt * (7f + _speedSmooth * 1.6f);
                float sw = Mathf.Sin(_cycle);
                _legL.transform.localRotation = Quaternion.Euler(0, 0, sw * 40f);
                _legR.transform.localRotation = Quaternion.Euler(0, 0, -sw * 40f);
                _bob.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_cycle * 2f)) * 0.06f * size, 0f);
                _rig.localRotation = Quaternion.Euler(0, 0, -_facing * 9f);
            }
            else
            {
                _legL.transform.localRotation = Quaternion.Euler(0, 0, 7f);
                _legR.transform.localRotation = Quaternion.Euler(0, 0, -7f);
                _bob.localPosition = new Vector3(0f, Mathf.Sin(t * 2.2f) * 0.015f * size, 0f);
                _rig.localRotation = Quaternion.Lerp(_rig.localRotation, Quaternion.identity, 6f * dt);
            }

            // arms: present the weapon forward when aiming, otherwise swing / rest
            if (aiming)
            {
                _armR.transform.localRotation = Quaternion.Euler(0, 0, -70f);
                _armL.transform.localRotation = Quaternion.Euler(0, 0, -46f);
            }
            else if (moving)
            {
                float sw = Mathf.Sin(_cycle);
                _armL.transform.localRotation = Quaternion.Euler(0, 0, 14f - sw * 34f);
                _armR.transform.localRotation = Quaternion.Euler(0, 0, -14f + sw * 34f);
            }
            else
            {
                _armL.transform.localRotation = Quaternion.Euler(0, 0, 16f);
                _armR.transform.localRotation = Quaternion.Euler(0, 0, -16f);
            }

            if (_glow != null)
                _glow.transform.localScale = Vector3.one * (size * 3.4f * (1f + 0.08f * Mathf.Sin(t * 2.5f)));
        }
    }
}
