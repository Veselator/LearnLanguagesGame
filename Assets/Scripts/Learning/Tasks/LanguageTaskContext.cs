using System.Collections.Generic;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Learning.Data;
using LearnLanguages.Learning.Grammar;
using LearnLanguages.Settings;

namespace LearnLanguages.Learning.Tasks
{
    /// <summary>
    /// Vocabulary prepared for one combination of trained language, interface language and level.
    /// </summary>
    public sealed class LanguageTaskContext
    {
        private readonly List<WordEntry> _words = new List<WordEntry>();
        private readonly List<WordEntry> _verbs = new List<WordEntry>();

        public LanguageTaskContext(LanguageData data, IConjugator conjugator, Language interfaceLanguage,
            LanguageLevel level, LanguageTasksSettings settings, IRandomService random)
        {
            Conjugator = conjugator;
            InterfaceLanguage = interfaceLanguage;
            Level = level;
            Settings = settings;
            Random = random;

            foreach (var word in data.Entries)
            {
                if (word.Level > level || !word.TryGetTranslation(interfaceLanguage, out _)) continue;
                _words.Add(word);
                if (word.Type == WordType.Verb) _verbs.Add(word);
            }
        }

        public IConjugator Conjugator { get; }
        public Language InterfaceLanguage { get; }
        public LanguageLevel Level { get; }
        public LanguageTasksSettings Settings { get; }
        public IRandomService Random { get; }
        public IReadOnlyList<WordEntry> Words => _words;
        public IReadOnlyList<WordEntry> Verbs => _verbs;

        public string Translate(WordEntry word) =>
            word.TryGetTranslation(InterfaceLanguage, out var translation) ? translation : word.Word;

        /// <summary>Picks up to <paramref name="count"/> distinct words matching the filter, excluding one word.</summary>
        public List<WordEntry> PickWords(int count, System.Func<WordEntry, bool> filter, WordEntry exclude = null)
        {
            var candidates = new List<WordEntry>();
            foreach (var word in _words)
                if (word != exclude && filter(word)) candidates.Add(word);
            Random.Shuffle(candidates);
            if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
            return candidates;
        }
    }
}
