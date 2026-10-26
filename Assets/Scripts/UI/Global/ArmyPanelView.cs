using System.Text;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Turns;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Localization;
using LearnLanguages.Map.Logic;
using LearnLanguages.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Global
{
    /// <summary>
    /// Cohorts of the selected army with reinforce and merge actions.
    /// </summary>
    public sealed class ArmyPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _cohorts;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private Button _reinforce;
        [SerializeField] private TMP_Text _reinforceLabel;
        [SerializeField] private Button _merge;

        private IEventBus _eventBus;
        private IPlayersRegistry _players;
        private IGameMap _map;
        private IConstructionRules _construction;
        private IMapCommandProcessor _processor;
        private IHumanTurnState _turnState;
        private ILocalizationService _localization;
        private UiFormat _format;
        private Army _army;
        private bool _lastCanAct;

        [Inject]
        public void Construct(IEventBus eventBus, IPlayersRegistry players, IGameMap map, IConstructionRules construction,
            IMapCommandProcessor processor, IHumanTurnState turnState, ILocalizationService localization, UiFormat format)
        {
            _eventBus = eventBus;
            _players = players;
            _map = map;
            _construction = construction;
            _processor = processor;
            _turnState = turnState;
            _localization = localization;
            _format = format;
            _eventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            _root.SetActive(false);
        }

        private void Awake()
        {
            _reinforce.onClick.AddListener(() =>
            {
                if (_army != null) _processor.Enqueue(new ReinforceArmyCommand(_army.OwnerId, _army.Id));
            });
            _merge.onClick.AddListener(Merge);
        }

        private void OnDestroy() => _eventBus?.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);

        private void Update()
        {
            var canAct = _turnState.CanAct;
            if (canAct == _lastCanAct) return;
            _lastCanAct = canAct;
            if (_army != null) Rebuild();
        }

        private void OnSelectionChanged(SelectionChangedEvent e)
        {
            _army = e.Army;
            _root.SetActive(_army != null);
            if (_army != null) Rebuild();
        }

        private void Rebuild()
        {
            var owner = _players.Get(_army.OwnerId);
            _title.text = _localization.Format("army.title", UiFormat.Colored(owner.Name, owner.Color), _army.Cohorts.Count) +
                          "  " + _localization.Format("army.morale", UiFormat.Percent(_army.Morale));

            var text = new StringBuilder();
            foreach (var cohort in _army.Cohorts)
            {
                text.Append(_localization.Get(cohort.Type.NameKey)).Append(": ")
                    .Append(cohort.Healthy).Append('/').Append(cohort.MaxSoldiers);
                if (cohort.Wounded > 0) text.Append(' ').Append(_localization.Format("army.wounded", cohort.Wounded));
                text.Append("  ").Append(_localization.Format("army.organization", UiFormat.Percent(cohort.Organization)));
                text.AppendLine();
            }
            _cohorts.text = text.ToString();

            var human = _turnState.Human;
            var isOwn = human != null && _army.OwnerId == human.Id;
            var canAct = isOwn && _turnState.CanAct;
            _hint.text = isOwn ? _localization.Get(_army.HasMoved ? "army.moved" : "army.move_hint") : string.Empty;

            var tile = _map.Get(_army.TileId);
            var cost = _construction.GetReinforceCost(_army);
            _reinforce.gameObject.SetActive(isOwn);
            _reinforceLabel.text = _localization.Format("army.reinforce", _format.Cost(cost));
            _reinforce.interactable = canAct && cost.Gold > 0 && tile.OwnerId == human.Id && tile.HasSettlement &&
                                      human.Resources.Covers(cost);

            _merge.gameObject.SetActive(isOwn);
            _merge.interactable = canAct && FindMergeTarget() != null;
        }

        private Army FindMergeTarget()
        {
            foreach (var other in _map.Get(_army.TileId).Armies)
                if (other != _army && other.OwnerId == _army.OwnerId) return other;
            return null;
        }

        private void Merge()
        {
            var target = FindMergeTarget();
            if (target != null) _processor.Enqueue(new MergeArmiesCommand(_army.OwnerId, _army.Id, target.Id));
        }
    }
}
