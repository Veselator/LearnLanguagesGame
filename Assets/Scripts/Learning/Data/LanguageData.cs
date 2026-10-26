using System;
using System.Collections.Generic;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Learning.Data
{
    /// <summary>
    /// The whole vocabulary of one trained language.
    /// </summary>
    [CreateAssetMenu(menuName = "LearnLanguages/Language Data", fileName = "LanguageData")]
    public sealed class LanguageData : ScriptableObject
    {
        [SerializeField] private Language _targetLanguage;
        [SerializeField] private WordEntry[] _entries = Array.Empty<WordEntry>();

        public Language TargetLanguage => _targetLanguage;
        public IReadOnlyList<WordEntry> Entries => _entries;

        public void SetContent(Language targetLanguage, WordEntry[] entries)
        {
            _targetLanguage = targetLanguage;
            _entries = entries;
        }
    }
}
