using LearnLanguages.Core.Events;
using LearnLanguages.Core.Input;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Turns;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Localization;
using LearnLanguages.Save;
using LearnLanguages.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Global
{
    /// <summary>
    /// Resources of the human player, turn info and the main global-map buttons.
    /// </summary>
    public sealed class TopBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] _resources;
        [SerializeField] private TMP_Text _turnInfo;
        [SerializeField] private TMP_Text _statistics;
        [SerializeField] private Button _endTurn;
        [SerializeField] private Button _save;
        [SerializeField] private Button _menu;
        [SerializeField] private PauseMenuView _pauseMenu;

        private IEventBus _eventBus;
        private IPlayersRegistry _players;
        private IEconomyService _economy;
        private IGlobalTurnsManager _turns;
        private IMapCommandProcessor _processor;
        private IHumanTurnState _turnState;
        private IGameSaver _saver;
        private ILocalizationService _localization;
        private ILanguageStatistics _languageStatistics;
        private UiFormat _format;
        private IInputService _input;

        [Inject]
        public void Construct(IEventBus eventBus, IPlayersRegistry players, IEconomyService economy,
            IGlobalTurnsManager turns, IMapCommandProcessor processor, IHumanTurnState turnState, IGameSaver saver,
            ILocalizationService localization, ILanguageStatistics languageStatistics, UiFormat format, IInputService input)
        {
            _input = input;
            _eventBus = eventBus;
            _players = players;
            _economy = economy;
            _turns = turns;
            _processor = processor;
            _turnState = turnState;
            _saver = saver;
            _localization = localization;
            _languageStatistics = languageStatistics;
            _format = format;

            _eventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
            _eventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            _eventBus.Subscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
            _eventBus.Subscribe<LanguageTaskFinishedEvent>(OnTaskFinished);
        }

        private void Awake()
        {
            _endTurn.onClick.AddListener(EndTurn);
            _save.onClick.AddListener(() => _saver.SaveNow());
            _menu.onClick.AddListener(() => _pauseMenu.Toggle());
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
            _eventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            _eventBus.Unsubscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
            _eventBus.Unsubscribe<LanguageTaskFinishedEvent>(OnTaskFinished);
        }

        private void Update()
        {
            var canAct = _turnState.CanAct;
            _endTurn.interactable = canAct;
            _save.interactable = canAct && _saver.CanSave;
            if (canAct && _input.NextTurnPressed) EndTurn();
        }

        private void EndTurn()
        {
            var human = _turnState.Human;
            if (human != null && _turnState.CanAct) _processor.Enqueue(new EndTurnCommand(human.Id));
        }

        private void OnResourcesChanged(ResourcesChangedEvent e)
        {
            if (e.Player.IsHuman) RefreshResources();
        }

        private void OnTileOwnerChanged(TileOwnerChangedEvent e) => RefreshResources();

        private void OnTaskFinished(LanguageTaskFinishedEvent e) => RefreshStatistics();

        private void OnTurnStarted(TurnStartedEvent e)
        {
            var player = e.Player;
            _turnInfo.text = _localization.Format("ui.turn_info", e.Round, UiFormat.Colored(player.Name, player.Color));
            RefreshResources();
            RefreshStatistics();
        }

        private void RefreshResources()
        {
            var human = _players.HumanPlayer;
            if (human == null) return;
            var balance = _economy.CalculateIncome(human.Data.Id) - _economy.CalculateUpkeep(human.Data.Id);
            for (var i = 0; i < _resources.Length && i < 4; i++)
            {
                var type = (ResourceType)i;
                _resources[i].text = $"{_format.Resource(type, human.Data.Resources.Get(type))} ({_format.Signed(balance.Get(type))})";
            }
        }

        private void RefreshStatistics() =>
            _statistics.text = _localization.Format("ui.statistics", _languageStatistics.Correct, _languageStatistics.Answered);
    }
}
