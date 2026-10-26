using LearnLanguages.Battle.Logic;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Conquest;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Learning.Tasks;

namespace LearnLanguages.Gameplay.Turns
{
    public interface IHumanTurnState
    {
        /// <summary>The human may act on the global map right now.</summary>
        bool CanAct { get; }
        PlayerData Human { get; }
    }

    public sealed class HumanTurnState : IHumanTurnState
    {
        private readonly IGlobalTurnsManager _turns;
        private readonly IBattleService _battles;
        private readonly IMapCommandProcessor _processor;
        private readonly IConquestService _conquest;
        private readonly ILanguageTaskService _tasks;
        private readonly IPlayersRegistry _players;

        public HumanTurnState(IGlobalTurnsManager turns, IBattleService battles, IMapCommandProcessor processor,
            IConquestService conquest, ILanguageTaskService tasks, IPlayersRegistry players)
        {
            _turns = turns;
            _battles = battles;
            _processor = processor;
            _conquest = conquest;
            _tasks = tasks;
            _players = players;
        }

        public PlayerData Human => _players.HumanPlayer?.Data;

        public bool CanAct =>
            _turns.Current != null && _turns.Current.IsHuman && !_battles.IsBattleInProgress && !_processor.IsBusy &&
            !_conquest.IsGameOver && !_tasks.IsBusy;
    }
}
