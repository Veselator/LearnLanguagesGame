using System.Collections.Generic;
using LearnLanguages.Battle.AI;
using LearnLanguages.Battle.Logic;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Localization;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;
using LearnLanguages.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LearnLanguages.Tests
{
    public sealed class BattleTests
    {
        private RandomService _random;
        private BattleSimulator _simulator;
        private int _nextArmyId;

        [SetUp]
        public void SetUp()
        {
            _random = new RandomService(7);
            var ai = new BattleAIProvider(new List<IBattleAI>
            {
                new EasyBattleAI(_random), new MediumBattleAI(_random), new HardBattleAI(_random)
            });
            var duels = new DuelResolver(null, _random, TestAssets.Players, TestAssets.Battle, new LocalizationService(TestAssets.Localization),
                new GameSession());
            _simulator = new BattleSimulator(TestAssets.Battle, _random, ai, duels);
        }

        private Army CreateArmy(int owner, int swordsmen, int archers, float strength = 1f)
        {
            var units = TestAssets.Units;
            var army = new Army(++_nextArmyId, owner, 0);
            army.AddCohort(new Cohort(units.Commander, units.Commander.MaxSoldiers, Mathf.RoundToInt(units.Commander.MaxSoldiers * strength), 0, 1f));
            for (var i = 0; i < swordsmen; i++)
            {
                var type = units.GetById("swordsmen");
                army.AddCohort(new Cohort(type, type.MaxSoldiers, Mathf.RoundToInt(type.MaxSoldiers * strength), 0, 1f));
            }
            for (var i = 0; i < archers; i++)
            {
                var type = units.GetById("archers");
                army.AddCohort(new Cohort(type, type.MaxSoldiers, Mathf.RoundToInt(type.MaxSoldiers * strength), 0, 1f));
            }
            return army;
        }

        private BattleSetup Setup(Army attacker, Army defender)
        {
            var a = new PlayerData(1, "A", Color.red, false, GameDifficulty.Medium, -1);
            var d = new PlayerData(2, "D", Color.blue, false, GameDifficulty.Medium, -1);
            var tile = new GameTile(0, new ProvinceDataEntry(Color.white));
            return new BattleSetup(
                new BattleSide(BattleSideId.Attacker, a, new List<Army> { attacker }, new List<Army>()),
                new BattleSide(BattleSideId.Defender, d, new List<Army> { defender }, new List<Army>()),
                TestAssets.Battlefields.Pick(LandscapeType.Meadow, _random), tile);
        }

        [Test]
        public void SimulatedBattleAlwaysFinishes()
        {
            for (var i = 0; i < 20; i++)
            {
                var attacker = CreateArmy(1, 3, 1);
                var defender = CreateArmy(2, 2, 2);
                var result = _simulator.Simulate(Setup(attacker, defender));
                Assert.IsNotNull(result);
                Assert.LessOrEqual(result.Turns, TestAssets.Battle.MaxTurns + 1);
            }
        }

        [Test]
        public void StrongerArmyUsuallyWins()
        {
            var wins = 0;
            const int battles = 20;
            for (var i = 0; i < battles; i++)
            {
                var result = _simulator.Simulate(Setup(CreateArmy(1, 6, 2), CreateArmy(2, 1, 0, 0.6f)));
                if (result.AttackerWon) wins++;
            }
            Assert.GreaterOrEqual(wins, battles * 3 / 4);
        }

        [Test]
        public void BattleWritesCasualtiesBack()
        {
            var attacker = CreateArmy(1, 3, 0);
            var defender = CreateArmy(2, 3, 0);
            var before = 0;
            foreach (var cohort in attacker.Cohorts) before += cohort.Healthy;
            _simulator.Simulate(Setup(attacker, defender));
            var after = 0;
            foreach (var cohort in attacker.Cohorts) after += cohort.Healthy;
            Assert.Less(after, before);
        }

        [Test]
        public void DeploymentStaysInsideFirstColumns()
        {
            var model = new BattleModel(Setup(CreateArmy(1, 4, 2), CreateArmy(2, 4, 2)), TestAssets.Battle, _random);
            model.AutoDeploy(BattleSideId.Attacker);
            model.AutoDeploy(BattleSideId.Defender);
            foreach (var cohort in model.Cohorts)
                Assert.IsTrue(model.IsDeploymentCell(cohort.Side, cohort.Cell), $"{cohort.Type.Id} at {cohort.Cell}");
        }
    }
}
