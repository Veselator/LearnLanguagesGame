using System.Collections.Generic;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Units;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// One option in a node of the search tree: changes a simulated position and,
    /// once chosen, becomes a real map command.
    /// </summary>
    public abstract class AIAction
    {
        public abstract void Apply(AISimulation state);

        /// <summary>Null when the action does not issue a command (ending the turn).</summary>
        public abstract IMapCommand ToCommand(int playerId);
    }

    /// <summary>Do nothing: ends the turn at the root of the tree, skips the ply deeper in it.</summary>
    public sealed class AIPassAction : AIAction
    {
        public override void Apply(AISimulation state) { }

        public override IMapCommand ToCommand(int playerId) => null;
    }

    public sealed class AIMoveAction : AIAction
    {
        private readonly int _armyIndex;

        public AIMoveAction(int armyIndex, int armyId, int owner, int fromTile, int toTile)
        {
            _armyIndex = armyIndex;
            ArmyId = armyId;
            Owner = owner;
            FromTile = fromTile;
            ToTile = toTile;
        }

        public int ArmyId { get; }
        public int Owner { get; }
        public int FromTile { get; }
        public int ToTile { get; }

        public override void Apply(AISimulation state) => state.MoveArmy(_armyIndex, ToTile);

        public override IMapCommand ToCommand(int playerId) => new MoveArmyCommand(playerId, ArmyId, ToTile);
    }

    public sealed class AIBuildAction : AIAction
    {
        public AIBuildAction(AIBuildOption option)
        {
            Option = option;
        }

        public AIBuildOption Option { get; }

        public override void Apply(AISimulation state) => state.Build(Option);

        public override IMapCommand ToCommand(int playerId) =>
            new BuildUpgradeCommand(playerId, Option.TileId, Option.Upgrade.Id);
    }

    public sealed class AIRecruitAction : AIAction
    {
        public AIRecruitAction(AIRecruitOption option)
        {
            Option = option;
        }

        public AIRecruitOption Option { get; }

        public override void Apply(AISimulation state) => state.Recruit(Option);

        public override IMapCommand ToCommand(int playerId) =>
            new RecruitCohortCommand(playerId, Option.TileId, Option.Unit.Id);
    }

    public sealed class AIReinforceAction : AIAction
    {
        private readonly int _armyIndex;

        public AIReinforceAction(int armyIndex, int armyId, int tile)
        {
            _armyIndex = armyIndex;
            ArmyId = armyId;
            Tile = tile;
        }

        public int ArmyId { get; }
        public int Tile { get; }

        public override void Apply(AISimulation state) => state.Reinforce(_armyIndex);

        public override IMapCommand ToCommand(int playerId) => new ReinforceArmyCommand(playerId, ArmyId);
    }

    public sealed class AIMergeAction : AIAction
    {
        private readonly int _sourceIndex;
        private readonly int _targetIndex;
        private readonly int _sourceId;
        private readonly int _targetId;

        public AIMergeAction(int sourceIndex, int sourceId, int targetIndex, int targetId, int tile)
        {
            _sourceIndex = sourceIndex;
            _sourceId = sourceId;
            _targetIndex = targetIndex;
            _targetId = targetId;
            Tile = tile;
        }

        public int Tile { get; }

        public override void Apply(AISimulation state) => state.Merge(_sourceIndex, _targetIndex);

        public override IMapCommand ToCommand(int playerId) => new MergeArmiesCommand(playerId, _sourceId, _targetId);
    }

    /// <summary>
    /// Lists the legal options of a position: everything the AI may do, or the moves of the opponents close enough to matter.
    /// </summary>
    public sealed class AIActionGenerator
    {
        public List<AIAction> GenerateOwn(AISimulation state)
        {
            var world = state.World;
            var actions = new List<AIAction> { new AIPassAction() };

            for (var i = 0; i < state.ArmyCount; i++)
            {
                var army = state.GetArmy(i);
                if (!army.Alive || army.Owner != world.PlayerId) continue;
                AddMoves(state, i, army, actions);
                if (army.IsPlanned) continue;
                AddReinforce(state, i, army, actions);
                AddMerges(state, i, army, actions);
            }

            foreach (var option in world.BuildOptions)
            {
                if (state.GetOwner(option.TileId) == world.PlayerId && !state.WasBuiltOn(option.TileId) &&
                    state.Resources.Covers(option.Upgrade.Cost)) actions.Add(new AIBuildAction(option));
            }

            foreach (var option in world.RecruitOptions)
            {
                if (state.GetOwner(option.TileId) == world.PlayerId && state.Resources.Covers(option.Cost) &&
                    !state.HasHostileArmy(option.TileId, world.PlayerId)) actions.Add(new AIRecruitAction(option));
            }
            return actions;
        }

        public List<AIAction> GenerateOpponents(AISimulation state)
        {
            var world = state.World;
            var actions = new List<AIAction> { new AIPassAction() };
            for (var i = 0; i < state.ArmyCount; i++)
            {
                var army = state.GetArmy(i);
                if (!army.Alive || army.Owner == world.PlayerId || army.Owner == Players.PlayerData.NeutralId) continue;
                if (IsRelevant(state, army)) AddMoves(state, i, army, actions);
            }
            return actions;
        }

        private static bool IsRelevant(AISimulation state, AIArmyState enemy)
        {
            var world = state.World;
            if (world.CapitalTile >= 0 && world.AreNear(enemy.Tile, world.CapitalTile)) return true;
            for (var i = 0; i < state.ArmyCount; i++)
            {
                var army = state.GetArmy(i);
                if (army.Alive && army.Owner == world.PlayerId && world.AreNear(enemy.Tile, army.Tile)) return true;
            }
            return false;
        }

        private static void AddMoves(AISimulation state, int index, AIArmyState army, List<AIAction> actions)
        {
            if (army.Moved || army.IsPlanned) return;
            foreach (var target in state.World.Neighbours[army.Tile])
            {
                if (state.CanStep(army.Owner, army.Tile, target))
                    actions.Add(new AIMoveAction(index, army.ArmyId, army.Owner, army.Tile, target));
            }
        }

        private static void AddReinforce(AISimulation state, int index, AIArmyState army, List<AIAction> actions)
        {
            var world = state.World;
            if (army.ReinforceGold <= 0 || state.GetOwner(army.Tile) != world.PlayerId) return;
            if (world.SettlementWeight[army.Tile] <= 0f) return;
            if (state.Resources.Covers(new ResourceBundle(army.ReinforceGold, 0, 0, 0)))
                actions.Add(new AIReinforceAction(index, army.ArmyId, army.Tile));
        }

        private static void AddMerges(AISimulation state, int index, AIArmyState army, List<AIAction> actions)
        {
            for (var other = index + 1; other < state.ArmyCount; other++)
            {
                var target = state.GetArmy(other);
                if (!target.Alive || target.Owner != army.Owner || target.Tile != army.Tile || target.IsPlanned) continue;
                // Each army has a commander: merging leaves one of them out.
                if (army.Cohorts + target.Cohorts - 1 > UnitsConfig.MaxCohortsPerArmy) continue;
                actions.Add(new AIMergeAction(index, army.ArmyId, other, target.ArmyId, army.Tile));
            }
        }
    }
}
