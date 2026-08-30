using NUnit.Framework;

namespace NeonHorde.Tests
{
    public class SeededRngTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new SeededRng(123456789);
            var b = new SeededRng(123456789);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt(), $"diverged at {i}");
        }

        [Test]
        public void DifferentSeeds_Diverge()
        {
            var a = new SeededRng(1);
            var b = new SeededRng(2);
            bool anyDifferent = false;
            for (int i = 0; i < 32; i++)
                if (a.NextUInt() != b.NextUInt()) { anyDifferent = true; break; }
            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void NextFloat_StaysInUnitInterval()
        {
            var r = new SeededRng(42);
            for (int i = 0; i < 20000; i++)
            {
                float f = r.NextFloat();
                Assert.GreaterOrEqual(f, 0f);
                Assert.Less(f, 1f);
            }
        }

        [Test]
        public void NextInt_RespectsBounds()
        {
            var r = new SeededRng(7);
            for (int i = 0; i < 20000; i++)
            {
                int v = r.NextInt(3, 9);
                Assert.GreaterOrEqual(v, 3);
                Assert.Less(v, 9);
            }
        }

        [Test]
        public void State_RoundTrips()
        {
            var r = new SeededRng(99);
            for (int i = 0; i < 50; i++) r.NextUInt();
            var snapshot = r.GetState();

            uint[] expected = new uint[10];
            for (int i = 0; i < 10; i++) expected[i] = r.NextUInt();

            r.SetState(snapshot);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(expected[i], r.NextUInt());
        }
    }
}
