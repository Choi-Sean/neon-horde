using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonHorde.Tests
{
    public class SpatialHashTests
    {
        [Test]
        public void Query_ReturnsNearbyIds_ExcludesFarOnes()
        {
            var hash = new SpatialHash(1f);
            hash.Insert(0, new Vector2(0f, 0f));
            hash.Insert(1, new Vector2(0.5f, 0.5f));
            hash.Insert(2, new Vector2(50f, 50f));

            var results = new List<int>();
            hash.Query(new Vector2(0f, 0f), 1.5f, results);

            Assert.Contains(0, results);
            Assert.Contains(1, results);
            Assert.IsFalse(results.Contains(2));
        }

        [Test]
        public void Clear_EmptiesAllCells()
        {
            var hash = new SpatialHash(1f);
            hash.Insert(0, Vector2.zero);
            hash.Insert(1, new Vector2(3f, 3f));
            hash.Clear();

            var results = new List<int>();
            hash.Query(Vector2.zero, 100f, results);
            Assert.IsEmpty(results);
        }

        [Test]
        public void Query_IsReusableAcrossRebuilds()
        {
            var hash = new SpatialHash(2f);
            var results = new List<int>();

            for (int frame = 0; frame < 5; frame++)
            {
                hash.Clear();
                hash.Insert(frame, new Vector2(frame, 0f));
                hash.Query(new Vector2(frame, 0f), 0.5f, results);
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(frame, results[0]);
            }
        }
    }
}
