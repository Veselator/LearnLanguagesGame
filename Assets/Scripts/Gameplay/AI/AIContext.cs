using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Read-only view of the world handed to an AI when its turn starts.
    /// </summary>
    public sealed class AIContext
    {
        public AIContext(PlayerData player, IGameMap map, IArmiesRegistry armies, IPlayersRegistry players,
            IEconomyService economy, IConstructionRules construction, IMovementRules movement, UnitsConfig units,
            IRandomService random)
        {
            Player = player;
            Map = map;
            Armies = armies;
            Players = players;
            Economy = economy;
            Construction = construction;
            Movement = movement;
            Units = units;
            Random = random;
        }

        public PlayerData Player { get; }
        public IGameMap Map { get; }
        public IArmiesRegistry Armies { get; }
        public IPlayersRegistry Players { get; }
        public IEconomyService Economy { get; }
        public IConstructionRules Construction { get; }
        public IMovementRules Movement { get; }
        public UnitsConfig Units { get; }
        public IRandomService Random { get; }
    }
}
