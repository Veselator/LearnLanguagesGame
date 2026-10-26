using LearnLanguages.Battle.Logic;
using LearnLanguages.Battle.Visual;
using LearnLanguages.Core.Events;
using LearnLanguages.Core.Input;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Localization;
using LearnLanguages.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Battle
{
    /// <summary>
    /// Battle screen interface: sides and morale, phase, order buttons, command grid and the result panel.
    /// </summary>
    public sealed class BattleHudView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _phase;
        [SerializeField] private TMP_Text _attackerName;
        [SerializeField] private TMP_Text _defenderName;
        [SerializeField] private Image _attackerMorale;
        [SerializeField] private Image _defenderMorale;
        [SerializeField] private TMP_Text _selectionInfo;
        [SerializeField] private Button _startBattle;
        [SerializeField] private Button _execute;
        [SerializeField] private Button _hold;
        [SerializeField] private Button _defend;
        [SerializeField] private Button _shout;
        [SerializeField] private Button _shoot;
        [SerializeField] private Button _retreat;
        [SerializeField] private BattleGridPanelView _grid;
        [SerializeField] private GameObject _resultRoot;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Button _continue;

        private IEventBus _eventBus;
        private IBattleController _controller;
        private IBattleSelection _selection;
        private ILocalizationService _localization;
        private BattleGridCellView.Pool _cellPool;
        private IInputService _input;
        private ILanguageTaskService _tasks;
        private BattleModel _model;

        [Inject]
        public void Construct(IEventBus eventBus, IBattleController controller, IBattleSelection selection,
            ILocalizationService localization, BattleGridCellView.Pool cellPool, IInputService input,
            ILanguageTaskService tasks)
        {
            _input = input;
            _tasks = tasks;
            _eventBus = eventBus;
            _controller = controller;
            _selection = selection;
            _localization = localization;
            _cellPool = cellPool;

            _eventBus.Subscribe<BattleStartedEvent>(OnStarted);
            _eventBus.Subscribe<BattleStateChangedEvent>(OnStateChanged);
            _eventBus.Subscribe<BattleTickResolvedEvent>(OnTickResolved);
            _eventBus.Subscribe<BattleFinishedEvent>(OnFinished);
            _eventBus.Subscribe<BattleClosedEvent>(OnClosed);
            _selection.Changed += Refresh;
            _root.SetActive(false);
        }

        private void Awake()
        {
            _startBattle.onClick.AddListener(() => _controller.ConfirmDeployment());
            _execute.onClick.AddListener(ExecuteOrders);
            _hold.onClick.AddListener(() => OrderSelected(c => _model.OrderHold(c)));
            _defend.onClick.AddListener(() => OrderSelected(c => _model.OrderDefend(c)));
            _shout.onClick.AddListener(() =>
            {
                var cohort = _selection.Selected;
                if (cohort == null) return;
                _selection.Select(null);
                _controller.RequestShout(cohort);
            });
            _shoot.onClick.AddListener(() => _selection.SetMode(BattleClickMode.Shoot));
            _retreat.onClick.AddListener(() => _controller.Retreat());
            _continue.onClick.AddListener(() => _controller.Close());
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<BattleStartedEvent>(OnStarted);
            _eventBus.Unsubscribe<BattleStateChangedEvent>(OnStateChanged);
            _eventBus.Unsubscribe<BattleTickResolvedEvent>(OnTickResolved);
            _eventBus.Unsubscribe<BattleFinishedEvent>(OnFinished);
            _eventBus.Unsubscribe<BattleClosedEvent>(OnClosed);
            _selection.Changed -= Refresh;
        }

        private void Update()
        {
            // Space advances the battle: start after deployment, execute orders, leave after the result.
            if (_model == null || _tasks.IsBusy || !_input.NextTurnPressed) return;
            switch (_model.Phase)
            {
                case BattlePhase.Deployment:
                    _controller.ConfirmDeployment();
                    break;
                case BattlePhase.Finished:
                    _controller.Close();
                    break;
                default:
                    if (_controller.IsWaitingForHumanOrders) ExecuteOrders();
                    break;
            }
        }

        private void ExecuteOrders()
        {
            _selection.Select(null);
            _controller.ExecuteOrders();
            Refresh();
        }

        private void OrderSelected(System.Action<BattleCohort> order)
        {
            var cohort = _selection.Selected;
            if (cohort == null || _model == null) return;
            order(cohort);
            _controller.NotifyOrdersChanged();
            _selection.Select(null);
        }

        private void OnStarted(BattleStartedEvent e)
        {
            _model = e.Model;
            _root.SetActive(true);
            _resultRoot.SetActive(false);
            var setup = e.Model.Setup;
            _title.text = _localization.Format("battle.title", setup.Tile.Data.Name);
            _attackerName.text = UiFormat.Colored(setup.Attacker.Player.Name, setup.Attacker.Player.Color);
            _defenderName.text = UiFormat.Colored(setup.Defender.Player.Name, setup.Defender.Player.Color);
            _grid.Build(e.Model, _cellPool, _selection);
            _selection.Select(null);
            Refresh();
        }

        private void OnStateChanged(BattleStateChangedEvent e)
        {
            if (e.Model == _model) Refresh();
        }

        private void OnTickResolved(BattleTickResolvedEvent e)
        {
            if (e.Model == _model) Refresh();
        }

        private void OnFinished(BattleFinishedEvent e)
        {
            _resultRoot.SetActive(true);
            var humanWon = e.Result.WinnerSide.IsHuman;
            _resultText.text = _localization.Get(humanWon ? "battle.victory" : "battle.defeat");
            Refresh();
        }

        private void OnClosed(BattleClosedEvent e)
        {
            _grid.Clear(_cellPool);
            _model = null;
            _root.SetActive(false);
        }

        private void Refresh()
        {
            if (_model == null) return;
            var phase = _model.Phase;
            var waiting = _controller.IsWaitingForHumanOrders;
            var selected = _selection.Selected;
            var ownSelected = selected != null && selected.Side == _controller.HumanSide && selected.IsOnField;

            _phase.text = _localization.Format("battle.phase", _model.Turn, _localization.Get("battle.phase." + phase.ToString().ToLowerInvariant()),
                phase == BattlePhase.Action ? $"{_model.Tick}/{_model.Config.TicksPerTurn}" : string.Empty);
            _attackerMorale.fillAmount = _model.GetSideMorale(BattleSideId.Attacker);
            _defenderMorale.fillAmount = _model.GetSideMorale(BattleSideId.Defender);

            _startBattle.gameObject.SetActive(phase == BattlePhase.Deployment);
            _execute.gameObject.SetActive(phase != BattlePhase.Deployment && phase != BattlePhase.Finished);
            _execute.interactable = waiting;
            _hold.interactable = waiting && ownSelected;
            _defend.interactable = waiting && ownSelected;
            _shout.interactable = waiting && ownSelected;
            _shoot.interactable = waiting && ownSelected && selected.Type.IsRanged;
            _retreat.interactable = phase == BattlePhase.Deployment || waiting;

            _selectionInfo.text = selected == null ? string.Empty : DescribeCohort(selected);
            _grid.Refresh(_model, _selection);
        }

        private string DescribeCohort(BattleCohort cohort)
        {
            var player = _model.Setup.Get(cohort.Side).Player;
            return $"{UiFormat.Colored(_localization.Get(cohort.Type.NameKey), player.Color)}  " +
                   _localization.Format("battle.cohort_info", cohort.Healthy, cohort.MaxSoldiers,
                       UiFormat.Percent(cohort.Organization), _localization.Get("order." + cohort.Order.Type.ToString().ToLowerInvariant()));
        }
    }
}
