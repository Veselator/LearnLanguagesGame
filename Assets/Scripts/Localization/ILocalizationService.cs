using System;
using System.Collections.Generic;
using LearnLanguages.Settings;

namespace LearnLanguages.Localization
{
    public interface ILocalizationService
    {
        Language CurrentLanguage { get; }
        event Action LanguageChanged;
        void SetLanguage(Language language);
        string Get(string key);
        string Format(string key, params object[] args);
        bool Has(string key);
    }

    public sealed class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, LocalizationEntry> _entries = new Dictionary<string, LocalizationEntry>();

        public LocalizationService(LocalizationTable table)
        {
            foreach (var entry in table.Entries)
                if (!string.IsNullOrEmpty(entry.Key)) _entries[entry.Key] = entry;
        }

        public Language CurrentLanguage { get; private set; } = Language.Russian;
        public event Action LanguageChanged;

        public void SetLanguage(Language language)
        {
            if (CurrentLanguage == language) return;
            CurrentLanguage = language;
            LanguageChanged?.Invoke();
        }

        public bool Has(string key) => key != null && _entries.ContainsKey(key);

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!_entries.TryGetValue(key, out var entry)) return key;
            var value = entry.Get(CurrentLanguage);
            if (string.IsNullOrEmpty(value)) value = entry.Get(Language.English);
            return string.IsNullOrEmpty(value) ? key : value;
        }

        public string Format(string key, params object[] args) => string.Format(Get(key), args);
    }
}
