using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    public struct RunEndSummary
    {
        public int kills;
        public int timeSec;
        public int level;
        public int bossKills;
        public long goldBanked;
    }

    /// <summary>
    /// Daily (date-seeded, 3/day) + milestone quests. Progress advances on run end,
    /// rewards (cores) are claimed manually.
    /// </summary>
    public sealed class QuestService
    {
        readonly MetaController _meta;
        public event Action Changed;

        public QuestService(MetaController meta)
        {
            _meta = meta;
            EnsureDaily();
        }

        public IReadOnlyList<MetaState.QuestEntry> Daily => _meta.State.dailyQuests;
        public IReadOnlyList<MetaState.QuestEntry> Milestones => _meta.State.milestoneQuests;

        public void EnsureDaily()
        {
            MetaState s = _meta.State;
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            bool needRoll = s.dailyDateIso != today || s.dailyQuests.Count == 0;

            if (needRoll)
            {
                s.dailyDateIso = today;
                s.dailyQuests.Clear();
                var rng = new SeededRng(Fnv(today));
                var pool = new List<int>();
                for (int i = 0; i < QuestCatalog.Daily.Length; i++) pool.Add(i);
                for (int i = 0; i < 3 && pool.Count > 0; i++)
                {
                    int p = rng.NextInt(0, pool.Count);
                    s.dailyQuests.Add(new MetaState.QuestEntry { id = QuestCatalog.Daily[pool[p]].id });
                    pool.RemoveAt(p);
                }
            }

            // ensure milestone entries exist
            foreach (var m in QuestCatalog.Milestones)
                if (!s.milestoneQuests.Exists(e => e.id == m.id))
                    s.milestoneQuests.Add(new MetaState.QuestEntry { id = m.id });

            _meta.Save();
        }

        static ulong Fnv(string str)
        {
            ulong h = 1469598103934665603UL;
            foreach (char c in str) { h ^= c; h *= 1099511628211UL; }
            return h;
        }

        public void OnRunEnded(RunEndSummary sum)
        {
            Advance(_meta.State.dailyQuests, sum);
            Advance(_meta.State.milestoneQuests, sum);
            _meta.Save();
            Changed?.Invoke();
        }

        static bool IsCumulative(QuestGoal g)
            => g == QuestGoal.PlayRuns || g == QuestGoal.TotalKills || g == QuestGoal.KillBoss;

        void Advance(List<MetaState.QuestEntry> list, RunEndSummary sum)
        {
            foreach (var e in list)
            {
                if (e.claimed) continue;
                var defN = QuestCatalog.Find(e.id);
                if (defN == null) continue;
                QuestDef def = defN.Value;

                int value = def.goal switch
                {
                    QuestGoal.PlayRuns => 1,
                    QuestGoal.TotalKills => sum.kills,
                    QuestGoal.KillBoss => sum.bossKills,
                    QuestGoal.SurviveSeconds => sum.timeSec,
                    QuestGoal.ReachLevel => sum.level,
                    QuestGoal.KillsInOneRun => sum.kills,
                    QuestGoal.BankGold => (int)sum.goldBanked,
                    _ => 0
                };

                if (IsCumulative(def.goal)) e.progress += value;
                else e.progress = Mathf.Max(e.progress, value);
            }
        }

        public bool CanClaim(string id)
        {
            var e = Get(id);
            var def = QuestCatalog.Find(id);
            return e != null && def != null && !e.claimed && e.progress >= def.Value.target;
        }

        public bool Claim(string id)
        {
            if (!CanClaim(id)) return false;
            var e = Get(id);
            var def = QuestCatalog.Find(id).Value;
            e.claimed = true;
            if (def.rewardCores > 0) _meta.AddCores(def.rewardCores);
            if (def.rewardGold > 0) _meta.AddGold(def.rewardGold);
            _meta.Save();
            Changed?.Invoke();
            return true;
        }

        MetaState.QuestEntry Get(string id)
        {
            var e = _meta.State.dailyQuests.Find(q => q.id == id);
            if (e != null) return e;
            return _meta.State.milestoneQuests.Find(q => q.id == id);
        }
    }
}
