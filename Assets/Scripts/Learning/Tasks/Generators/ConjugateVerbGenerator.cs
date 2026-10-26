using System.Collections.Generic;
using LearnLanguages.Learning.Data;
using LearnLanguages.Localization;

namespace LearnLanguages.Learning.Tasks.Generators
{
    /// <summary>Asks to type the form of a verb for a given person and tense.</summary>
    public sealed class ConjugateVerbGenerator : ILanguageTaskGenerator
    {
        private readonly ILocalizationService _localization;

        public ConjugateVerbGenerator(ILocalizationService localization)
        {
            _localization = localization;
        }

        public LanguageTaskType Type => LanguageTaskType.ConjugateVerb;

        public bool CanGenerate(LanguageTaskContext context) => context.Verbs.Count > 0;

        public BasicLanguageTaskData Generate(LanguageTaskContext context)
        {
            var tenses = new List<Tense>();
            foreach (Tense tense in System.Enum.GetValues(typeof(Tense)))
                if (context.Conjugator.IsTenseAvailable(tense, context.Level)) tenses.Add(tense);

            var verb = context.Random.Pick(context.Verbs);
            var chosenTense = context.Random.Pick(tenses);
            var person = (Person)context.Random.Range(0, ConjugationEntry.PersonsCount);

            var conjugator = context.Conjugator;
            var form = conjugator.Conjugate(verb, chosenTense, person);
            var withPronoun = conjugator.ConjugateWithPronoun(verb, chosenTense, person);
            var tenseName = _localization.Get("tense." + chosenTense.ToString().ToLowerInvariant());
            var prompt = $"{WordVariants.First(verb.Word)} ({WordVariants.Display(context.Translate(verb))})\n" +
                         $"{conjugator.GetPronoun(person)} — {tenseName}";

            return new TextInputTaskData(Type, "task.conjugate", prompt, context.Settings.InputTimeLimit,
                new[] { withPronoun, form });
        }
    }
}
