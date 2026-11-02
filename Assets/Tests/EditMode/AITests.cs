using System.Collections.Generic;
using LearnLanguages.Core.Events;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.AI;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;
using LearnLanguages.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LearnLanguages.Tests
{
    public sealed class AITests
    {
        private const int Ai = 0;
        private const int Enemy = 1;

        private GameMap _map;
        private ArmiesRegistry _armies;
        private PlayersRegistry _players;
        private AIContext _context;
        private UnitsConfig _units;

        /// <summary>A chain of six land tiles: AI capital 0 ... enemy capital 5.</summary>
        [SetUp]
        public void SetUp()
        {
            var entries = new ProvinceDataEntry[6];
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = new ProvinceDataEntry(new Color32((byte)i, 0, 0, 255));
                entry.SetName("T" + i);
                entry.SetType(ProvinceType.Land);
                entry.SetLandscape(LandscapeType.Meadow);
                entry.SetSettlement(i == 0 || i == 5 ? SettlementType.City : SettlementType.Village);
                entry.SetNeighbours(i == 0 ? new[] { 1 } : i == 5 ? new[] { 4 } : new[] { i - 1, i + 1 });
                entries[i] = entry;
            }
            var data = ScriptableObject.CreateInstance<ProvincesData>();
            data.SetEntries(entries);

            _map = new GameMap();
            _map.Build(data);
            _armies = new ArmiesRegistry(_map);
            _players = new PlayersRegistry();
            _units = TestAssets.Units;

            var ai = new PlayerData(Ai, "AI", Color.red, false, GameDifficulty.Hard, 0);
            var enemy = new PlayerData(Enemy, "Enemy", Color.blue, true, GameDifficulty.Hard, 5);
            ai.SetResources(new ResourceBundle(100, 100, 100, 100));
            _players.Initialize(new List<PlayerData> { ai, enemy },
                new PlayerData(PlayerData.NeutralId, "N", Color.gray, false, GameDifficulty.Hard, -1));
            _map.Get(0).SetOwner(Ai);
            _map.Get(5).SetOwner(Enemy);

            var bus = new EventBus();
            var factory = new ArmyFactory(_units, _armies, bus);
            factory.CreateArmy(Ai, 0, _units.StartingArmy);

            var economy = new EconomyService(AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/Data/EconomyConfig.asset"), _map, _armies, bus);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradesConfig>("Assets/Data/Upgrades/UpgradesConfig.asset");
            _context = new AIContext(ai, _map, _armies, _players, economy, new ConstructionRules(upgrades, _map),
                new MovementRules(_map), _units, new RandomService(1));
        }

        private static AIProfile Profile(int depth, float[] priorities, params float[] probabilities)
        {
            var profile = new AIProfile();
            profile.SetSearchDepth(depth);
            profile.SetMaxBranching(30);
            profile.SetPriorities(priorities);
            profile.SetDecisionProbabilities(probabilities);
            return profile;
        }

        private static float[] Only(AICriterion criterion)
        {
            var priorities = new float[AICriteria.Count];
            priorities[(int)criterion] = 1f;
            return priorities;
        }

        [Test]
        public void ThereAreExactlyTenCriteria()
        {
            Assert.AreEqual(10, AICriteria.Count);
            Assert.AreEqual(10, System.Enum.GetValues(typeof(AICriterion)).Length);
        }

        [Test]
        public void PrioritiesAreNormalizedByTheirSum()
        {
            var normalized = WeightVector.Normalize(new[] { 2f, 3f, 5f });
            Assert.AreEqual(0.2f, normalized[0], 1e-5f);
            Assert.AreEqual(0.3f, normalized[1], 1e-5f);
            Assert.AreEqual(0.5f, normalized[2], 1e-5f);

            var zeros = WeightVector.Normalize(new[] { 0f, 0f });
            Assert.AreEqual(0.5f, zeros[0], 1e-5f);
        }

        [Test]
        public void ProbabilitiesPickTheRankOfTheRoll()
        {
            var probabilities = AIDecisionPicker.GetRankProbabilities(new[] { 0.5f, 0.3f, 0.2f }, 3);
            Assert.AreEqual(0, AIDecisionPicker.Pick(probabilities, 0.49f));
            Assert.AreEqual(1, AIDecisionPicker.Pick(probabilities, 0.51f));
            Assert.AreEqual(2, AIDecisionPicker.Pick(probabilities, 0.99f));
        }

        [Test]
        public void ProbabilitiesAreRescaledToTheOptionsThatExist()
        {
            var probabilities = AIDecisionPicker.GetRankProbabilities(new[] { 0.5f, 0.25f, 0.25f }, 2);
            Assert.AreEqual(2f / 3f, probabilities[0], 1e-5f);
            Assert.AreEqual(1f / 3f, probabilities[1], 1e-5f);

            var noChances = AIDecisionPicker.GetRankProbabilities(new float[0], 3);
            Assert.AreEqual(1f, noChances[0], 1e-5f);
        }

        [Test]
        public void DifficultyFollowsDepthAndProbabilities()
        {
            var greedy = new[] { 1f };
            var careless = new[] { 0.4f, 0.3f, 0.3f };
            Assert.Greater(AIDifficultyRating.Calculate(3, greedy), AIDifficultyRating.Calculate(1, greedy), "Deeper is stronger");
            Assert.Greater(AIDifficultyRating.Calculate(3, greedy), AIDifficultyRating.Calculate(3, careless), "Reliable is stronger");
            Assert.AreEqual(GameDifficulty.Easy, Profile(1, new float[AICriteria.Count], careless).Difficulty);
            Assert.AreEqual(GameDifficulty.Hard, Profile(3, new float[AICriteria.Count], greedy).Difficulty);
        }

        [Test]
        public void PickFallsBackToTheClosestDifficulty()
        {
            var config = ScriptableObject.CreateInstance<AIProfilesConfig>();
            var hard = Profile(3, new float[AICriteria.Count], 1f);
            hard.SetId("hard");
            config.SetDefaults(new[] { hard });
            var store = new AIProfilesStore(config, new MemoryStorage());
            Assert.AreEqual("hard", store.Pick(GameDifficulty.Easy, new RandomService(1)).Id);
        }

        [Test]
        public void TerritoryLovingAiWalksIntoFreeLand()
        {
            var commands = new AIPlanner().Plan(_context, Profile(1, Only(AICriterion.Territory), 1f), AINullDecisionObserver.Instance);
            Assert.IsTrue(commands.Exists(c => c is MoveArmyCommand move && move.TargetTileId == 1));
        }

        [Test]
        public void RankedOptionsMatchPlainMinimax()
        {
            var weights = new float[AICriteria.Count];
            for (var i = 0; i < weights.Length; i++) weights[i] = i + 1f;
            var profile = Profile(3, weights, 1f);
            new ArmyFactory(_units, _armies, new EventBus()).CreateArmy(Enemy, 3, _units.StartingArmy);

            var trace = new AIDecisionTrace("AI", profile);
            var world = new AIWorld(_context);
            var state = new AISimulation(_context, world);
            var evaluator = new AIEvaluator(profile.NormalizedPriorities);
            var generator = new AIActionGenerator();
            new AIMinimax(profile, evaluator, generator, _context.Random, trace).Decide(state);

            var ranking = trace.Steps[trace.Steps.Count - 1].Ranking;
            foreach (var option in ranking)
            {
                var next = state.Clone();
                option.Action.Apply(next);
                Assert.AreEqual(Plain(next, 2, false, evaluator, generator), option.Value, 1e-4f);
            }
            for (var i = 1; i < ranking.Count; i++) Assert.GreaterOrEqual(ranking[i - 1].Value, ranking[i].Value);
        }

        private static float Plain(AISimulation state, int depth, bool maximizing, AIEvaluator evaluator, AIActionGenerator generator)
        {
            if (state.IsTerminal) return AIEvaluator.TerminalValue(state, depth);
            if (depth <= 0) return evaluator.Score(state);
            var best = maximizing ? float.NegativeInfinity : float.PositiveInfinity;
            foreach (var action in maximizing ? generator.GenerateOwn(state) : generator.GenerateOpponents(state))
            {
                var next = state.Clone();
                action.Apply(next);
                var value = Plain(next, depth - 1, !maximizing, evaluator, generator);
                best = maximizing ? Mathf.Max(best, value) : Mathf.Min(best, value);
            }
            return best;
        }

        [Test]
        public void TraceRecordsAWellFormedTree()
        {
            new ArmyFactory(_units, _armies, new EventBus()).CreateArmy(Enemy, 3, _units.StartingArmy);
            var profile = Profile(3, new float[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, 1f);
            var trace = new AIDecisionTrace("AI", profile);
            new AIPlanner().Plan(_context, profile, trace);

            Assert.Greater(trace.DecisionCount, 0);
            var entered = 0;
            var exited = 0;
            var evaluated = 0;
            foreach (var step in trace.Steps)
            {
                if (step.Kind == AITraceStepKind.NodeEntered) entered++;
                if (step.Kind == AITraceStepKind.NodeExited) exited++;
                if (step.Kind == AITraceStepKind.LeafEvaluated && !step.IsTerminal)
                {
                    evaluated++;
                    Assert.AreEqual(AICriteria.Count, step.Criteria.Length);
                }
            }
            Assert.AreEqual(entered, exited, "Every entered node is left");
            Assert.Greater(evaluated, 0);
            foreach (var node in trace.Nodes)
            {
                if (node.EnteredStep >= 0 && node.ParentId >= 0) Assert.Greater(node.ExitedStep, node.EnteredStep);
                if (node.ParentId >= 0) Assert.Contains(node.Id, trace.Nodes[node.ParentId].Children);
            }
            var last = trace.Steps[trace.Steps.Count - 1];
            Assert.AreEqual(AITraceStepKind.Picked, last.Kind);
        }

        [Test]
        public void PlaybackStepsBothWaysAndJumpsTreeByTree()
        {
            var profile = Profile(2, new float[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, 1f);
            var trace = new AIDecisionTrace("AI", profile);
            new AIPlanner().Plan(_context, profile, trace);

            var playback = new AIDecisionPlayback(trace);
            Assert.IsFalse(playback.CanStepBack);
            playback.StepForward();
            Assert.AreEqual(1, playback.Cursor);
            playback.StepBack();
            Assert.AreEqual(0, playback.Cursor);

            playback.ToEndOfTree();
            Assert.AreEqual(trace.GetLastStep(0), playback.Cursor);
            for (var i = 0; i < trace.DecisionCount; i++) playback.ToEndOfTree();
            Assert.IsFalse(playback.CanStepForward);
        }

        private sealed class MemoryStorage : IAIProfilesStorage
        {
            public string Json;
            public string Read() => Json;
            public void Write(string json) => Json = json;
        }

        private static AIProfilesConfig Config =>
            AssetDatabase.LoadAssetAtPath<AIProfilesConfig>("Assets/Data/AI/AIProfilesConfig.asset");

        [Test]
        public void EveryShippedProfileIsValid()
        {
            var store = new AIProfilesStore(Config, new MemoryStorage());
            foreach (GameDifficulty difficulty in System.Enum.GetValues(typeof(GameDifficulty)))
                Assert.AreEqual(difficulty, store.Pick(difficulty, new RandomService(1)).Difficulty);
            foreach (var profile in store.Profiles)
            {
                Assert.AreEqual(AICriteria.Count, profile.Priorities.Count, profile.Id);
                Assert.Greater(profile.DecisionProbabilities.Count, 0, profile.Id);
                var sum = 0f;
                foreach (var weight in profile.NormalizedPriorities) sum += weight;
                Assert.AreEqual(1f, sum, 1e-4f, profile.Id);
            }
        }

        [Test]
        public void EditedAndAddedProfilesAreSavedAutomatically()
        {
            var storage = new MemoryStorage();
            var store = new AIProfilesStore(Config, storage);
            Assert.IsNull(storage.Json, "Nothing changed yet");
            var shippedCount = store.Profiles.Count;
            var shippedProbabilities = Config.Defaults[0].DecisionProbabilities[0];

            var added = store.Add("Mine");
            store.Edit(added, p => p.SetPriority(AICriterion.Income, 7f));
            var copy = store.Duplicate(added, "Mine 2");
            store.Edit(store.Profiles[0], p => p.SetDecisionProbabilities(new[] { 0.3f, 0.7f }));
            Assert.AreNotEqual(added.Id, copy.Id);

            var reloaded = new AIProfilesStore(Config, storage);
            Assert.AreEqual(shippedCount + 2, reloaded.Profiles.Count);
            Assert.AreEqual("Mine", reloaded.GetById(added.Id).DisplayName);
            Assert.AreEqual(7f, reloaded.GetById(copy.Id).Priorities[(int)AICriterion.Income]);
            Assert.AreEqual(0.7f, reloaded.Profiles[0].DecisionProbabilities[1], 1e-5f);
            Assert.AreEqual(shippedProbabilities, Config.Defaults[0].DecisionProbabilities[0], "Shipped data stay untouched");
        }

        [Test]
        public void CorruptedProfilesFileFallsBackToShippedModels()
        {
            var store = new AIProfilesStore(Config, new MemoryStorage { Json = "{\"Profiles\":[{\"_id\":\"broken\"}]}" });
            var broken = store.GetById("broken");
            Assert.AreEqual(AICriteria.Count, broken.Priorities.Count);
            Assert.GreaterOrEqual(store.Profiles.Count, Config.Defaults.Count);

            var unreadable = new AIProfilesStore(Config, new MemoryStorage { Json = "not json" });
            Assert.AreEqual(Config.Defaults.Count, unreadable.Profiles.Count);
        }

        [Test]
        public void HardAiPlansQuicklyOnARealMap()
        {
            var profile = new AIProfilesStore(Config, new MemoryStorage()).Pick(GameDifficulty.Hard, new RandomService(1));
            var mapAsset = AssetDatabase.LoadAssetAtPath<Map.Data.Map>("Assets/Data/Maps/Circle/Map_Circle.asset");

            var map = new GameMap();
            map.Build(mapAsset.ProvincesData);
            var armies = new ArmiesRegistry(map);
            var players = new PlayersRegistry();
            var spawns = mapAsset.ProvincesData.PossibleSpawnPoints;
            var ai = new PlayerData(Ai, "AI", Color.red, false, GameDifficulty.Hard, spawns[0]);
            var enemy = new PlayerData(Enemy, "Enemy", Color.blue, true, GameDifficulty.Hard, spawns[1]);
            ai.SetResources(new ResourceBundle(300, 200, 200, 200));
            players.Initialize(new List<PlayerData> { ai, enemy },
                new PlayerData(PlayerData.NeutralId, "N", Color.gray, false, GameDifficulty.Hard, -1));
            map.Get(spawns[0]).SetOwner(Ai);
            map.Get(spawns[1]).SetOwner(Enemy);

            var bus = new EventBus();
            var factory = new ArmyFactory(_units, armies, bus);
            factory.CreateArmy(Ai, spawns[0], _units.StartingArmy);
            factory.CreateArmy(Enemy, spawns[1], _units.StartingArmy);
            foreach (var tile in map.Tiles)
                if (tile.IsLand && tile.IsNeutral && tile.HasSettlement) factory.CreateGarrison(tile);

            var economy = new EconomyService(AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/Data/EconomyConfig.asset"), map, armies, bus);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradesConfig>("Assets/Data/Upgrades/UpgradesConfig.asset");
            var context = new AIContext(ai, map, armies, players, economy, new ConstructionRules(upgrades, map),
                new MovementRules(map), _units, new RandomService(1));

            var trace = new AIDecisionTrace("AI", profile);
            var started = System.DateTime.UtcNow;
            var commands = new AIPlanner().Plan(context, profile, trace);
            var milliseconds = (System.DateTime.UtcNow - started).TotalMilliseconds;
            Debug.Log($"Hard AI: {commands.Count} commands, {trace.DecisionCount} decisions, {trace.Steps.Count} steps, {milliseconds:0} ms");

            Assert.Greater(commands.Count, 0, "The AI has something to do at the start of a match");
            Assert.Less(milliseconds, 3000, "The turn must not freeze the game");
        }
    }
}
