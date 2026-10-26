using System.Collections.Generic;

namespace LearnLanguages.Learning.Tasks.Generators
{
    /// <summary>
    /// Strategy producing one kind of exercise.
    /// </summary>
    public interface ILanguageTaskGenerator
    {
        LanguageTaskType Type { get; }
        bool CanGenerate(LanguageTaskContext context);
        BasicLanguageTaskData Generate(LanguageTaskContext context);
    }

    public abstract class ChoiceTaskGeneratorBase : ILanguageTaskGenerator
    {
        protected const int OptionsCount = 4;

        public abstract LanguageTaskType Type { get; }
        public abstract bool CanGenerate(LanguageTaskContext context);
        public abstract BasicLanguageTaskData Generate(LanguageTaskContext context);

        /// <summary>Shuffles the correct option among distinct distractors.</summary>
        protected static ChoiceTaskData BuildChoice(LanguageTaskContext context, LanguageTaskType type, string instructionKey,
            string prompt, string correct, IEnumerable<string> distractors)
        {
            var options = new List<string> { correct };
            foreach (var distractor in distractors)
            {
                if (options.Count >= OptionsCount) break;
                if (!string.IsNullOrEmpty(distractor) && !options.Contains(distractor)) options.Add(distractor);
            }
            context.Random.Shuffle(options);
            return new ChoiceTaskData(type, instructionKey, prompt, context.Settings.ChoiceTimeLimit, options.ToArray(),
                options.IndexOf(correct));
        }
    }
}
