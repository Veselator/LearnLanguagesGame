using System;
using System.Collections.Generic;
using LearnLanguages.Settings;

namespace LearnLanguages.Learning.Grammar
{
    public interface IConjugatorProvider
    {
        IConjugator Get(Language language);
    }

    public sealed class ConjugatorProvider : IConjugatorProvider
    {
        private readonly Dictionary<Language, IConjugator> _conjugators = new Dictionary<Language, IConjugator>();

        public ConjugatorProvider(IEnumerable<IConjugator> conjugators)
        {
            foreach (var conjugator in conjugators) _conjugators[conjugator.Language] = conjugator;
        }

        public IConjugator Get(Language language)
        {
            if (_conjugators.TryGetValue(language, out var conjugator)) return conjugator;
            throw new ArgumentException($"No grammar rules registered for {language}.");
        }
    }
}
