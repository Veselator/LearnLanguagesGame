using System;
using System.Collections.Generic;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Localization
{
    [Serializable]
    public sealed class LocalizationEntry
    {
        [SerializeField] private string _key;
        [Tooltip("Values indexed by the Language enum: English, French, Russian, Ukrainian.")]
        [SerializeField] private string[] _values;

        public LocalizationEntry(string key, string[] values)
        {
            _key = key;
            _values = values;
        }

        public string Key => _key;

        public string Get(Language language)
        {
            var index = (int)language;
            return _values != null && index < _values.Length ? _values[index] : null;
        }
    }

    [CreateAssetMenu(menuName = "LearnLanguages/Localization Table", fileName = "LocalizationTable")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [SerializeField] private LocalizationEntry[] _entries = Array.Empty<LocalizationEntry>();

        public IReadOnlyList<LocalizationEntry> Entries => _entries;

        public void SetEntries(LocalizationEntry[] entries) => _entries = entries;
    }
}
