using System.Collections.Generic;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Localization;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.Conquest
{
    public interface IConquestService
    {
        bool IsGameOver { get; }
        void Capture(GameTile tile, int newOwnerId);
        void DestroyArmy(Army army);
    }

    /// <summary>
    /// Ownership changes, elimination of players who lost their capital and the victory check.
    /// </summary>
    public sealed class ConquestService : IConquestService
    {
        private readonly IGameMap _map;
        private readonly ArmiesRegistry _armies;
        private readonly IPlayersRegistry _players;
        private readonly IEventBus _eventBus;
        private readonly ILocalizationService _localization;

        public ConquestService(IGameMap map, ArmiesRegistry armies, IPlayersRegistry players, IEventBus eventBus,
            ILocalizationService localization)
        {
            _map = map;
            _armies = armies;
            _players = players;
            _eventBus = eventBus;
            _localization = localization;
        }

        public bool IsGameOver { get; private set; }

        public void Capture(GameTile tile, int newOwnerId)
        {
            if (!tile.IsLand || tile.OwnerId == newOwnerId) return;
            var previousOwner = tile.OwnerId;
            tile.SetOwner(newOwnerId);
            _eventBus.Publish(new TileOwnerChangedEvent(tile, previousOwner));

            var loser = _players.GetPlayer(previousOwner);
            if (loser != null && loser.Data.IsAlive && loser.Data.CapitalTileId == tile.Id)
                Eliminate(loser.Data, _players.Get(newOwnerId));
        }

        public void DestroyArmy(Army army)
        {
            _armies.Remove(army);
            _eventBus.Publish(new ArmyDestroyedEvent(army));
        }

        private void Eliminate(PlayerData loser, PlayerData conqueror)
        {
            loser.Eliminate();

            foreach (var tile in _map.Tiles)
            {
                if (tile.OwnerId != loser.Id) continue;
                tile.SetOwner(conqueror.Id);
                _eventBus.Publish(new TileOwnerChangedEvent(tile, loser.Id));
            }

            foreach (var army in new List<Army>(_armies.All))
                if (army.OwnerId == loser.Id) DestroyArmy(army);

            _eventBus.Publish(new PlayerEliminatedEvent(loser, conqueror));
            _eventBus.Publish(new NotificationEvent(_localization.Format("notify.eliminated", loser.Name, conqueror.Name)));
            CheckGameOver();
        }

        private void CheckGameOver()
        {
            if (IsGameOver) return;
            var human = _players.HumanPlayer;
            if (human != null && !human.Data.IsAlive)
            {
                IsGameOver = true;
                _eventBus.Publish(new GameOverEvent(null, false));
                return;
            }

            if (_players.AliveCount > 1) return;
            foreach (var player in _players.Players)
            {
                if (!player.Data.IsAlive) continue;
                IsGameOver = true;
                _eventBus.Publish(new GameOverEvent(player.Data, player.IsHuman));
                return;
            }
        }
    }
}
