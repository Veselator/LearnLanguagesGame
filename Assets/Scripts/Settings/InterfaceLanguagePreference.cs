using System;
using LearnLanguages.Localization;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Settings
{
    /// <summary>
    /// Remembers the interface language between launches: restores it on start and stores every change.
    /// </summary>
    public sealed class InterfaceLanguagePreference : IInitializable, IDisposable
    {
        private const string PrefsKey = "settings.interface_language";

        private readonly ILocalizationService _localization;

        public InterfaceLanguagePreference(ILocalizationService localization)
        {
            _localization = localization;
        }

        public void Initialize()
        {
            if (Enum.TryParse(PlayerPrefs.GetString(PrefsKey, string.Empty), out Language saved) &&
                IsInterfaceLanguage(saved))
                _localization.SetLanguage(saved);
            _localization.LanguageChanged += Save;
        }

        public void Dispose() => _localization.LanguageChanged -= Save;

        private static bool IsInterfaceLanguage(Language language)
        {
            foreach (var candidate in LanguageRules.InterfaceLanguages)
                if (candidate == language) return true;
            return false;
        }

        private void Save()
        {
            PlayerPrefs.SetString(PrefsKey, _localization.CurrentLanguage.ToString());
            PlayerPrefs.Save();
        }
    }
}
