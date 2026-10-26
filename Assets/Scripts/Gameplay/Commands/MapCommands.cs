using System;
using LearnLanguages.Gameplay.Events;

namespace LearnLanguages.Gameplay.Commands
{
    /// <summary>
    /// An action of a player on the global map. Humans issue them through the interface, the AI through its logic.
    /// </summary>
    public interface IMapCommand
    {
        int PlayerId { get; }
        bool CanExecute(MapCommandContext context);
        void Execute(MapCommandContext context, Action onComplete);
    }

    public abstract class MapCommand : IMapCommand
    {
        protected MapCommand(int playerId)
        {
            PlayerId = playerId;
        }

        public int PlayerId { get; }

        public bool CanExecute(MapCommandContext context) =>
            context.Turns.Current != null && context.Turns.Current.Id == PlayerId && Validate(context);

        public abstract void Execute(MapCommandContext context, Action onComplete);

        protected abstract bool Validate(MapCommandContext context);
    }

    public sealed class MoveArmyCommand : MapCommand
    {
        public MoveArmyCommand(int playerId, int armyId, int targetTileId) : base(playerId)
        {
            ArmyId = armyId;
            TargetTileId = targetTileId;
        }

        public int ArmyId { get; }
        public int TargetTileId { get; }

        protected override bool Validate(MapCommandContext context)
        {
            var army = context.Armies.Get(ArmyId);
            return army != null && army.OwnerId == PlayerId && context.Movement.CanMove(army, context.Map.Get(TargetTileId));
        }

        public override void Execute(MapCommandContext context, Action onComplete) =>
            context.War.EnterTile(context.Armies.Get(ArmyId), context.Map.Get(TargetTileId), onComplete);
    }

    public sealed class BuildUpgradeCommand : MapCommand
    {
        public BuildUpgradeCommand(int playerId, int tileId, string upgradeId) : base(playerId)
        {
            TileId = tileId;
            UpgradeId = upgradeId;
        }

        public int TileId { get; }
        public string UpgradeId { get; }

        protected override bool Validate(MapCommandContext context)
        {
            var upgrade = context.Upgrades.GetById(UpgradeId);
            return upgrade != null && context.Construction.GetBuildBlocker(context.Map.Get(TileId), upgrade,
                context.Players.Get(PlayerId)) == null;
        }

        public override void Execute(MapCommandContext context, Action onComplete)
        {
            var tile = context.Map.Get(TileId);
            var upgrade = context.Upgrades.GetById(UpgradeId);
            var player = context.Players.Get(PlayerId);
            player.TrySpend(upgrade.Cost);
            tile.AddUpgrade(upgrade);
            context.EventBus.Publish(new TileUpgradedEvent(tile, upgrade));
            context.EventBus.Publish(new ResourcesChangedEvent(player));
            onComplete();
        }
    }

    public sealed class RecruitCohortCommand : MapCommand
    {
        public RecruitCohortCommand(int playerId, int tileId, string unitId) : base(playerId)
        {
            TileId = tileId;
            UnitId = unitId;
        }

        public int TileId { get; }
        public string UnitId { get; }

        protected override bool Validate(MapCommandContext context)
        {
            var unit = context.Units.GetById(UnitId);
            var tile = context.Map.Get(TileId);
            if (unit == null || tile == null || tile.HasHostileArmy(PlayerId)) return false;
            return context.Construction.GetRecruitBlocker(tile, unit, context.Players.Get(PlayerId)) == null;
        }

        public override void Execute(MapCommandContext context, Action onComplete)
        {
            var tile = context.Map.Get(TileId);
            var unit = context.Units.GetById(UnitId);
            var player = context.Players.Get(PlayerId);
            player.TrySpend(context.Construction.GetRecruitCost(tile, unit));

            Units.Army target = null;
            foreach (var army in tile.GetArmiesOf(PlayerId))
                if (!army.IsFull) target = army;

            if (target != null)
            {
                target.AddCohort(context.ArmyFactory.CreateCohort(unit));
                context.EventBus.Publish(new ArmyChangedEvent(target));
            }
            else
            {
                var army = context.ArmyFactory.CreateArmy(PlayerId, TileId, new[] { unit });
                army.MarkMoved();
            }
            context.EventBus.Publish(new ResourcesChangedEvent(player));
            onComplete();
        }
    }

    public sealed class ReinforceArmyCommand : MapCommand
    {
        public ReinforceArmyCommand(int playerId, int armyId) : base(playerId)
        {
            ArmyId = armyId;
        }

        public int ArmyId { get; }

        protected override bool Validate(MapCommandContext context)
        {
            var army = context.Armies.Get(ArmyId);
            if (army == null || army.OwnerId != PlayerId) return false;
            var tile = context.Map.Get(army.TileId);
            var cost = context.Construction.GetReinforceCost(army);
            return tile.OwnerId == PlayerId && tile.HasSettlement && cost.Gold > 0 &&
                   context.Players.Get(PlayerId).Resources.Covers(cost);
        }

        public override void Execute(MapCommandContext context, Action onComplete)
        {
            var army = context.Armies.Get(ArmyId);
            var player = context.Players.Get(PlayerId);
            player.TrySpend(context.Construction.GetReinforceCost(army));
            foreach (var cohort in army.Cohorts) cohort.Reinforce(cohort.Killed);
            context.EventBus.Publish(new ArmyChangedEvent(army));
            context.EventBus.Publish(new ResourcesChangedEvent(player));
            onComplete();
        }
    }

    public sealed class MergeArmiesCommand : MapCommand
    {
        public MergeArmiesCommand(int playerId, int sourceArmyId, int targetArmyId) : base(playerId)
        {
            SourceArmyId = sourceArmyId;
            TargetArmyId = targetArmyId;
        }

        public int SourceArmyId { get; }
        public int TargetArmyId { get; }

        protected override bool Validate(MapCommandContext context)
        {
            var source = context.Armies.Get(SourceArmyId);
            var target = context.Armies.Get(TargetArmyId);
            return source != null && target != null && source != target && source.OwnerId == PlayerId &&
                   target.OwnerId == PlayerId && source.TileId == target.TileId;
        }

        public override void Execute(MapCommandContext context, Action onComplete)
        {
            var source = context.Armies.Get(SourceArmyId);
            var target = context.Armies.Get(TargetArmyId);
            foreach (var cohort in new System.Collections.Generic.List<Units.Cohort>(source.Cohorts))
            {
                if (cohort.IsCommander && target.Commander != null) continue;
                if (!target.AddCohort(cohort)) continue;
                source.RemoveCohort(cohort);
            }
            if (source.HasMoved) target.MarkMoved();

            if (source.IsEmpty || (source.Cohorts.Count == 1 && source.Commander != null))
            {
                context.Armies.Remove(source);
                context.EventBus.Publish(new ArmyDestroyedEvent(source));
            }
            else
            {
                context.EventBus.Publish(new ArmyChangedEvent(source));
            }
            context.EventBus.Publish(new ArmyChangedEvent(target));
            onComplete();
        }
    }

    public sealed class EndTurnCommand : MapCommand
    {
        public EndTurnCommand(int playerId) : base(playerId) { }

        protected override bool Validate(MapCommandContext context) => true;

        public override void Execute(MapCommandContext context, Action onComplete)
        {
            onComplete();
            context.Turns.EndTurn();
        }
    }
}
