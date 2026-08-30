namespace NeonHorde
{
    public enum QuestGoal
    {
        PlayRuns, TotalKills, SurviveSeconds, ReachLevel, KillBoss, KillsInOneRun, BankGold
    }

    public struct QuestDef
    {
        public string id;
        public string description;
        public QuestGoal goal;
        public int target;
        public int rewardCores;
        public int rewardGold;
        public bool milestone;
    }

    public static class QuestCatalog
    {
        // Rotating daily pool — 3 picked per day (date-seeded).
        public static readonly QuestDef[] Daily =
        {
            D("d_play3",   "3판 플레이",         QuestGoal.PlayRuns,       3,  5),
            D("d_kill800", "몬스터 800 처치",     QuestGoal.TotalKills,     800, 5),
            D("d_surv8",   "8분 생존",           QuestGoal.SurviveSeconds, 480, 8),
            D("d_lv20",    "레벨 20 도달",       QuestGoal.ReachLevel,     20, 6),
            D("d_boss1",   "보스 1회 처치",       QuestGoal.KillBoss,       1,  10),
            D("d_run500",  "한 판에서 500 처치",  QuestGoal.KillsInOneRun,  500, 7),
            D("d_gold300", "한 판에서 골드 300",  QuestGoal.BankGold,       300, 6),
        };

        // One-time milestones.
        public static readonly QuestDef[] Milestones =
        {
            M("m_lv30",   "레벨 30 도달",        QuestGoal.ReachLevel,     30,  20),
            M("m_surv15", "15분 생존",           QuestGoal.SurviveSeconds, 900, 25),
            M("m_boss5",  "보스 5회 처치(누적)",  QuestGoal.KillBoss,       5,   30),
            M("m_kill20k","누적 20,000 처치",     QuestGoal.TotalKills,     20000, 40),
            M("m_run1000","한 판에서 1000 처치",  QuestGoal.KillsInOneRun,  1000, 35),
        };

        static QuestDef D(string id, string desc, QuestGoal g, int target, int cores)
            => new QuestDef { id = id, description = desc, goal = g, target = target, rewardCores = cores, rewardGold = 0, milestone = false };

        static QuestDef M(string id, string desc, QuestGoal g, int target, int cores)
            => new QuestDef { id = id, description = desc, goal = g, target = target, rewardCores = cores, rewardGold = 0, milestone = true };

        public static QuestDef? Find(string id)
        {
            foreach (var q in Daily) if (q.id == id) return q;
            foreach (var q in Milestones) if (q.id == id) return q;
            return null;
        }
    }
}
