using System.Linq;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;
using NUnit.Framework;
using UnityEditor;

namespace LearnLanguages.Tests
{
    public sealed class MapTests
    {
        private static Map.Data.Map TestMap => AssetDatabase.LoadAssetAtPath<Map.Data.Map>("Assets/Data/Maps/Test/Map_Test.asset");

        [Test]
        public void NeighboursAreSymmetric()
        {
            var data = TestMap.ProvincesData;
            for (var i = 0; i < data.Count; i++)
            foreach (var neighbour in data.Get(i).Neighbours)
                Assert.IsTrue(data.Get(neighbour).Neighbours.Contains(i), $"{i} -> {neighbour}");
        }

        [Test]
        public void SpawnPointsAreLandCitiesReachableFromEachOther()
        {
            var map = new GameMap();
            map.Build(TestMap.ProvincesData);
            var spawns = TestMap.ProvincesData.PossibleSpawnPoints;
            Assert.GreaterOrEqual(spawns.Count, 2);
            foreach (var spawn in spawns)
            {
                var tile = map.Get(spawn);
                Assert.IsTrue(tile.IsLand);
                Assert.AreEqual(SettlementType.City, tile.Settlement);
                // Any route is allowed here (ports may be built later), only the graph itself is checked.
                var distances = map.GetDistances(spawn, (from, to) => to.IsPassable);
                foreach (var other in spawns) Assert.GreaterOrEqual(distances[other], 0, $"{spawn} cannot reach {other}");
            }
        }

        [Test]
        public void EveryPixelBelongsToAProvince()
        {
            var lookup = new ProvinceLookup();
            lookup.Build(TestMap.Provinces, TestMap.ProvincesData);
            Assert.IsFalse(lookup.PixelProvinces.Any(id => id < 0));
        }
    }
}
