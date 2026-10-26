using System;
using System.Collections.Generic;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Learning.Data
{
    /// <summary>
    /// Translation of a word into a concrete language.
    /// </summary>
    [Serializable]
    public struct TranslationEntry
    {
        [SerializeField] private Language _language;
        [SerializeField] private string _translation;

        public TranslationEntry(Language language, string translation)
        {
            _language = language;
            _translation = translation;
        }

        public Language Language => _language;
        public string Translation => _translation;
    }

    /// <summary>
    /// Explicit (irregular) forms of a verb for one tense. Regular forms are produced by the language rules.
    /// </summary>
    [Serializable]
    public sealed class ConjugationEntry
    {
        public const int PersonsCount = 6;

        [SerializeField] private Tense _tense;
        [Tooltip("Forms without pronouns, indexed by Person.")]
        [SerializeField] private string[] _forms = new string[PersonsCount];

        public ConjugationEntry(Tense tense, string[] forms)
        {
            _tense = tense;
            _forms = forms;
        }

        public Tense Tense => _tense;

        public string GetForm(Person person)
        {
            var index = (int)person;
            return _forms != null && index < _forms.Length ? _forms[index] : null;
        }
    }

    [Serializable]
    public sealed class WordEntry
    {
        [SerializeField] private string _word;
        [SerializeField] private WordType _type;
        [SerializeField] private ConjugationEntry[] _conjugation;
        [SerializeField] private LanguageLevel _level;
        [SerializeField] private WordTag[] _tags = Array.Empty<WordTag>();
        [SerializeField] private TranslationEntry[] _translations = Array.Empty<TranslationEntry>();

        public WordEntry(string word, WordType type, LanguageLevel level, WordTag[] tags,
            TranslationEntry[] translations, ConjugationEntry[] conjugation)
        {
            _word = word;
            _type = type;
            _level = level;
            _tags = tags ?? Array.Empty<WordTag>();
            _translations = translations ?? Array.Empty<TranslationEntry>();
            _conjugation = conjugation;
        }

        public string Word => _word;
        public WordType Type => _type;
        public LanguageLevel Level => _level;
        public IReadOnlyList<WordTag> Tags => _tags;
        public IReadOnlyList<TranslationEntry> Translations => _translations;
        public IReadOnlyList<ConjugationEntry> Conjugation => _conjugation;

        public bool HasTag(WordTag tag) => Array.IndexOf(_tags, tag) >= 0;

        public bool TryGetTranslation(Language language, out string translation)
        {
            foreach (var entry in _translations)
            {
                if (entry.Language != language || string.IsNullOrEmpty(entry.Translation)) continue;
                translation = entry.Translation;
                return true;
            }
            translation = null;
            return false;
        }

        public ConjugationEntry FindConjugation(Tense tense)
        {
            if (_conjugation == null) return null;
            foreach (var entry in _conjugation)
                if (entry != null && entry.Tense == tense) return entry;
            return null;
        }
    }
}
