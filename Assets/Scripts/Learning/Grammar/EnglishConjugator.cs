using LearnLanguages.Learning.Data;
using LearnLanguages.Settings;

namespace LearnLanguages.Learning.Grammar
{
    public sealed class EnglishConjugator : ConjugatorBase
    {
        private static readonly string[] Pronouns = { "I", "you", "he", "we", "you", "they" };

        public override Language Language => Language.English;

        public override string GetPronoun(Person person) => Pronouns[(int)person];

        protected override string ConjugateByRules(WordEntry verb, Tense tense, Person person)
        {
            var baseForm = verb.Word;
            switch (tense)
            {
                case Tense.Present:
                    return person == Person.ThirdSingular ? ThirdPersonSingular(baseForm) : baseForm;
                case Tense.Past:
                    return RegularPast(baseForm);
                case Tense.Future:
                    return "will " + baseForm;
                default:
                    return "would " + baseForm;
            }
        }

        private static string ThirdPersonSingular(string verb)
        {
            if (verb.EndsWith("s") || verb.EndsWith("x") || verb.EndsWith("z") || verb.EndsWith("ch") ||
                verb.EndsWith("sh") || verb.EndsWith("o"))
                return verb + "es";
            if (EndsWithConsonantY(verb)) return verb.Substring(0, verb.Length - 1) + "ies";
            return verb + "s";
        }

        private static string RegularPast(string verb)
        {
            if (verb.EndsWith("e")) return verb + "d";
            if (EndsWithConsonantY(verb)) return verb.Substring(0, verb.Length - 1) + "ied";
            return verb + "ed";
        }

        private static bool EndsWithConsonantY(string verb) =>
            verb.Length > 1 && verb.EndsWith("y") && "aeiou".IndexOf(verb[verb.Length - 2]) < 0;
    }
}
