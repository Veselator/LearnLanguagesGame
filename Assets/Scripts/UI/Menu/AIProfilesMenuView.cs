using System;
using System.Collections.Generic;
using LearnLanguages.Gameplay.AI;
using LearnLanguages.Localization;
using LearnLanguages.Settings;
using LearnLanguages.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Menu
{
    /// <summary>
    /// Window of the main menu to view, edit and add AI models. Every change is saved immediately by the store.
    /// </summary>
    public sealed class AIProfilesMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _listContainer;
        [SerializeField] private Button _add;
        [SerializeField] private Button _duplicate;
        [SerializeField] private Button _back;
        [SerializeField] private TMP_InputField _name;
        [SerializeField] private TMP_Text _difficulty;
        [SerializeField] private AIProfileFieldView _searchDepth;
        [SerializeField] private AIProfileFieldView _maxBranching;
        [SerializeField] private AIProfileFieldView _maxActions;
        [SerializeField] private Transform _prioritiesContainer;
        [SerializeField] private Transform _probabilitiesContainer;
        [SerializeField] private Button _addProbability;
        [SerializeField] private Button _removeProbability;

        private readonly List<ActionButtonView> _listItems = new List<ActionButtonView>();
        private readonly List<AIProfileFieldView> _priorityFields = new List<AIProfileFieldView>();
        private readonly List<AIProfileFieldView> _probabilityFields = new List<AIProfileFieldView>();

        private IAIProfilesStore _store;
        private ILocalizationService _localization;
        private ActionButtonView.Pool _buttonPool;
        private AIProfileFieldView.Pool _fieldPool;
        private AIProfile _selected;

        public event Action Closed;

        [Inject]
        public void Construct(IAIProfilesStore store, ILocalizationService localization, ActionButtonView.Pool buttonPool,
            AIProfileFieldView.Pool fieldPool)
        {
            _store = store;
            _localization = localization;
            _buttonPool = buttonPool;
            _fieldPool = fieldPool;
            _localization.LanguageChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (_localization != null) _localization.LanguageChanged -= Refresh;
        }

        private void Start()
        {
            _add.onClick.AddListener(() => Select(_store.Add(_localization.Get("ai.edit.new_name"))));
            _duplicate.onClick.AddListener(() =>
                Select(_store.Duplicate(_selected, _localization.Format("ai.edit.copy_name", _selected.DisplayName))));
            _back.onClick.AddListener(() =>
            {
                Show(false);
                Closed?.Invoke();
            });
            _name.onEndEdit.AddListener(OnNameEdited);
            _addProbability.onClick.AddListener(() => ResizeProbabilities(1));
            _removeProbability.onClick.AddListener(() => ResizeProbabilities(-1));
        }

        public void Show(bool show)
        {
            _panel.SetActive(show);
            if (!show) return;
            if (_selected == null && _store.Profiles.Count > 0) _selected = _store.Profiles[0];
            Refresh();
        }

        private void Select(AIProfile profile)
        {
            _selected = profile;
            Refresh();
        }

        private void Edit(Action<AIProfile> change)
        {
            _store.Edit(_selected, change);
            Refresh();
        }

        private void Refresh()
        {
            if (_selected == null || !_panel.activeSelf) return;
            RefreshList();
            RefreshGeneral();
            RefreshPriorities();
            RefreshProbabilities();
        }

        private void RefreshList()
        {
            var profiles = _store.Profiles;
            while (_listItems.Count < profiles.Count)
            {
                var item = _buttonPool.Spawn();
                item.transform.SetParent(_listContainer, false);
                _listItems.Add(item);
            }
            for (var i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                var title = profile == _selected ? "> " + profile.DisplayName : profile.DisplayName;
                _listItems[i].Bind(title, DifficultyName(profile.Difficulty), profile != _selected, () => Select(profile));
            }
        }

        private void RefreshGeneral()
        {
            _name.SetTextWithoutNotify(_selected.DisplayName);

            _difficulty.text = _localization.Format("ai.edit.difficulty_value", DifficultyName(_selected.Difficulty),
                Mathf.RoundToInt(_selected.DifficultyRating * 100f));

            _searchDepth.Bind(_localization.Get("ai.edit.depth"), _selected.SearchDepth,
                v => EditNumber(v, p => p.SetSearchDepth(Mathf.RoundToInt(v))));
            _maxBranching.Bind(_localization.Get("ai.edit.branching"), _selected.MaxBranching,
                v => EditNumber(v, p => p.SetMaxBranching(Mathf.RoundToInt(v))));
            _maxActions.Bind(_localization.Get("ai.edit.actions"), _selected.MaxActionsPerTurn,
                v => EditNumber(v, p => p.SetMaxActionsPerTurn(Mathf.RoundToInt(v))));
        }

        private void RefreshPriorities()
        {
            EnsureFields(_priorityFields, AICriteria.Count, _prioritiesContainer);
            var shares = _selected.NormalizedPriorities;
            for (var i = 0; i < AICriteria.Count; i++)
            {
                var criterion = (AICriterion)i;
                _priorityFields[i].Bind(_localization.Get("ai.criterion." + criterion.ToString().ToLowerInvariant()),
                    _selected.Priorities[i], shares[i], v => EditNumber(v, p => p.SetPriority(criterion, v)));
            }
        }

        private void RefreshProbabilities()
        {
            var values = _selected.DecisionProbabilities;
            EnsureFields(_probabilityFields, values.Count, _probabilitiesContainer);
            var shares = WeightVector.Normalize(values);
            for (var i = 0; i < values.Count; i++)
            {
                var index = i;
                _probabilityFields[i].Bind(_localization.Format("ai.edit.option", i + 1), values[i], shares[i],
                    v => EditNumber(v, p => SetProbability(p, index, Mathf.Max(0f, v))));
            }
            _removeProbability.interactable = values.Count > 1;
        }

        private void EnsureFields(List<AIProfileFieldView> fields, int count, Transform container)
        {
            while (fields.Count < count)
            {
                var field = _fieldPool.Spawn();
                field.transform.SetParent(container, false);
                fields.Add(field);
            }
            while (fields.Count > count)
            {
                _fieldPool.Despawn(fields[fields.Count - 1]);
                fields.RemoveAt(fields.Count - 1);
            }
        }

        /// <summary>Text that is not a number just restores the previous value.</summary>
        private void EditNumber(float value, Action<AIProfile> change)
        {
            if (float.IsNaN(value)) Refresh();
            else Edit(change);
        }

        private static void SetProbability(AIProfile profile, int index, float value)
        {
            var values = new List<float>(profile.DecisionProbabilities);
            values[index] = value;
            profile.SetDecisionProbabilities(values.ToArray());
        }

        private void ResizeProbabilities(int delta)
        {
            var values = new List<float>(_selected.DecisionProbabilities);
            if (delta > 0) values.Add(0f);
            else if (values.Count > 1) values.RemoveAt(values.Count - 1);
            Edit(p => p.SetDecisionProbabilities(values.ToArray()));
        }

        private void OnNameEdited(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) Refresh();
            else Edit(p => p.SetDisplayName(text.Trim()));
        }

        private string DifficultyName(GameDifficulty difficulty) =>
            _localization.Get("difficulty." + difficulty.ToString().ToLowerInvariant());
    }
}
