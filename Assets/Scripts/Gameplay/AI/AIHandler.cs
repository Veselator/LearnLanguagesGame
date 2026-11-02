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
    /// Shows the recorded decisions of an AI turn and reports when the viewer is done with them.
    /// </summary>
    public interface IAIDecisionViewer
    {
        void Show(AIDecisionTrace trace, Action onClosed);
    }

    /// <summary>
    /// Listens to the turns of AI players, asks the planner for commands with the model of that player and queues them.
    /// In debug mode the planner's decisions are recorded and shown first.
    /// </summary>
    public sealed class AIHandler : IDisposable
    {
        private readonly AIPlanner _planner = new AIPlanner();
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
        private readonly IAIProfilesStore _profiles;
        private readonly IGameOptions _options;
        private readonly IAIDecisionViewer _viewer;

        public AIHandler(IAIProfilesStore profiles, IGameOptions options, IAIDecisionViewer viewer, IPlayersRegistry players, IMapCommandProcessor processor, IGameMap map,
            IArmiesRegistry armies, IEconomyService economy, IConstructionRules construction, IMovementRules movement,
            UnitsConfig units, IRandomService random)
        {
            _profiles = profiles;
            _options = options;
            _viewer = viewer;
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
            var profile = _profiles.GetById(data.AiProfileId) ?? _profiles.Pick(data.AiDifficulty, _random);

            if (!_options.DebugMode)
            {
                Enqueue(data.Id, _planner.Plan(context, profile, AINullDecisionObserver.Instance));
                return;
            }

            var trace = new AIDecisionTrace(data.Name, profile);
            var commands = _planner.Plan(context, profile, trace);
            _viewer.Show(trace, () => Enqueue(data.Id, commands));
        }

        private void Enqueue(int playerId, List<IMapCommand> commands)
        {
            foreach (var command in commands) _processor.Enqueue(command);
            _processor.Enqueue(new EndTurnCommand(playerId));
        }
    }
}
