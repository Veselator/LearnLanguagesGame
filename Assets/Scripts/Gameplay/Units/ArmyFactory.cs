using System.Collections.Generic;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Map.Logic;
using UnityEngine;

namespace LearnLanguages.Gameplay.Units
{
    public interface IArmyFactory
    {
        Cohort CreateCohort(UnitTypeDefinition type, float strength = 1f);
        Army CreateArmy(int ownerId, int tileId, IEnumerable<UnitTypeDefinition> units, float strength = 1f, bool withCommander = true);
        Army CreateGarrison(GameTile tile);

        /// <summary>Gives an army that lost its commander a new (weaker) one.</summary>
        void EnsureCommander(Army army);
    }

    public sealed class ArmyFactory : IArmyFactory
    {
        private const float PromotedCommanderStrength = 0.4f;

        private readonly UnitsConfig _units;
        private readonly ArmiesRegistry _registry;
        private readonly IEventBus _eventBus;

        public ArmyFactory(UnitsConfig units, ArmiesRegistry registry, IEventBus eventBus)
        {
            _units = units;
            _registry = registry;
            _eventBus = eventBus;
        }

        public Cohort CreateCohort(UnitTypeDefinition type, float strength = 1f)
        {
            var healthy = Mathf.Max(1, Mathf.RoundToInt(type.MaxSoldiers * Mathf.Clamp01(strength)));
            return new Cohort(type, type.MaxSoldiers, healthy, 0, 1f);
        }

        public Army CreateArmy(int ownerId, int tileId, IEnumerable<UnitTypeDefinition> units, float strength = 1f,
            bool withCommander = true)
        {
            var army = new Army(_registry.ReserveId(), ownerId, tileId);
            if (withCommander) army.AddCohort(CreateCohort(_units.Commander, strength));
            foreach (var unit in units)
                if (unit != null) army.AddCohort(CreateCohort(unit, strength));

            _registry.Add(army);
            _eventBus.Publish(new ArmyCreatedEvent(army));
            return army;
        }

        public Army CreateGarrison(GameTile tile) =>
            CreateArmy(tile.OwnerId, tile.Id, _units.GetGarrison(tile.Settlement), _units.GarrisonStrength);

        public void EnsureCommander(Army army)
        {
            if (army.IsEmpty || army.Commander != null) return;
            if (army.IsFull) army.RemoveCohort(army.Cohorts[army.Cohorts.Count - 1]);
            army.AddCohort(CreateCohort(_units.Commander, PromotedCommanderStrength));
        }
    }
}
