namespace LearnLanguages.Learning.Tasks.Generators
{
    /// <summary>Shows a word in the interface language, the player types it in the trained language.</summary>
    public sealed class TypeTranslationGenerator : ILanguageTaskGenerator
    {
        public LanguageTaskType Type => LanguageTaskType.TypeTranslation;

        public bool CanGenerate(LanguageTaskContext context) => context.Words.Count > 0;

        public BasicLanguageTaskData Generate(LanguageTaskContext context)
        {
            var word = context.Random.Pick(context.Words);
            return new TextInputTaskData(Type, "task.type_translation", WordVariants.Display(context.Translate(word)),
                context.Settings.InputTimeLimit, WordVariants.Split(word.Word));
        }
    }
}
