using NUnit.Framework;
using UnityEngine;

namespace NeonHorde.Tests
{
    public class ProgressionTests
    {
        [Test]
        public void XpCurve_IsMonotonicIncreasing()
        {
            float prev = 0f;
            for (int lv = 2; lv < 60; lv++)
            {
                float x = RunState.XpForLevel(lv);
                Assert.Greater(x, prev);
                prev = x;
            }
        }

        [Test]
        public void AddXp_LevelsUpAndQueues()
        {
            var st = new RunState();
            st.stats = DerivedStats.Identity;
            st.AddXp(1000f);
            Assert.Greater(st.level, 1);
            Assert.Greater(st.pendingLevelUps, 0);
        }

        [Test]
        public void Weapons_CapAtMaxCount()
        {
            var st = new RunState();
            int added = 0;
            for (int i = 0; i < WeaponCatalog.BaseWeaponCount; i++)
                if (st.AddOrLevelWeapon((WeaponId)i)) added++;
            Assert.LessOrEqual(st.weapons.Count, RunState.MaxWeapons);
        }

        [Test]
        public void DerivedStats_RespondToPassives()
        {
            var st = new RunState();
            var baseline = DerivedStats.Compute(st.passives, default);
            st.AddOrLevelPassive(PassiveId.Might);
            st.AddOrLevelPassive(PassiveId.Might);
            var buffed = DerivedStats.Compute(st.passives, default);
            Assert.Greater(buffed.damageMul, baseline.damageMul);
        }

        [Test]
        public void UpgradeGenerator_ReturnsDistinctOptions()
        {
            var st = new RunState();
            st.AddOrLevelWeapon(WeaponId.Bolt);
            var rng = new SeededRng(5);
            var opts = UpgradeGenerator.Generate(st, rng, 3);
            Assert.AreEqual(3, opts.Count);
            Assert.AreNotSame(opts[0], opts[1]);
            Assert.AreNotSame(opts[1], opts[2]);
        }

        [Test]
        public void WeaponCatalog_AllHaveEightLevels()
        {
            for (int i = 0; i < WeaponCatalog.BaseWeaponCount; i++)
            {
                var def = WeaponCatalog.Get((WeaponId)i);
                Assert.AreEqual(8, def.levels.Length, def.name);
            }
        }
    }
}
