using System.Collections.Generic;
using System.Linq;
using LearnLanguages.Learning.Data;

namespace LearnLanguages.Learning.Tasks.Generators
{
    /// <summary>Three words share a topic, the fourth does not; the player finds the odd one.</summary>
    public sealed class OddWordOutGenerator : ChoiceTaskGeneratorBase
    {
        private const int TopicWords = OptionsCount - 1;

        public override LanguageTaskType Type => LanguageTaskType.OddWordOut;

        public override bool CanGenerate(LanguageTaskContext context) => GetUsableTags(context).Count > 0;

        public override BasicLanguageTaskData Generate(LanguageTaskContext context)
        {
            var tag = context.Random.Pick(GetUsableTags(context));
            var topic = context.PickWords(TopicWords, w => w.HasTag(tag));
            var oddCandidates = context.PickWords(1, w => !w.HasTag(tag) && !SharesTag(w, topic));
            if (oddCandidates.Count == 0) oddCandidates = context.PickWords(1, w => !w.HasTag(tag));
            var odd = oddCandidates[0];

            return BuildChoice(context, Type, "task.odd_word_out", string.Empty, WordVariants.First(odd.Word),
                topic.Select(w => WordVariants.First(w.Word)));
        }

        private static List<WordTag> GetUsableTags(LanguageTaskContext context)
        {
            var counts = new Dictionary<WordTag, int>();
            foreach (var word in context.Words)
            foreach (var tag in word.Tags)
                counts[tag] = counts.TryGetValue(tag, out var c) ? c + 1 : 1;

            return counts.Where(pair => pair.Value >= TopicWords && pair.Value < context.Words.Count - 1)
                .Select(pair => pair.Key).ToList();
        }

        private static bool SharesTag(WordEntry word, List<WordEntry> topic) =>
            topic.Any(t => t.Tags.Any(word.HasTag));
    }
}
