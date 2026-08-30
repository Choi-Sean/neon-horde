using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// A pool of SpriteRenderers driven imperatively each frame. A manager calls
    /// Begin(), then Draw(...) once per visible entity, then End() to hide the unused
    /// tail. Replaces Graphics.RenderMeshInstanced, whose INSTANCING_ON shader variant
    /// was stripped from the player build — so enemies / projectiles / pickups / terrain
    /// drew nothing on device even though the plain grid (a MeshRenderer) rendered fine.
    /// SpriteRenderer + the built-in "Sprites/Default" shader is the path that already
    /// works for the player and the VFX, so everything visual now goes through it.
    /// </summary>
    public sealed class SpritePool
    {
        readonly Transform _root;
        readonly int _sortingOrder;
        SpriteRenderer[] _sr;
        int _n;      // draw calls issued this frame
        int _live;   // renderers currently enabled (high-water from last End)

        public SpritePool(string name, int sortingOrder, Transform parent = null, int initial = 64)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            _root = go.transform;
            _sortingOrder = sortingOrder;
            _sr = new SpriteRenderer[Mathf.Max(8, initial)];
            for (int i = 0; i < _sr.Length; i++) _sr[i] = NewSprite(i);
        }

        SpriteRenderer NewSprite(int i)
        {
            var go = new GameObject("s" + i);
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = _sortingOrder;
            go.SetActive(false);
            return sr;
        }

        public void Begin() => _n = 0;

        public void Draw(Sprite sprite, Vector2 pos, float z, Color color, Vector2 scale, float rotDeg = 0f)
        {
            if (_n >= _sr.Length) Grow();
            var sr = _sr[_n];
            if (_n >= _live) sr.gameObject.SetActive(true);
            _n++;
            sr.sprite = sprite;
            sr.color = color;
            var t = sr.transform;
            t.position = new Vector3(pos.x, pos.y, z);
            t.localScale = new Vector3(scale.x, scale.y, 1f);
            t.localRotation = rotDeg != 0f ? Quaternion.Euler(0f, 0f, rotDeg) : Quaternion.identity;
        }

        public void Draw(Sprite sprite, Vector2 pos, float z, Color color, float scale, float rotDeg = 0f)
            => Draw(sprite, pos, z, color, new Vector2(scale, scale), rotDeg);

        public void End()
        {
            for (int i = _n; i < _live; i++) _sr[i].gameObject.SetActive(false);
            _live = _n;
        }

        void Grow()
        {
            int old = _sr.Length;
            System.Array.Resize(ref _sr, old * 2);
            for (int i = old; i < _sr.Length; i++) _sr[i] = NewSprite(i);
        }
    }
}
