using System.Collections.Generic;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Turn planning of a computer player: economy, recruitment and army movement (minimax).
    /// Difficulty levels tune the parameters and a few decisions.
    /// </summary>
    public abstract class BaseAILogic
    {
        public abstract GameDifficulty Difficulty { get; }
        protected abstract int SearchDepth { get; }
        protected abstract float DecisionNoise { get; }
        protected abstract int MaxBuildsPerTurn { get; }
        protected abstract int MaxRecruitsPerTurn { get; }
        protected abstract int DesiredCohorts { get; }
        protected abstract int GoldReserve { get; }
        protected virtual float IdleChance => 0f;
        protected virtual bool MergesArmies => true;

        public List<IMapCommand> Decide(AIContext context)
        {
            var commands = new List<IMapCommand>();
            var budget = context.Player.Resources;

            if (MergesArmies) PlanMerges(context, commands);
            PlanReinforcements(context, commands, ref budget);
            PlanConstruction(context, commands, ref budget);
            PlanRecruitment(context, commands, ref budget);
            PlanMovement(context, commands);
            return commands;
        }

        #region Economy

        private void PlanMerges(AIContext context, List<IMapCommand> commands)
        {
            var armies = context.Armies.GetArmiesOf(context.Player.Id);
            var merged = new HashSet<int>();
            foreach (var a in armies)
            foreach (var b in armies)
            {
                if (a == b || a.TileId != b.TileId || merged.Contains(a.Id) || merged.Contains(b.Id)) continue;
                if (a.Cohorts.Count + b.Cohorts.Count - 1 > UnitsConfig.MaxCohortsPerArmy) continue;
                commands.Add(new MergeArmiesCommand(context.Player.Id, b.Id, a.Id));
                merged.Add(b.Id);
                merged.Add(a.Id);
            }
        }

        private void PlanReinforcements(AIContext context, List<IMapCommand> commands, ref ResourceBundle budget)
        {
            foreach (var army in context.Armies.GetArmiesOf(context.Player.Id))
            {
                var tile = context.Map.Get(army.TileId);
                if (tile.OwnerId != context.Player.Id || !tile.HasSettlement) continue;
                var killed = 0;
                var max = 0;
                foreach (var cohort in army.Cohorts)
                {
                    killed += cohort.Killed;
                    max += cohort.MaxSoldiers;
                }
                if (max == 0 || killed < max * 0.25f) continue;
                var cost = context.Construction.GetReinforceCost(army);
                if (!budget.Covers(cost)) continue;
                budget -= cost;
                commands.Add(new ReinforceArmyCommand(context.Player.Id, army.Id));
            }
        }

        private void PlanConstruction(AIContext context, List<IMapCommand> commands, ref ResourceBundle budget)
        {
            var needsPort = !HasLandTargets(context);
            for (var built = 0; built < MaxBuildsPerTurn; built++)
            {
                GameTile bestTile = null;
                UpgradeDefinition bestUpgrade = null;
                var bestScore = 0f;

                foreach (var tile in context.Map.Tiles)
                {
                    if (tile.OwnerId != context.Player.Id || !tile.HasSettlement) continue;
                    foreach (var upgrade in context.Construction.GetCandidates(tile))
                    {
                        var blocker = context.Construction.GetBuildBlocker(tile, upgrade, context.Player);
                        if (blocker != null && blocker != "reason.no_resources") continue;
                        var afterBuild = budget - upgrade.Cost;
                        if (!budget.Covers(upgrade.Cost) || afterBuild.Gold < GoldReserve) continue;
                        if (IsPlanned(commands, tile.Id)) continue;

                        var score = ScoreUpgrade(context, tile, upgrade, needsPort);
                        score *= 1f + context.Random.Range(-DecisionNoise, DecisionNoise) * 0.01f;
                        if (score <= bestScore) continue;
                        bestScore = score;
                        bestTile = tile;
                        bestUpgrade = upgrade;
                    }
                }

                if (bestUpgrade == null) return;
                budget -= bestUpgrade.Cost;
                commands.Add(new BuildUpgradeCommand(context.Player.Id, bestTile.Id, bestUpgrade.Id));
            }
        }

        protected virtual float ScoreUpgrade(AIContext context, GameTile tile, UpgradeDefinition upgrade, bool needsPort)
        {
            var yield = upgrade.BaseYield;
            if (upgrade.MatchingProduction != ProductionType.None && upgrade.MatchingProduction == tile.Data.Production)
                yield += upgrade.MatchingBonusYield;
            var value = yield.Value * 10f;
            if (upgrade.IsPort) value += needsPort ? 400f : 5f;
            if (upgrade.UnlocksAdvancedUnits) value += 25f;
            if (upgrade.HealingBonus > 0f || upgrade.MoraleBonus > 0f) value += 10f;
            return value / Mathf.Max(1f, upgrade.Cost.Value);
        }

        private static bool IsPlanned(List<IMapCommand> commands, int tileId)
        {
            foreach (var command in commands)
                if (command is BuildUpgradeCommand build && build.TileId == tileId) return true;
            return false;
        }

        private void PlanRecruitment(AIContext context, List<IMapCommand> commands, ref ResourceBundle budget)
        {
            var ownCohorts = 0;
            foreach (var army in context.Armies.GetArmiesOf(context.Player.Id)) ownCohorts += army.Cohorts.Count;

            for (var i = 0; i < MaxRecruitsPerTurn && ownCohorts < DesiredCohorts + ThreatBonus(context); i++)
            {
                var tile = ChooseRecruitTile(context);
                if (tile == null) return;

                UnitTypeDefinition best = null;
                var bestScore = 0f;
                foreach (var unit in context.Units.Units)
                {
                    var blocker = context.Construction.GetRecruitBlocker(tile, unit, context.Player);
                    if (blocker != null && blocker != "reason.no_resources") continue;
                    var cost = context.Construction.GetRecruitCost(tile, unit);
                    if (!budget.Covers(cost)) continue;
                    var score = unit.BaseStrength / Mathf.Max(1f, cost.Value) * context.Random.Range(0.8f, 1.2f);
                    if (score <= bestScore) continue;
                    bestScore = score;
                    best = unit;
                }

                if (best == null) return;
                budget -= context.Construction.GetRecruitCost(tile, best);
                commands.Add(new RecruitCohortCommand(context.Player.Id, tile.Id, best.Id));
                ownCohorts++;
            }
        }

        private int ThreatBonus(AIContext context)
        {
            var threat = 0;
            foreach (var army in context.Armies.All)
            {
                if (army.OwnerId == context.Player.Id || army.OwnerId == PlayerData.NeutralId) continue;
                foreach (var id in context.Map.GetNeighbours(army.TileId))
                    if (context.Map.Get(id).OwnerId == context.Player.Id)
                    {
                        threat += army.Cohorts.Count;
                        break;
                    }
            }
            return threat;
        }

        private static GameTile ChooseRecruitTile(AIContext context)
        {
            var capital = context.Map.Get(context.Player.CapitalTileId);
            if (capital != null && capital.OwnerId == context.Player.Id && !capital.HasHostileArmy(context.Player.Id))
                return capital;
            foreach (var tile in context.Map.Tiles)
                if (tile.OwnerId == context.Player.Id && tile.Settlement >= SettlementType.Castle) return tile;
            return null;
        }

        #endregion

        #region Movement

        private void PlanMovement(AIContext context, List<IMapCommand> commands)
        {
            var own = context.Armies.GetArmiesOf(context.Player.Id);
            own.Sort((a, b) => b.Strength.CompareTo(a.Strength));
            var state = BuildState(context, out var indexById);
            var planner = new MinimaxPlanner(context, BuildGoalDistances(context), DecisionNoise);

            foreach (var army in own)
            {
                if (army.HasMoved || IsMergedAway(commands, army.Id)) continue;
                if (context.Random.Chance(IdleChance)) continue;

                var index = indexById[army.Id];
                var destination = planner.ChooseDestination(state, index, SearchDepth);
                if (destination == army.TileId) continue;
                state.ApplyMove(index, destination, context.Map.Get(destination).IsLand);
                commands.Add(new MoveArmyCommand(context.Player.Id, army.Id, destination));
            }
        }

        private static bool IsMergedAway(List<IMapCommand> commands, int armyId)
        {
            foreach (var command in commands)
                if (command is MergeArmiesCommand merge && merge.SourceArmyId == armyId) return true;
            return false;
        }

        private static AIWorldState BuildState(AIContext context, out Dictionary<int, int> indexById)
        {
            var map = context.Map;
            var owners = new int[map.Count];
            var values = new float[map.Count];
            for (var i = 0; i < map.Count; i++)
            {
                var tile = map.Get(i);
                owners[i] = tile.OwnerId;
                var isCapital = false;
                foreach (var player in context.Players.Players)
                    if (player.Data.IsAlive && player.Data.CapitalTileId == i) isCapital = true;
                values[i] = AIWorldState.TileValue(tile.Settlement, isCapital, context.Player.CapitalTileId == i, tile.IsLand);
            }

            indexById = new Dictionary<int, int>();
            var armies = new AIWorldState.ArmyState[context.Armies.All.Count];
            for (var i = 0; i < armies.Length; i++)
            {
                var army = context.Armies.All[i];
                armies[i] = new AIWorldState.ArmyState
                {
                    Id = army.Id, Owner = army.OwnerId, Tile = army.TileId, Strength = army.Strength, Alive = true
                };
                indexById[army.Id] = i;
            }
            return new AIWorldState(owners, values, armies);
        }

        /// <summary>Multi-source BFS from every land tile worth conquering, following legal steps backwards.</summary>
        private static int[] BuildGoalDistances(AIContext context)
        {
            var map = context.Map;
            var distances = new int[map.Count];
            var queue = new Queue<int>();
            for (var i = 0; i < map.Count; i++)
            {
                var tile = map.Get(i);
                distances[i] = tile.IsLand && tile.OwnerId != context.Player.Id && tile.IsPassable ? 0 : -1;
                if (distances[i] == 0) queue.Enqueue(i);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var previous in map.GetNeighbours(current))
                {
                    if (distances[previous] >= 0) continue;
                    if (!context.Movement.CanStep(context.Player.Id, map.Get(previous), map.Get(current))) continue;
                    distances[previous] = distances[current] + 1;
                    queue.Enqueue(previous);
                }
            }
            return distances;
        }

        private static bool HasLandTargets(AIContext context)
        {
            var capital = context.Player.CapitalTileId;
            if (capital < 0) return true;
            var distances = context.Map.GetDistances(capital, (from, to) => from.IsLand && to.IsLand && to.IsPassable);
            for (var i = 0; i < distances.Length; i++)
                if (distances[i] > 0 && context.Map.Get(i).OwnerId != context.Player.Id) return true;
            return false;
        }

        #endregion
    }

    public sealed class EasyAI : BaseAILogic
    {
        public override GameDifficulty Difficulty => GameDifficulty.Easy;
        protected override int SearchDepth => 1;
        protected override float DecisionNoise => 40f;
        protected override int MaxBuildsPerTurn => 1;
        protected override int MaxRecruitsPerTurn => 1;
        protected override int DesiredCohorts => 5;
        protected override int GoldReserve => 0;
        protected override float IdleChance => 0.3f;
        protected override bool MergesArmies => false;
    }

    public sealed class MediumAI : BaseAILogic
    {
        public override GameDifficulty Difficulty => GameDifficulty.Medium;
        protected override int SearchDepth => 2;
        protected override float DecisionNoise => 10f;
        protected override int MaxBuildsPerTurn => 1;
        protected override int MaxRecruitsPerTurn => 2;
        protected override int DesiredCohorts => 8;
        protected override int GoldReserve => 20;
    }

    public sealed class HardAI : BaseAILogic
    {
        public override GameDifficulty Difficulty => GameDifficulty.Hard;
        protected override int SearchDepth => 3;
        protected override float DecisionNoise => 0f;
        protected override int MaxBuildsPerTurn => 2;
        protected override int MaxRecruitsPerTurn => 3;
        protected override int DesiredCohorts => 12;
        protected override int GoldReserve => 40;
    }
}
