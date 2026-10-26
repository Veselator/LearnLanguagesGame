using System.Collections.Generic;
using LearnLanguages.Localization;
using LearnLanguages.Map.Data;
using LearnLanguages.Save;
using LearnLanguages.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Menu
{
    /// <summary>
    /// Main menu and the new game setup: map, trained language, interface language, level, difficulty, opponents.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Header("Main")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private Button _newGame;
        [SerializeField] private Button _continue;
        [SerializeField] private Button _quit;
        [SerializeField] private TMP_Dropdown _menuLanguage;

        [Header("New game")]
        [SerializeField] private GameObject _newGamePanel;
        [SerializeField] private TMP_Dropdown _map;
        [SerializeField] private RawImage _mapIcon;
        [SerializeField] private TMP_Dropdown _targetLanguage;
        [SerializeField] private TMP_Dropdown _interfaceLanguage;
        [SerializeField] private TMP_Dropdown _level;
        [SerializeField] private TMP_Dropdown _difficulty;
        [SerializeField] private TMP_Dropdown _aiPlayers;
        [SerializeField] private Button _start;
        [SerializeField] private Button _back;

        private readonly List<Language> _interfaceOptions = new List<Language>();
        private MapsConfig _maps;
        private ILocalizationService _localization;
        private IGameSession _session;
        private ISaveService _saves;
        private ISceneLoader _scenes;
        private bool _refreshing;

        [Inject]
        public void Construct(MapsConfig maps, ILocalizationService localization, IGameSession session,
            ISaveService saves, ISceneLoader scenes)
        {
            _maps = maps;
            _localization = localization;
            _session = session;
            _saves = saves;
            _scenes = scenes;
            _localization.LanguageChanged += RefreshTexts;
        }

        private void OnDestroy()
        {
            if (_localization != null) _localization.LanguageChanged -= RefreshTexts;
        }

        private void Start()
        {
            _newGame.onClick.AddListener(() => ShowNewGame(true));
            _back.onClick.AddListener(() => ShowNewGame(false));
            _continue.onClick.AddListener(Continue);
            _quit.onClick.AddListener(() => _scenes.Quit());
            _start.onClick.AddListener(StartGame);
            _menuLanguage.onValueChanged.AddListener(OnMenuLanguageChanged);
            _interfaceLanguage.onValueChanged.AddListener(_ => OnInterfaceLanguagePicked());
            _targetLanguage.onValueChanged.AddListener(_ => OnTargetLanguageChanged());
            _map.onValueChanged.AddListener(_ => RefreshMapIcon());

            _continue.interactable = _saves.HasSave;
            ShowNewGame(false);
            RefreshTexts();
        }

        private void ShowNewGame(bool show)
        {
            _mainPanel.SetActive(!show);
            _newGamePanel.SetActive(show);
            if (show) _interfaceLanguage.SetValueWithoutNotify(InterfaceIndexOf(_localization.CurrentLanguage));
        }

        private void RefreshTexts()
        {
            if (_refreshing) return;
            _refreshing = true;

            Fill(_menuLanguage, LanguageRules.InterfaceLanguages, l => _localization.Get("lang." + l));
            _menuLanguage.SetValueWithoutNotify(IndexOf(LanguageRules.InterfaceLanguages, _localization.CurrentLanguage));

            var mapIndex = _map.value;
            _map.ClearOptions();
            var mapNames = new List<string>();
            foreach (var map in _maps.Maps) mapNames.Add(map.Name);
            _map.AddOptions(mapNames);
            _map.SetValueWithoutNotify(Mathf.Clamp(mapIndex, 0, mapNames.Count - 1));

            var target = _targetLanguage.value;
            Fill(_targetLanguage, LanguageRules.TrainableLanguages, l => _localization.Get("lang." + l));
            _targetLanguage.SetValueWithoutNotify(target);

            var level = _level.value;
            Fill(_level, (LanguageLevel[])System.Enum.GetValues(typeof(LanguageLevel)), l => l.ToString());
            _level.SetValueWithoutNotify(level);

            var difficulty = _difficulty.options.Count > 0 ? _difficulty.value : (int)GameDifficulty.Medium;
            Fill(_difficulty, (GameDifficulty[])System.Enum.GetValues(typeof(GameDifficulty)),
                d => _localization.Get("difficulty." + d.ToString().ToLowerInvariant()));
            _difficulty.SetValueWithoutNotify(difficulty);

            var ai = _aiPlayers.options.Count > 0 ? _aiPlayers.value : 1;
            Fill(_aiPlayers, new[] { 1, 2, 3 }, n => n.ToString());
            _aiPlayers.SetValueWithoutNotify(ai);

            RebuildInterfaceOptions();
            RefreshMapIcon();
            _refreshing = false;
        }

        /// <summary>The interface language may never be the trained one.</summary>
        private void RebuildInterfaceOptions()
        {
            var target = LanguageRules.TrainableLanguages[_targetLanguage.value];
            _interfaceOptions.Clear();
            foreach (var language in LanguageRules.InterfaceLanguages)
                if (LanguageRules.IsValidPair(target, language)) _interfaceOptions.Add(language);

            Fill(_interfaceLanguage, _interfaceOptions, l => _localization.Get("lang." + l));
            _interfaceLanguage.SetValueWithoutNotify(Mathf.Max(0, InterfaceIndexOf(_localization.CurrentLanguage)));
        }

        private void OnTargetLanguageChanged()
        {
            RebuildInterfaceOptions();
            if (!_interfaceOptions.Contains(_localization.CurrentLanguage)) _localization.SetLanguage(_interfaceOptions[0]);
        }

        private int InterfaceIndexOf(Language language)
        {
            var index = _interfaceOptions.IndexOf(language);
            return index < 0 ? 0 : index;
        }

        private void OnMenuLanguageChanged(int index) => _localization.SetLanguage(LanguageRules.InterfaceLanguages[index]);

        private void OnInterfaceLanguagePicked() =>
            _localization.SetLanguage(_interfaceOptions[Mathf.Clamp(_interfaceLanguage.value, 0, _interfaceOptions.Count - 1)]);

        private void RefreshMapIcon()
        {
            if (_maps.Maps.Count == 0) return;
            var map = _maps.Maps[Mathf.Clamp(_map.value, 0, _maps.Maps.Count - 1)];
            _mapIcon.texture = map.Icon != null ? map.Icon : map.VisualLandscape;
        }

        private void StartGame()
        {
            var map = _maps.Maps[Mathf.Clamp(_map.value, 0, _maps.Maps.Count - 1)];
            var target = LanguageRules.TrainableLanguages[_targetLanguage.value];
            var ui = _interfaceOptions[Mathf.Clamp(_interfaceLanguage.value, 0, _interfaceOptions.Count - 1)];
            _session.StartNew(new GameSettings(map.Id, target, ui, (LanguageLevel)_level.value,
                (GameDifficulty)_difficulty.value, _aiPlayers.value + 1, System.Environment.TickCount));
            _scenes.LoadGame();
        }

        private void Continue()
        {
            var save = _saves.Load();
            if (save == null)
            {
                _continue.interactable = false;
                return;
            }
            _session.StartFromSave(save);
            _scenes.LoadGame();
        }

        private static void Fill<T>(TMP_Dropdown dropdown, IReadOnlyList<T> values, System.Func<T, string> label)
        {
            var options = new List<string>(values.Count);
            foreach (var value in values) options.Add(label(value));
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
        }

        private static int IndexOf<T>(IReadOnlyList<T> values, T value)
        {
            for (var i = 0; i < values.Count; i++)
                if (EqualityComparer<T>.Default.Equals(values[i], value)) return i;
            return 0;
        }
    }
}
