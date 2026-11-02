using System;
using System.Collections.Generic;
using LearnLanguages.Localization;
using LearnLanguages.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Menu
{
    /// <summary>
    /// Game settings opened from the main menu: the interface language and debug mode.
    /// </summary>
    public sealed class SettingsMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Dropdown _interfaceLanguage;
        [SerializeField] private Toggle _debugMode;
        [SerializeField] private Button _back;

        private ILocalizationService _localization;
        private IGameOptions _options;

        public event Action Closed;

        [Inject]
        public void Construct(ILocalizationService localization, IGameOptions options)
        {
            _localization = localization;
            _options = options;
            _localization.LanguageChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (_localization != null) _localization.LanguageChanged -= Refresh;
        }

        private void Start()
        {
            _interfaceLanguage.onValueChanged.AddListener(OnLanguagePicked);
            _debugMode.onValueChanged.AddListener(enabled => _options.DebugMode = enabled);
            _back.onClick.AddListener(() =>
            {
                Show(false);
                Closed?.Invoke();
            });
        }

        public void Show(bool show)
        {
            _panel.SetActive(show);
            if (show) Refresh();
        }

        private void Refresh()
        {
            var languages = LanguageRules.InterfaceLanguages;
            var options = new List<string>(languages.Count);
            var current = 0;
            for (var i = 0; i < languages.Count; i++)
            {
                options.Add(_localization.Get("lang." + languages[i]));
                if (languages[i] == _localization.CurrentLanguage) current = i;
            }
            _interfaceLanguage.ClearOptions();
            _interfaceLanguage.AddOptions(options);
            _interfaceLanguage.SetValueWithoutNotify(current);
            _debugMode.SetIsOnWithoutNotify(_options.DebugMode);
        }

        private void OnLanguagePicked(int index) => _localization.SetLanguage(LanguageRules.InterfaceLanguages[index]);
    }
}
