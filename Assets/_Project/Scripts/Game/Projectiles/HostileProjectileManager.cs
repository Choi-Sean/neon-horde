using UnityEngine;

namespace NeonHorde
{
    /// <summary>Enemy projectiles (spitters, boss ring attacks). Damage the player only.</summary>
    public sealed class HostileProjectileManager : MonoBehaviour
    {
        public const int Capacity = 1500;

        struct P { public Vector2 pos; public Vector2 vel; public float life; public int dmg; }

        readonly P[] _p = new P[Capacity];
        int _count;

        Mesh _quad;
        Material _mat;
        Matrix4x4[] _mBuf;
        PlayerController _player;

        void Awake()
        {
            _quad = NeonMesh.Quad;
            _mat = NeonMesh.NewUnlit(new Color(2.6f, 0.35f, 0.25f, 1f));
            _mBuf = new Matrix4x4[Capacity];
        }

        void Start() => _player = FindFirstObjectByType<PlayerController>();

        public void Fire(Vector2 pos, Vector2 dir, float speed, int dmg, float life)
        {
            if (_count >= Capacity) return;
            ref P p = ref _p[_count++];
            p.pos = pos;
            p.vel = dir.sqrMagnitude > 0.0001f ? dir.normalized * speed : Vector2.up * speed;
            p.dmg = dmg;
            p.life = life;
        }

        public void FireRing(Vector2 pos, int count, float speed, int dmg, float life)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                Fire(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), speed, dmg, life);
            }
        }

        void RemoveAt(int i) { _count--; if (i != _count) _p[i] = _p[_count]; }

        public void SimTick(float dt)
        {
            if (_player == null) return;
            Vector2 pp = _player.transform.position;
            for (int i = _count - 1; i >= 0; i--)
            {
                ref P p = ref _p[i];
                p.pos += p.vel * dt;
                p.life -= dt;
                if ((p.pos - pp).sqrMagnitude < 0.36f)
                {
                    _player.TakeContactDamage(p.dmg);
                    RemoveAt(i);
                    continue;
                }
                if (p.life <= 0f) RemoveAt(i);
            }
        }

        void LateUpdate()
        {
            for (int i = 0; i < _count; i++)
                _mBuf[i] = Matrix4x4.TRS(new Vector3(_p[i].pos.x, _p[i].pos.y, -0.1f),
                    Quaternion.identity, new Vector3(0.3f, 0.3f, 1f));
            NeonMesh.RenderInstanced(_mat, _quad, _mBuf, _count);
        }
    }
}
