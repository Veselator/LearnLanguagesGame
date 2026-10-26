using System.Linq;

namespace LearnLanguages.Learning.Tasks.Generators
{
    /// <summary>Shows a word of the trained language, the player picks its translation out of four.</summary>
    public sealed class ChooseTranslationGenerator : ChoiceTaskGeneratorBase
    {
        public override LanguageTaskType Type => LanguageTaskType.ChooseTranslation;

        public override bool CanGenerate(LanguageTaskContext context) => context.Words.Count >= OptionsCount;

        public override BasicLanguageTaskData Generate(LanguageTaskContext context)
        {
            var word = context.Random.Pick(context.Words);
            var sameType = context.PickWords(OptionsCount * 2, w => w.Type == word.Type, word);
            var fallback = context.PickWords(OptionsCount * 2, w => true, word);
            var distractors = sameType.Concat(fallback).Select(w => WordVariants.Display(context.Translate(w)));

            return BuildChoice(context, Type, "task.choose_translation", WordVariants.Display(word.Word),
                WordVariants.Display(context.Translate(word)), distractors);
        }
    }
}
