using UnityEngine;

namespace NeonHorde
{
    public enum PickupKind : byte { Gem, Gold, Chest }

    /// <summary>
    /// XP gems, gold coins and chests. Gems/coins are magnetised toward the player;
    /// chests must be walked over. Instanced per-kind rendering.
    /// </summary>
    public sealed class PickupManager : MonoBehaviour
    {
        public const int Capacity = 6000;

        struct G
        {
            public Vector2 pos;
            public Vector2 vel;
            public float value;
            public byte kind;
        }

        readonly G[] _g = new G[Capacity];
        int _count;
        public int Count => _count;

        SpritePool _pool;
        Sprite _sprGem, _sprGold, _sprChest;
        static readonly Color GemTint = Palette.XpGem * 1.5f;
        static readonly Color GoldTint = new Color(2.9f, 2.2f, 0.6f, 1f);
        static readonly Color ChestTint = new Color(2.6f, 0.9f, 2.7f, 1f);

        PlayerController _player;
        RunManager _run;

        void Awake()
        {
            _sprGem = NeonArt.Sprite(NeonShapeKind.Diamond, 96, 0.5f, 0.4f);
            _sprGold = NeonArt.Sprite(NeonShapeKind.Disc, 96, 0.5f, 0.45f);
            _sprChest = NeonArt.Sprite(NeonShapeKind.Square, 96, 0.5f, 0.35f);
            _pool = new SpritePool("PickupSprites", 5, transform);
        }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            _run = RunManager.Instance;
        }

        public void ClearAll() => _count = 0;

        void Add(PickupKind kind, Vector2 pos, float value)
        {
            if (_count >= Capacity) return;
            ref G g = ref _g[_count++];
            g.pos = pos;
            g.value = value;
            g.kind = (byte)kind;

            SeededRng rng = _run != null ? _run.Rng : null;
            float a = rng != null ? rng.NextFloat() * Mathf.PI * 2f : 0f;
            float r = rng != null ? Mathf.Sqrt(rng.NextFloat()) * 1.4f : 0f;
            g.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        public void SpawnGem(Vector2 pos, float value) => Add(PickupKind.Gem, pos, value);
        public void SpawnGold(Vector2 pos, int amount) => Add(PickupKind.Gold, pos, amount);
        public void SpawnChest(Vector2 pos) => Add(PickupKind.Chest, pos, 1);

        void RemoveAt(int i) { _count--; if (i != _count) _g[i] = _g[_count]; }

        public void SimTick(float dt)
        {
            if (_player == null || _run == null) return;
            RunState st = _run.State;
            Vector2 pp = _player.transform.position;
            float pickup = Balance.PlayerPickupRadius * st.stats.pickupRadiusMul;
            float pickup2 = pickup * pickup;
            float goldMul = (st.map != null ? st.map.goldMul : 1f) * st.stats.goldMul;

            for (int i = _count - 1; i >= 0; i--)
            {
                ref G g = ref _g[i];
                Vector2 to = pp - g.pos;
                float d2 = to.sqrMagnitude;

                if (d2 < 0.20f)
                {
                    switch ((PickupKind)g.kind)
                    {
                        case PickupKind.Gem: st.AddXp(g.value); break;
                        case PickupKind.Gold: st.goldBanked += Mathf.Max(1, Mathf.RoundToInt(g.value * goldMul)); break;
                        case PickupKind.Chest: st.pendingChests++; break;
                    }
                    RemoveAt(i);
                    continue;
                }

                if ((PickupKind)g.kind != PickupKind.Chest)
                {
                    if (d2 < pickup2) g.vel = Vector2.Lerp(g.vel, to.normalized * 10f, 0.2f);
                    else g.vel = Vector2.Lerp(g.vel, Vector2.zero, 0.08f);
                    g.pos += g.vel * dt;
                }
                else
                {
                    g.vel = Vector2.Lerp(g.vel, Vector2.zero, 0.1f);
                    g.pos += g.vel * dt;
                }
            }
        }

        void LateUpdate()
        {
            if (_pool == null) return;
            _pool.Begin();
            for (int i = 0; i < _count; i++)
            {
                ref G g = ref _g[i];
                switch ((PickupKind)g.kind)
                {
                    case PickupKind.Gem:
                        _pool.Draw(_sprGem, g.pos, -0.05f, GemTint, 0.5f, 45f);
                        break;
                    case PickupKind.Gold:
                        _pool.Draw(_sprGold, g.pos, -0.05f, GoldTint, 0.55f);
                        break;
                    case PickupKind.Chest:
                        _pool.Draw(_sprChest, g.pos, -0.05f, ChestTint, 0.95f, 45f);
                        break;
                }
            }
            _pool.End();
        }
    }
}
