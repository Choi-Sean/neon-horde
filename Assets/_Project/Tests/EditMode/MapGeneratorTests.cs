using NUnit.Framework;
using UnityEngine;

namespace NeonHorde.Tests
{
    public class MapGeneratorTests
    {
        [Test]
        public void Generate_IsDeterministicForSeed()
        {
            var a = MapGenerator.Generate(4242, MapThemeId.Furnace, null);
            var b = MapGenerator.Generate(4242, MapThemeId.Furnace, null);

            Assert.AreEqual(a.arenaSize, b.arenaSize);
            Assert.AreEqual(a.navGrid.width, b.navGrid.width);
            CollectionAssert.AreEqual(a.navGrid.flags, b.navGrid.flags);
        }

        [Test]
        public void Generate_CentreIsWalkable_AndRosterNonEmpty()
        {
            var plan = MapGenerator.Generate(7, MapThemeId.Void, null);
            var g = plan.navGrid;
            Assert.IsFalse(g.IsBlocked(g.width / 2, g.height / 2), "spawn point must be clear");
            Assert.IsNotNull(plan.enemyIds);
            Assert.Greater(plan.enemyIds.Length, 0);
        }

        [Test]
        public void FlowField_ReachesCentreFromEdge()
        {
            var plan = MapGenerator.Generate(99, MapThemeId.Grid, null);
            var g = plan.navGrid;
            var field = new FlowField(g);
            field.Rebuild(g.width / 2, g.height / 2);

            // a walkable cell a few steps in from a corner should have a path
            int probe = 6;
            for (int x = probe; x < g.width - probe; x++)
            {
                if (g.IsBlocked(x, probe)) continue;
                Assert.IsTrue(field.HasPath(g.CellCenter(x, probe)), $"no path from ({x},{probe})");
                return;
            }
            Assert.Pass("no open probe cell found (acceptable)");
        }

        [Test]
        public void RollEnemy_StaysWithinRoster()
        {
            var plan = MapGenerator.Generate(11, MapThemeId.Cryo, null);
            var rng = new SeededRng(1);
            for (int i = 0; i < 500; i++)
            {
                var id = plan.RollEnemy(rng);
                Assert.Contains(id, plan.enemyIds);
            }
        }
    }
}
