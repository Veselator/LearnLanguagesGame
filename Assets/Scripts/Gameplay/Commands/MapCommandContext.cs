using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Conquest;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Turns;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.Commands
{
    /// <summary>
    /// Services a map command may use while executing.
    /// </summary>
    public sealed class MapCommandContext
    {
        public MapCommandContext(IGameMap map, ArmiesRegistry armies, IPlayersRegistry players, IMovementRules movement,
            IConstructionRules construction, IArmyFactory armyFactory, IWarService war, IEventBus eventBus,
            UnitsConfig units, UpgradesConfig upgrades, IGlobalTurnsManager turns)
        {
            Map = map;
            Armies = armies;
            Players = players;
            Movement = movement;
            Construction = construction;
            ArmyFactory = armyFactory;
            War = war;
            EventBus = eventBus;
            Units = units;
            Upgrades = upgrades;
            Turns = turns;
        }

        public IGameMap Map { get; }
        public ArmiesRegistry Armies { get; }
        public IPlayersRegistry Players { get; }
        public IMovementRules Movement { get; }
        public IConstructionRules Construction { get; }
        public IArmyFactory ArmyFactory { get; }
        public IWarService War { get; }
        public IEventBus EventBus { get; }
        public UnitsConfig Units { get; }
        public UpgradesConfig Upgrades { get; }
        public IGlobalTurnsManager Turns { get; }
    }
}
