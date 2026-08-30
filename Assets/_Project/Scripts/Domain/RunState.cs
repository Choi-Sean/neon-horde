using System.Collections.Generic;
using UnityEngine;

namespace NeonHorde
{
    /// <summary>
    /// Transient per-run state, owned by RunManager. Persistent profile lives in MetaState.
    /// </summary>
    public sealed class RunState
    {
        public float time;
        public int kills;
        public int level = 1;
        public float xp;
        public float xpToNext = XpForLevel(2);
        public long goldBanked;      // coins collected this run (added to MetaState on end)
        public bool gameOver;
        public bool victory;

        public int pendingLevelUps;
        public int pendingChests;

        public CharacterId characterId = CharacterId.Pulse;

        public readonly List<WeaponInstance> weapons = new List<WeaponInstance>();
        public readonly Dictionary<PassiveId, int> passives = new Dictionary<PassiveId, int>();
        public const int MaxWeapons = 6;
        public const int MaxPassives = 6;

        public DerivedStats stats = DerivedStats.Identity;
        public MetaMods metaMods = MetaMods.Identity;
        public MapPlan map;

        public int freeRevives;

        // reroll / banish economy (M4)
        public int rerolls = 1;
        public int banishes = 1;
        public readonly HashSet<string> banned = new HashSet<string>();

        public bool EvolveWeapon(WeaponId baseId)
        {
            var w = GetWeapon(baseId);
            if (w == null) return false;
            var def = WeaponCatalog.Get(baseId);
            if (!def.hasEvolution) return false;
            w.id = def.evolvesInto;
            w.level = 1;
            return true;
        }

        public bool CanEvolve(WeaponId baseId)
        {
            var w = GetWeapon(baseId);
            if (w == null || !w.AtMax) return false;
            var def = WeaponCatalog.Get(baseId);
            if (!def.hasEvolution) return false;
            return PassiveLevel(def.evolveRequires) >= 3;
        }

        public void RecomputeStats(CharacterMods charMods)
        {
            stats = DerivedStats.Compute(passives, charMods, metaMods);
        }

        public void AddXp(float amount)
        {
            xp += amount * stats.xpMul;
            while (xp >= xpToNext)
            {
                xp -= xpToNext;
                level++;
                xpToNext = XpForLevel(level + 1);
                pendingLevelUps++;
            }
        }

        public WeaponInstance GetWeapon(WeaponId id) => weapons.Find(w => w.id == id);

        public bool AddOrLevelWeapon(WeaponId id)
        {
            var w = GetWeapon(id);
            if (w != null) { if (!w.AtMax) w.level++; return true; }
            if (weapons.Count >= MaxWeapons) return false;
            weapons.Add(new WeaponInstance(id));
            return true;
        }

        public int PassiveLevel(PassiveId id) => passives.TryGetValue(id, out var v) ? v : 0;

        public bool AddOrLevelPassive(PassiveId id)
        {
            int cur = PassiveLevel(id);
            int max = PassiveCatalog.Get(id).maxLevel;
            if (cur >= max) return false;
            if (cur == 0 && passives.Count >= MaxPassives) return false;
            passives[id] = cur + 1;
            return true;
        }

        public static float XpForLevel(int targetLevel)
        {
            int n = Mathf.Max(2, targetLevel);
            return 5f + (n - 2) * 4f + Mathf.Pow(n, 1.5f);
        }
    }
}
