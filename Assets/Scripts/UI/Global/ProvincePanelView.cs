using System.Text;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Selection;
using LearnLanguages.Gameplay.Turns;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Localization;
using LearnLanguages.Map.Logic;
using LearnLanguages.UI.Common;
using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.UI.Global
{
    /// <summary>
    /// Information about the selected province with construction and recruitment actions.
    /// </summary>
    public sealed class ProvincePanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _details;
        [SerializeField] private GameObject _buildSection;
        [SerializeField] private Transform _buildContainer;
        [SerializeField] private GameObject _recruitSection;
        [SerializeField] private Transform _recruitContainer;
        [SerializeField] private Transform _armiesContainer;

        private IEventBus _eventBus;
        private IPlayersRegistry _players;
        private IEconomyService _economy;
        private IConstructionRules _construction;
        private IMapCommandProcessor _processor;
        private IHumanTurnState _turnState;
        private ISelectionService _selection;
        private ILocalizationService _localization;
        private UnitsConfig _units;
        private UpgradesConfig _upgrades;
        private UiFormat _format;
        private ButtonList _buildButtons;
        private ButtonList _recruitButtons;
        private ButtonList _armyButtons;
        private GameTile _tile;
        private bool _lastCanAct;

        [Inject]
        public void Construct(IEventBus eventBus, IPlayersRegistry players, IEconomyService economy,
            IConstructionRules construction, IMapCommandProcessor processor, IHumanTurnState turnState,
            ISelectionService selection, ILocalizationService localization, UnitsConfig units, UpgradesConfig upgrades,
            UiFormat format, ActionButtonView.Pool buttonPool)
        {
            _eventBus = eventBus;
            _players = players;
            _economy = economy;
            _construction = construction;
            _processor = processor;
            _turnState = turnState;
            _selection = selection;
            _localization = localization;
            _units = units;
            _upgrades = upgrades;
            _format = format;
            _buildButtons = new ButtonList(_buildContainer, buttonPool);
            _recruitButtons = new ButtonList(_recruitContainer, buttonPool);
            _armyButtons = new ButtonList(_armiesContainer, buttonPool);
            _eventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            _root.SetActive(false);
        }

        private void OnDestroy() => _eventBus?.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);

        private void Update()
        {
            // Buttons depend on whether the human may act; rebuild when that changes.
            var canAct = _turnState.CanAct;
            if (canAct == _lastCanAct) return;
            _lastCanAct = canAct;
            if (_tile != null) Rebuild();
        }

        private void OnSelectionChanged(SelectionChangedEvent e)
        {
            _tile = e.Tile;
            _root.SetActive(_tile != null);
            if (_tile != null) Rebuild();
        }

        private void Rebuild()
        {
            var owner = _players.Get(_tile.OwnerId);
            var data = _tile.Data;
            _title.text = string.IsNullOrEmpty(data.Name) ? _localization.Get("province.unnamed") : data.Name;

            var details = new StringBuilder();
            details.AppendLine(_localization.Format("province.owner", UiFormat.Colored(owner.Name, owner.Color)));
            if (_tile.IsLand)
            {
                details.AppendLine(_localization.Format("province.landscape", _localization.Get("landscape." + Key(data.Landscape))));
                details.AppendLine(_localization.Format("province.settlement", _localization.Get("settlement." + Key(data.Settlement))));
                details.AppendLine(_localization.Format("province.production", _localization.Get("production." + Key(data.Production))));
                if (!_tile.IsNeutral)
                    details.AppendLine(_localization.Format("province.income", _format.Cost(_economy.CalculateTileIncome(_tile))));
                if (_tile.Upgrades.Count > 0)
                {
                    details.Append(_localization.Get("province.buildings")).Append(' ');
                    for (var i = 0; i < _tile.Upgrades.Count; i++)
                    {
                        if (i > 0) details.Append(", ");
                        details.Append(_localization.Get(_tile.Upgrades[i].NameKey));
                    }
                    details.AppendLine();
                }
            }
            else details.AppendLine(_localization.Get("province.sea"));
            _details.text = details.ToString();

            var human = _turnState.Human;
            var isOwn = human != null && _tile.OwnerId == human.Id && _tile.HasSettlement;
            _buildSection.SetActive(isOwn);
            _recruitSection.SetActive(isOwn);
            _buildButtons.Clear();
            _recruitButtons.Clear();
            if (isOwn) FillActions(human);
            FillArmies();
        }

        private void FillActions(PlayerData human)
        {
            var canAct = _turnState.CanAct;
            foreach (var upgrade in _construction.GetCandidates(_tile))
            {
                var blocker = _construction.GetBuildBlocker(_tile, upgrade, human);
                var subtitle = _format.Cost(upgrade.Cost);
                if (blocker != null) subtitle += "  " + UiFormat.Colored(_localization.Get(blocker), Color.gray);
                var id = upgrade.Id;
                _buildButtons.Add(_localization.Get(upgrade.NameKey), subtitle, canAct && blocker == null,
                    () => _processor.Enqueue(new BuildUpgradeCommand(human.Id, _tile.Id, id)));
            }

            foreach (var unit in _units.Units)
            {
                if (!unit.Recruitable) continue;
                var blocker = _construction.GetRecruitBlocker(_tile, unit, human);
                var subtitle = _format.Cost(_construction.GetRecruitCost(_tile, unit));
                if (blocker != null) subtitle += "  " + UiFormat.Colored(_localization.Get(blocker), Color.gray);
                var id = unit.Id;
                _recruitButtons.Add(_localization.Get(unit.NameKey), subtitle, canAct && blocker == null,
                    () => _processor.Enqueue(new RecruitCohortCommand(human.Id, _tile.Id, id)));
            }
        }

        private void FillArmies()
        {
            _armyButtons.Clear();
            foreach (var army in _tile.Armies)
            {
                var owner = _players.Get(army.OwnerId);
                var title = _localization.Format("army.title", UiFormat.Colored(owner.Name, owner.Color), army.Cohorts.Count);
                var target = army;
                _armyButtons.Add(title, _localization.Format("army.morale", UiFormat.Percent(army.Morale)), true,
                    () => _selection.Select(_tile, target));
            }
        }

        private static string Key(System.Enum value) => value.ToString().ToLowerInvariant();
    }
}
