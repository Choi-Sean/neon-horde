using System.Collections.Generic;

namespace NeonHorde
{
    /// <summary>Builds level-up / chest card choices from the run's weapons + passives, honouring banned keys and offering weapon evolutions.</summary>
    public static class UpgradeGenerator
    {
        public static List<UpgradeOption> Generate(RunState st, SeededRng rng, int count)
        {
            var pool = new List<UpgradeOption>();

            // weapon evolutions first (rare, high value)
            for (int i = 0; i < WeaponCatalog.BaseWeaponCount; i++)
            {
                var baseId = (WeaponId)i;
                if (!st.CanEvolve(baseId)) continue;
                var def = WeaponCatalog.Get(baseId);
                var evoName = WeaponCatalog.Get(def.evolvesInto).name;
                Add(pool, st, $"evo:{def.evolvesInto}", $"진화 → {evoName}", "무기 진화",
                    () => st.EvolveWeapon(baseId));
            }

            foreach (var w in st.weapons)
            {
                if (w.AtMax) continue;
                var captured = w;
                Add(pool, st, $"w:{w.id}", $"{w.Def.name} Lv{w.level + 1}", "무기 강화",
                    () => st.AddOrLevelWeapon(captured.id));
            }

            if (st.weapons.Count < RunState.MaxWeapons)
                for (int i = 0; i < WeaponCatalog.BaseWeaponCount; i++)
                {
                    var id = (WeaponId)i;
                    if (st.GetWeapon(id) != null) continue;
                    Add(pool, st, $"nw:{id}", $"신규 무기: {WeaponCatalog.Get(id).name}", "무기 획득",
                        () => st.AddOrLevelWeapon(id));
                }

            foreach (var kv in st.passives)
            {
                var id = kv.Key;
                if (kv.Value >= PassiveCatalog.Get(id).maxLevel) continue;
                Add(pool, st, $"p:{id}", $"{PassiveCatalog.Get(id).name} Lv{kv.Value + 1}", "특성 강화",
                    () => st.AddOrLevelPassive(id));
            }

            if (st.passives.Count < RunState.MaxPassives)
                for (int i = 0; i < PassiveCatalog.BasePassiveCount; i++)
                {
                    var id = (PassiveId)i;
                    if (st.PassiveLevel(id) > 0) continue;
                    Add(pool, st, $"np:{id}", $"신규 특성: {PassiveCatalog.Get(id).name}", "특성 획득",
                        () => st.AddOrLevelPassive(id));
                }

            if (pool.Count == 0)
            {
                pool.Add(new UpgradeOption("misc:gold", "골드 +30", "", () => st.goldBanked += 30));
                pool.Add(new UpgradeOption("misc:xp", "경험치 +20", "", () => st.AddXp(20f)));
            }

            var result = new List<UpgradeOption>();
            int take = System.Math.Min(count, pool.Count);
            for (int i = 0; i < take; i++)
            {
                int r = rng.NextInt(0, pool.Count);
                result.Add(pool[r]);
                pool.RemoveAt(r);
            }
            return result;
        }

        static void Add(List<UpgradeOption> pool, RunState st, string key, string title, string desc, System.Action apply)
        {
            if (st.banned.Contains(key)) return;
            pool.Add(new UpgradeOption(key, title, desc, apply));
        }
    }
}
