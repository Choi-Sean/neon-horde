using UnityEngine;
using UnityEngine.UI;

namespace NeonHorde
{
    /// <summary>Reads RunState / PlayerController / EnemyManager each frame and updates the HUD.</summary>
    public sealed class HudController : MonoBehaviour
    {
        public Image xpFill;
        public Image hpFill;
        public Text infoText;
        public GameObject bossBarRoot;
        public Image bossFill;

        PlayerController _player;
        EnemyManager _enemies;

        void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            _enemies = FindFirstObjectByType<EnemyManager>();
            if (bossBarRoot != null) bossBarRoot.SetActive(false);
        }

        void Update()
        {
            RunManager rm = RunManager.Instance;
            if (rm == null) return;
            RunState s = rm.State;

            if (xpFill != null)
                xpFill.fillAmount = s.xpToNext > 0f ? Mathf.Clamp01(s.xp / s.xpToNext) : 0f;

            if (hpFill != null && _player != null)
                hpFill.fillAmount = _player.MaxHp > 0 ? Mathf.Clamp01((float)_player.Hp / _player.MaxHp) : 0f;

            if (infoText != null)
            {
                int m = (int)(s.time / 60f);
                int sec = (int)(s.time % 60f);
                infoText.text = $"{m:00}:{sec:00}   Lv {s.level}   Kills {s.kills}   G {s.goldBanked}";
            }

            if (_enemies != null && bossBarRoot != null)
            {
                bool on = _enemies.BossActive;
                if (bossBarRoot.activeSelf != on) bossBarRoot.SetActive(on);
                if (on && bossFill != null) bossFill.fillAmount = _enemies.BossHp01;
            }
        }
    }
}
