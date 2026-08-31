using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Cosmetic held-weapon: shows the player's primary weapon in hand, aimed at the
    /// nearest enemy, and tells the stick figure to present it. The sprite is rebuilt
    /// by WeaponArt whenever the weapon's id or level changes, so it visibly grows /
    /// morphs on level-up and on evolution. Self-installed by NeonShape at runtime.
    /// </summary>
    public sealed class PlayerWeaponRig : MonoBehaviour
    {
        NeonShape _shape;
        EnemyManager _enemies;
        SpriteRenderer _wr;

        WeaponId _shownId = (WeaponId)(-1);
        int _shownLevel = -1;
        Vector2 _lastDir = Vector2.right;

        void Start()
        {
            _shape = GetComponent<NeonShape>();
            _enemies = FindFirstObjectByType<EnemyManager>();

            var go = new GameObject("HeldWeapon");
            go.transform.SetParent(transform, false);
            _wr = go.AddComponent<SpriteRenderer>();
            _wr.sortingOrder = (_shape != null ? _shape.sortingOrder : 10) + 4;
        }

        void LateUpdate()
        {
            if (_wr == null) return;
            var rm = RunManager.Instance;
            var st = rm != null ? rm.State : null;
            if (st == null || st.weapons == null || st.weapons.Count == 0 || st.gameOver)
            {
                _wr.enabled = false;
                _shape?.SetAim(null);
                return;
            }
            _wr.enabled = true;

            // primary weapon → rebuild sprite on change
            var inst = st.weapons[0];
            if (inst.id != _shownId || inst.level != _shownLevel)
            {
                _shownId = inst.id;
                _shownLevel = inst.level;
                _wr.sprite = WeaponArt.Get(inst.id, inst.level);
                _wr.color = WeaponCatalog.Get(inst.id).color * 1.6f;
            }

            // aim at nearest enemy, else keep pointing the last way we aimed / moved
            Vector3 pp = transform.position;
            Vector2 target;
            int n = _enemies != null ? _enemies.NearestEnemy(pp, 16f) : -1;
            if (n >= 0) target = _enemies.EnemyPos(n);
            else target = (Vector2)pp + _lastDir * 3f;

            Vector3 shoulder = _shape != null ? _shape.ShoulderWorld : pp;
            Vector2 dir = ((Vector2)target - (Vector2)shoulder);
            if (dir.sqrMagnitude < 1e-4f) dir = _lastDir; else dir.Normalize();
            _lastDir = dir;

            _shape?.SetAim(target);

            float sc = (_shape != null ? _shape.Scale : 0.8f) * (1f + _shownLevel * 0.045f);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _wr.transform.position = shoulder + (Vector3)(dir * (0.18f * sc));
            _wr.transform.rotation = Quaternion.Euler(0, 0, ang);
            _wr.transform.localScale = new Vector3(sc, dir.x < 0f ? -sc : sc, 1f); // keep upright when aiming left
        }
    }
}
