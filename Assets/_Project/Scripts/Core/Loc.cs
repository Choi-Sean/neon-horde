using System.Collections.Generic;

namespace NeonHorde
{
    public enum Lang { Ko = 0, En = 1 }

    /// <summary>
    /// Minimal two-language string table. Menu / results strings are keyed here; the
    /// rest of the UI is retrofitted incrementally (see docs/DEV_LOG.md M6).
    /// </summary>
    public static class Loc
    {
        public static Lang Current = Lang.Ko;

        static readonly Dictionary<string, string[]> Table = new()
        {
            // key            { ko, en }
            ["play"]         = new[] { "플레이", "PLAY" },
            ["daily"]        = new[] { "일일 도전", "DAILY" },
            ["character"]    = new[] { "캐릭터", "CHARACTERS" },
            ["shop"]         = new[] { "상점 (영구 강화)", "SHOP" },
            ["quests"]       = new[] { "퀘스트", "QUESTS" },
            ["store"]        = new[] { "스토어", "STORE" },
            ["settings"]     = new[] { "설정", "SETTINGS" },
            ["close"]        = new[] { "닫기", "CLOSE" },
            ["select"]       = new[] { "선택", "SELECT" },
            ["selected"]     = new[] { "선택됨", "SELECTED" },
            ["retry"]        = new[] { "다시하기", "RETRY" },
            ["menu"]         = new[] { "메뉴", "MENU" },
            ["victory"]      = new[] { "VICTORY", "VICTORY" },
            ["gameover"]     = new[] { "GAME OVER", "GAME OVER" },
            ["levelup"]      = new[] { "LEVEL UP", "LEVEL UP" },
            ["chest"]        = new[] { "CHEST", "CHEST" },
            ["guest_warn"]   = new[]
            {
                "게스트 모드입니다.\n계정을 연결하지 않으면 앱 삭제·기기 변경 시 진행상황이 사라집니다.",
                "You're playing as a guest.\nWithout a linked account your progress is lost if you delete the app or switch devices."
            },
            ["ad_revive"]    = new[] { "광고 보고 부활", "Watch ad to revive" },
            ["see_result"]   = new[] { "결과 보기", "See results" },
            ["double_gold"]  = new[] { "골드 2배 (광고)", "Double gold (ad)" },
            ["hint_move"]    = new[] { "드래그로 이동 · 무기는 자동", "Drag to move · weapons auto-fire" },
        };

        public static string T(string key)
        {
            if (Table.TryGetValue(key, out var v))
                return v[(int)Current < v.Length ? (int)Current : 0];
            return key;
        }
    }
}
