using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// A small world-space HP bar under the player. Replaces the screen-bottom bar
    /// (which it hides). Self-installed by NeonShape at runtime.
    /// </summary>
    public sealed class PlayerHealthBar : MonoBehaviour
    {
        PlayerController _pc;
        SpriteRenderer _bg, _fill;
        float _flash;
        int _lastHp = int.MaxValue;

        const float Width = 1.3f, Height = 0.16f, YOffset = -0.28f;

        void Start()
        {
            _pc = GetComponent<PlayerController>() ?? FindFirstObjectByType<PlayerController>();

            var s = UiKit.Solid;
            var order = 30;
            _bg = Make("HpBarBg", s, new Color(0f, 0f, 0f, 0.6f), order);
            _fill = Make("HpBarFill", s, new Color(0.25f, 1.5f, 0.55f, 1f), order + 1);
            _bg.transform.localScale = new Vector3(Width, Height, 1f);

            var hud = GameObject.Find("HpBarBg");                 // old screen-bottom bar
            if (hud != null) hud.SetActive(false);
        }

        SpriteRenderer Make(string n, Sprite spr, Color c, int order)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        void LateUpdate()
        {
            if (_pc == null || _bg == null) return;
            float max = Mathf.Max(1, _pc.MaxHp);
            float t = Mathf.Clamp01(_pc.Hp / max);

            int hp = Mathf.CeilToInt(_pc.Hp);
            if (hp < _lastHp) _flash = 1f;
            _lastHp = hp;
            _flash = Mathf.Max(0f, _flash - Time.deltaTime * 4f);

            Vector3 p = transform.position;
            _bg.transform.position = new Vector3(p.x, p.y + YOffset, 0f);
            _fill.transform.position = new Vector3(p.x - Width * 0.5f * (1f - t), p.y + YOffset, -0.01f);
            _fill.transform.localScale = new Vector3(Width * t, Height * 0.72f, 1f);

            Color green = new Color(0.25f, 1.5f, 0.55f, 1f);
            Color low = new Color(1.7f, 0.4f, 0.3f, 1f);
            _fill.color = Color.Lerp(Color.Lerp(low, green, t), Color.white * 2f, _flash);
        }
    }
}
