using LearnLanguages.Learning.Data;
using LearnLanguages.Settings;

namespace LearnLanguages.Learning.Grammar
{
    /// <summary>
    /// Built-in grammar rules of a trained language (strategy per language).
    /// </summary>
    public interface IConjugator
    {
        Language Language { get; }

        /// <summary>Verb form without a pronoun. Irregular forms from the vocabulary take priority over rules.</summary>
        string Conjugate(WordEntry verb, Tense tense, Person person);

        /// <summary>Verb form together with its subject pronoun, e.g. "j'aime" or "he speaks".</summary>
        string ConjugateWithPronoun(WordEntry verb, Tense tense, Person person);

        string GetPronoun(Person person);

        /// <summary>Whether this tense is taught at the given level.</summary>
        bool IsTenseAvailable(Tense tense, LanguageLevel level);
    }

    public abstract class ConjugatorBase : IConjugator
    {
        public abstract Language Language { get; }

        public string Conjugate(WordEntry verb, Tense tense, Person person)
        {
            var explicitForm = verb.FindConjugation(tense)?.GetForm(person);
            return string.IsNullOrEmpty(explicitForm) ? ConjugateByRules(verb, tense, person) : explicitForm;
        }

        public virtual string ConjugateWithPronoun(WordEntry verb, Tense tense, Person person) =>
            GetPronoun(person) + " " + Conjugate(verb, tense, person);

        public abstract string GetPronoun(Person person);

        public virtual bool IsTenseAvailable(Tense tense, LanguageLevel level)
        {
            switch (tense)
            {
                case Tense.Present: return true;
                case Tense.Past:
                case Tense.Future: return level >= LanguageLevel.A2;
                default: return level >= LanguageLevel.B1;
            }
        }

        protected abstract string ConjugateByRules(WordEntry verb, Tense tense, Person person);
    }
}
