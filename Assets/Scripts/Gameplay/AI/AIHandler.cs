using System;
using System.Collections.Generic;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Logic;
using LearnLanguages.Settings;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Listens to the turns of AI players, asks the AI of the matching difficulty for commands and queues them.
    /// </summary>
    public sealed class AIHandler : IDisposable
    {
        private readonly Dictionary<GameDifficulty, BaseAILogic> _logics = new Dictionary<GameDifficulty, BaseAILogic>();
        private readonly List<Player> _subscribed = new List<Player>();
        private readonly IPlayersRegistry _players;
        private readonly IMapCommandProcessor _processor;
        private readonly IGameMap _map;
        private readonly IArmiesRegistry _armies;
        private readonly IEconomyService _economy;
        private readonly IConstructionRules _construction;
        private readonly IMovementRules _movement;
        private readonly UnitsConfig _units;
        private readonly IRandomService _random;

        public AIHandler(List<BaseAILogic> logics, IPlayersRegistry players, IMapCommandProcessor processor, IGameMap map,
            IArmiesRegistry armies, IEconomyService economy, IConstructionRules construction, IMovementRules movement,
            UnitsConfig units, IRandomService random)
        {
            foreach (var logic in logics) _logics[logic.Difficulty] = logic;
            _players = players;
            _processor = processor;
            _map = map;
            _armies = armies;
            _economy = economy;
            _construction = construction;
            _movement = movement;
            _units = units;
            _random = random;
        }

        /// <summary>Called once players exist (new game or loaded save).</summary>
        public void Attach()
        {
            Detach();
            foreach (var player in _players.Players)
            {
                if (player.IsHuman) continue;
                player.OnPlayersTurn += HandleTurn;
                _subscribed.Add(player);
            }
        }

        public void Dispose() => Detach();

        private void Detach()
        {
            foreach (var player in _subscribed) player.OnPlayersTurn -= HandleTurn;
            _subscribed.Clear();
        }

        private void HandleTurn(Player player)
        {
            var data = player.Data;
            var context = new AIContext(data, _map, _armies, _players, _economy, _construction, _movement, _units, _random);
            var logic = _logics.TryGetValue(data.AiDifficulty, out var found) ? found : _logics[GameDifficulty.Medium];

            foreach (var command in logic.Decide(context)) _processor.Enqueue(command);
            _processor.Enqueue(new EndTurnCommand(data.Id));
        }
    }
}
