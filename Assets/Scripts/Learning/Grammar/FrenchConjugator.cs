using LearnLanguages.Learning.Data;
using LearnLanguages.Settings;

namespace LearnLanguages.Learning.Grammar
{
    /// <summary>
    /// Rules of the three French verb groups. Irregular verbs keep their forms in the vocabulary.
    /// Past = imparfait, Future = futur simple, Conditional = conditionnel présent.
    /// </summary>
    public sealed class FrenchConjugator : ConjugatorBase
    {
        private static readonly string[] Pronouns = { "je", "tu", "il", "nous", "vous", "ils" };
        private static readonly string[] FirstGroupPresent = { "e", "es", "e", "ons", "ez", "ent" };
        private static readonly string[] SecondGroupPresent = { "is", "is", "it", "issons", "issez", "issent" };
        private static readonly string[] ThirdGroupPresent = { "s", "s", "", "ons", "ez", "ent" };
        private static readonly string[] ImperfectEndings = { "ais", "ais", "ait", "ions", "iez", "aient" };
        private static readonly string[] FutureEndings = { "ai", "as", "a", "ons", "ez", "ont" };

        public override Language Language => Language.French;

        public override string GetPronoun(Person person) => Pronouns[(int)person];

        public override string ConjugateWithPronoun(WordEntry verb, Tense tense, Person person)
        {
            var form = Conjugate(verb, tense, person);
            if (person == Person.FirstSingular && StartsWithVowelSound(form)) return "j'" + form;
            return GetPronoun(person) + " " + form;
        }

        protected override string ConjugateByRules(WordEntry verb, Tense tense, Person person)
        {
            switch (tense)
            {
                case Tense.Present: return Present(verb.Word, person);
                case Tense.Past: return Imperfect(verb, person);
                case Tense.Future: return FutureStem(verb) + FutureEndings[(int)person];
                default: return FutureStem(verb) + ImperfectEndings[(int)person];
            }
        }

        private static string Present(string infinitive, Person person)
        {
            var index = (int)person;
            if (infinitive.EndsWith("er"))
            {
                var stem = infinitive.Substring(0, infinitive.Length - 2);
                var ending = FirstGroupPresent[index];
                return SoftenStem(stem, ending) + ending;
            }
            if (infinitive.EndsWith("ir"))
                return infinitive.Substring(0, infinitive.Length - 2) + SecondGroupPresent[index];
            if (infinitive.EndsWith("re"))
                return infinitive.Substring(0, infinitive.Length - 2) + ThirdGroupPresent[index];
            return infinitive;
        }

        private string Imperfect(WordEntry verb, Person person)
        {
            var nousForm = Conjugate(verb, Tense.Present, Person.FirstPlural);
            var stem = nousForm.EndsWith("ons") ? nousForm.Substring(0, nousForm.Length - 3) : nousForm;
            var ending = ImperfectEndings[(int)person];

            // mangeons -> mangeais / mangions, commençons -> commençais / commencions
            if (ending[0] == 'i')
            {
                if (stem.EndsWith("ge")) stem = stem.Substring(0, stem.Length - 1);
                else if (stem.EndsWith("ç")) stem = stem.Substring(0, stem.Length - 1) + "c";
            }
            return stem + ending;
        }

        private static string FutureStem(WordEntry verb)
        {
            var explicitFuture = verb.FindConjugation(Tense.Future)?.GetForm(Person.FirstSingular);
            if (!string.IsNullOrEmpty(explicitFuture) && explicitFuture.EndsWith("ai"))
                return explicitFuture.Substring(0, explicitFuture.Length - 2);

            var infinitive = verb.Word;
            return infinitive.EndsWith("re") ? infinitive.Substring(0, infinitive.Length - 1) : infinitive;
        }

        private static string SoftenStem(string stem, string ending)
        {
            if (ending.Length == 0 || (ending[0] != 'a' && ending[0] != 'o')) return stem;
            if (stem.EndsWith("g")) return stem + "e";
            if (stem.EndsWith("c")) return stem.Substring(0, stem.Length - 1) + "ç";
            return stem;
        }

        private static bool StartsWithVowelSound(string word) =>
            !string.IsNullOrEmpty(word) && "aeiouyhéèêâîôûà".IndexOf(char.ToLowerInvariant(word[0])) >= 0;
    }
}
