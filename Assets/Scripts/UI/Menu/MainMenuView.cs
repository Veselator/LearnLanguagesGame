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
    /// Main menu and the new game setup: map, trained language, level, difficulty, opponents.
    /// The interface language lives in <see cref="SettingsMenuView"/>.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Header("Main")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private Button _newGame;
        [SerializeField] private Button _continue;
        [SerializeField] private Button _settings;
        [SerializeField] private Button _aiProfiles;
        [SerializeField] private Button _quit;
        [SerializeField] private SettingsMenuView _settingsMenu;
        [SerializeField] private AIProfilesMenuView _aiProfilesMenu;

        [Header("New game")]
        [SerializeField] private GameObject _newGamePanel;
        [SerializeField] private TMP_Dropdown _map;
        [SerializeField] private RawImage _mapIcon;
        [SerializeField] private TMP_Dropdown _targetLanguage;
        [SerializeField] private TMP_Dropdown _level;
        [SerializeField] private TMP_Dropdown _difficulty;
        [SerializeField] private TMP_Dropdown _aiPlayers;
        [SerializeField] private Toggle _simplifiedBattles;
        [SerializeField] private Button _start;
        [SerializeField] private Button _back;

        private readonly List<Language> _targetOptions = new List<Language>();
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
            _settings.onClick.AddListener(ShowSettings);
            _settingsMenu.Closed += () => _mainPanel.SetActive(true);
            _aiProfiles.onClick.AddListener(ShowAiProfiles);
            _aiProfilesMenu.Closed += () => _mainPanel.SetActive(true);
            _quit.onClick.AddListener(() => _scenes.Quit());
            _start.onClick.AddListener(StartGame);
            _map.onValueChanged.AddListener(_ => RefreshMapIcon());

            _continue.interactable = _saves.HasSave;
            _settingsMenu.Show(false);
            _aiProfilesMenu.Show(false);
            ShowNewGame(false);
            RefreshTexts();
        }

        private void ShowNewGame(bool show)
        {
            _mainPanel.SetActive(!show);
            _newGamePanel.SetActive(show);
        }

        private void ShowAiProfiles()
        {
            _mainPanel.SetActive(false);
            _aiProfilesMenu.Show(true);
        }

        private void ShowSettings()
        {
            _mainPanel.SetActive(false);
            _settingsMenu.Show(true);
        }

        private void RefreshTexts()
        {
            if (_refreshing) return;
            _refreshing = true;

            var mapIndex = _map.value;
            _map.ClearOptions();
            var mapNames = new List<string>();
            foreach (var map in _maps.Maps) mapNames.Add(map.Name);
            _map.AddOptions(mapNames);
            _map.SetValueWithoutNotify(Mathf.Clamp(mapIndex, 0, mapNames.Count - 1));

            RebuildTargetOptions();

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

            RefreshMapIcon();
            _refreshing = false;
        }

        /// <summary>The trained language may never be the interface one: only valid pairs are offered.</summary>
        private void RebuildTargetOptions()
        {
            var previous = _targetOptions.Count > 0
                ? _targetOptions[Mathf.Clamp(_targetLanguage.value, 0, _targetOptions.Count - 1)]
                : LanguageRules.TrainableLanguages[0];
            _targetOptions.Clear();
            foreach (var language in LanguageRules.TrainableLanguages)
                if (LanguageRules.IsValidPair(language, _localization.CurrentLanguage)) _targetOptions.Add(language);

            Fill(_targetLanguage, _targetOptions, l => _localization.Get("lang." + l));
            _targetLanguage.SetValueWithoutNotify(Mathf.Max(0, _targetOptions.IndexOf(previous)));
        }

        private void RefreshMapIcon()
        {
            if (_maps.Maps.Count == 0) return;
            var map = _maps.Maps[Mathf.Clamp(_map.value, 0, _maps.Maps.Count - 1)];
            _mapIcon.texture = map.Icon != null ? map.Icon : map.VisualLandscape;
        }

        private void StartGame()
        {
            var map = _maps.Maps[Mathf.Clamp(_map.value, 0, _maps.Maps.Count - 1)];
            var target = _targetOptions[Mathf.Clamp(_targetLanguage.value, 0, _targetOptions.Count - 1)];
            _session.StartNew(new GameSettings(map.Id, target, _localization.CurrentLanguage, (LanguageLevel)_level.value,
                (GameDifficulty)_difficulty.value, _aiPlayers.value + 1, System.Environment.TickCount, _simplifiedBattles.isOn));
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
    }
}
