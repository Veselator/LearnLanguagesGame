using System;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Conquest;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Turns;
using LearnLanguages.Gameplay.World;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Localization;
using LearnLanguages.Settings;
using Zenject;

namespace LearnLanguages.Save
{
    public interface IGameSaver
    {
        bool CanSave { get; }
        void SaveNow();
    }

    /// <summary>
    /// Saves at the start of every human turn and on request. Battles are never saved mid-way.
    /// </summary>
    public sealed class GameSaveController : IGameSaver, IInitializable, IDisposable
    {
        private readonly ISaveService _saves;
        private readonly IGameWorldBuilder _builder;
        private readonly IGlobalTurnsManager _turns;
        private readonly IGameSession _session;
        private readonly ILanguageStatistics _statistics;
        private readonly IConquestService _conquest;
        private readonly IEventBus _eventBus;
        private readonly ILocalizationService _localization;
        private readonly IHumanTurnState _turnState;

        public GameSaveController(ISaveService saves, IGameWorldBuilder builder, IGlobalTurnsManager turns,
            IGameSession session, ILanguageStatistics statistics, IConquestService conquest, IEventBus eventBus,
            ILocalizationService localization, IHumanTurnState turnState)
        {
            _turnState = turnState;
            _saves = saves;
            _builder = builder;
            _turns = turns;
            _session = session;
            _statistics = statistics;
            _conquest = conquest;
            _eventBus = eventBus;
            _localization = localization;
        }

        /// <summary>Only between actions of the human player: never during battles, exercises or AI turns.</summary>
        public bool CanSave => _turnState.CanAct;

        public void Initialize()
        {
            _eventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            _eventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        public void SaveNow()
        {
            if (!CanSave) return;
            Write();
            _eventBus.Publish(new NotificationEvent(_localization.Get("notify.saved")));
        }

        private void OnTurnStarted(TurnStartedEvent e)
        {
            if (e.Player.IsHuman && !_conquest.IsGameOver) Write();
        }

        private void OnGameOver(GameOverEvent e) => _saves.Delete();

        private void Write()
        {
            var save = _builder.Capture(_session.Settings, _turns.Round, _turns.CurrentIndex);
            save.TasksAnswered = _statistics.Answered;
            save.TasksCorrect = _statistics.Correct;
            _saves.Save(save);
        }
    }
}
