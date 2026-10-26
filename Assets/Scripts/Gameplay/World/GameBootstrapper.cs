using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.AI;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Turns;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Localization;
using LearnLanguages.Map.Data;
using LearnLanguages.Settings;
using Zenject;

namespace LearnLanguages.Gameplay.World
{
    /// <summary>
    /// Starts the match when the game scene loads: builds or restores the world, then hands over to the turn manager.
    /// </summary>
    public sealed class GameBootstrapper : IInitializable
    {
        private readonly IGameSession _session;
        private readonly IGameWorldBuilder _builder;
        private readonly IGlobalTurnsManager _turns;
        private readonly AIHandler _aiHandler;
        private readonly IEventBus _eventBus;
        private readonly ILocalizationService _localization;
        private readonly ILanguageStatistics _statistics;
        private readonly MapsConfig _maps;

        public GameBootstrapper(IGameSession session, IGameWorldBuilder builder, IGlobalTurnsManager turns,
            AIHandler aiHandler, IEventBus eventBus, ILocalizationService localization, ILanguageStatistics statistics,
            MapsConfig maps)
        {
            _session = session;
            _builder = builder;
            _turns = turns;
            _aiHandler = aiHandler;
            _eventBus = eventBus;
            _localization = localization;
            _statistics = statistics;
            _maps = maps;
        }

        public void Initialize()
        {
            // Allows starting the game scene directly from the editor.
            if (!_session.HasSettings)
            {
                _session.StartNew(new GameSettings(_maps.Maps[0].Id, Language.French, Language.Russian, LanguageLevel.A1,
                    GameDifficulty.Medium, 2, System.Environment.TickCount));
            }

            var settings = _session.Settings;
            _localization.SetLanguage(settings.InterfaceLanguage);
            _builder.PrepareMap(settings);

            var save = _session.PendingSave;
            if (save != null)
            {
                _builder.Restore(save);
                _statistics.Restore(save.TasksAnswered, save.TasksCorrect);
                _session.ConsumePendingSave();
            }
            else
            {
                _builder.CreateNew(settings);
            }

            _aiHandler.Attach();
            _eventBus.Publish(new GameReadyEvent());

            if (save != null) _turns.Start(save.CurrentPlayerIndex, save.Round, false);
            else _turns.Start(0, 1, false);
        }
    }
}
