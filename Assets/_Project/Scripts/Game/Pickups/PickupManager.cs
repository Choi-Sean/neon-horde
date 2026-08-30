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

        Mesh _quad;
        Material _matGem, _matGold, _matChest;
        Matrix4x4[] _mGem, _mGold, _mChest;
        int _nGem, _nGold, _nChest;

        PlayerController _player;
        RunManager _run;

        void Awake()
        {
            _quad = NeonMesh.Quad;
            _matGem = NeonMesh.NewGlow(Palette.XpGem * 1.5f, NeonArt.Tex(NeonShapeKind.Diamond, 96, 0.5f, 0.4f));
            _matGold = NeonMesh.NewGlow(new Color(2.9f, 2.2f, 0.6f, 1f), NeonArt.Tex(NeonShapeKind.Disc, 96, 0.5f, 0.45f));
            _matChest = NeonMesh.NewGlow(new Color(2.6f, 0.9f, 2.7f, 1f), NeonArt.Tex(NeonShapeKind.Square, 96, 0.5f, 0.35f));
            _mGem = new Matrix4x4[Capacity];
            _mGold = new Matrix4x4[Capacity];
            _mChest = new Matrix4x4[256];
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
            _nGem = _nGold = _nChest = 0;
            for (int i = 0; i < _count; i++)
            {
                ref G g = ref _g[i];
                switch ((PickupKind)g.kind)
                {
                    case PickupKind.Gem:
                        _mGem[_nGem++] = M(g.pos, -0.05f, 45f, 0.5f);
                        break;
                    case PickupKind.Gold:
                        _mGold[_nGold++] = M(g.pos, -0.05f, 0f, 0.55f);
                        break;
                    case PickupKind.Chest:
                        if (_nChest < _mChest.Length) _mChest[_nChest++] = M(g.pos, -0.05f, 0f, 0.95f);
                        break;
                }
            }
            NeonMesh.RenderInstanced(_matGem, _quad, _mGem, _nGem);
            NeonMesh.RenderInstanced(_matGold, _quad, _mGold, _nGold);
            NeonMesh.RenderInstanced(_matChest, _quad, _mChest, _nChest);
        }

        static Matrix4x4 M(Vector2 pos, float z, float rotZ, float scale)
            => Matrix4x4.TRS(new Vector3(pos.x, pos.y, z), Quaternion.Euler(0, 0, rotZ), new Vector3(scale, scale, 1f));
    }
}
