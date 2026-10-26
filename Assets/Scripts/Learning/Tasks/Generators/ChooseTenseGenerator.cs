using System.Collections.Generic;
using LearnLanguages.Learning.Data;
using LearnLanguages.Localization;

namespace LearnLanguages.Learning.Tasks.Generators
{
    /// <summary>Shows a conjugated verb, the player picks its tense out of four.</summary>
    public sealed class ChooseTenseGenerator : ChoiceTaskGeneratorBase
    {
        private static readonly Tense[] AllTenses = { Tense.Present, Tense.Past, Tense.Future, Tense.Conditional };
        private readonly ILocalizationService _localization;

        public ChooseTenseGenerator(ILocalizationService localization)
        {
            _localization = localization;
        }

        public override LanguageTaskType Type => LanguageTaskType.ChooseTense;

        public override bool CanGenerate(LanguageTaskContext context) =>
            context.Verbs.Count > 0 && GetAvailableTenses(context).Count >= 2;

        public override BasicLanguageTaskData Generate(LanguageTaskContext context)
        {
            var available = GetAvailableTenses(context);
            var verb = context.Random.Pick(context.Verbs);
            var tense = context.Random.Pick(available);
            var person = (Person)context.Random.Range(0, ConjugationEntry.PersonsCount);
            var prompt = context.Conjugator.ConjugateWithPronoun(verb, tense, person);

            // All four tenses are always offered so the answer cannot be guessed by elimination.
            var distractors = new List<string>();
            foreach (var other in AllTenses)
                if (other != tense) distractors.Add(TenseName(other));

            return BuildChoice(context, Type, "task.choose_tense", prompt, TenseName(tense), distractors);
        }

        private string TenseName(Tense tense) => _localization.Get("tense." + tense.ToString().ToLowerInvariant());

        private static List<Tense> GetAvailableTenses(LanguageTaskContext context)
        {
            var result = new List<Tense>();
            foreach (var tense in AllTenses)
                if (context.Conjugator.IsTenseAvailable(tense, context.Level)) result.Add(tense);
            return result;
        }
    }
}
