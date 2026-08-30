using NUnit.Framework;

namespace NeonHorde.Tests
{
    public class RogueliteTests
    {
        [Test]
        public void Evolution_RequiresMaxWeaponAndPassive()
        {
            var st = new RunState();
            st.AddOrLevelWeapon(WeaponId.Bolt);
            Assert.IsFalse(st.CanEvolve(WeaponId.Bolt), "not maxed yet");

            var w = st.GetWeapon(WeaponId.Bolt);
            while (!w.AtMax) st.AddOrLevelWeapon(WeaponId.Bolt);
            Assert.IsFalse(st.CanEvolve(WeaponId.Bolt), "passive missing");

            for (int i = 0; i < 3; i++) st.AddOrLevelPassive(WeaponCatalog.Get(WeaponId.Bolt).evolveRequires);
            Assert.IsTrue(st.CanEvolve(WeaponId.Bolt));

            Assert.IsTrue(st.EvolveWeapon(WeaponId.Bolt));
            Assert.IsNull(st.GetWeapon(WeaponId.Bolt));
            Assert.IsNotNull(st.GetWeapon(WeaponId.Railgun));
        }

        [Test]
        public void BannedKey_IsExcludedFromPool()
        {
            var st = new RunState();
            st.AddOrLevelWeapon(WeaponId.Bolt);
            st.banned.Add("nw:Aura");
            var rng = new SeededRng(3);
            for (int trial = 0; trial < 20; trial++)
            {
                var opts = UpgradeGenerator.Generate(st, rng, 3);
                foreach (var o in opts) Assert.AreNotEqual("nw:Aura", o.key);
            }
        }

        [Test]
        public void PowerCatalog_CostGrows()
        {
            var d = PowerCatalog.Get(PowerId.Damage);
            Assert.Less(d.CostForLevel(0), d.CostForLevel(1));
            Assert.Less(d.CostForLevel(1), d.CostForLevel(2));
        }
    }
}
