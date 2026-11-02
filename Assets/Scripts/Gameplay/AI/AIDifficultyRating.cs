using System.Collections.Generic;
using LearnLanguages.Settings;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// How strong a model plays, derived from what it does: how far it looks ahead and how reliably it takes the best options.
    /// </summary>
    public static class AIDifficultyRating
    {
        private const float MediumFrom = 0.45f;
        private const float HardFrom = 0.68f;

        /// <returns>From 0 (no foresight, mostly weak options) to almost 1.</returns>
        public static float Calculate(int searchDepth, IReadOnlyList<float> decisionProbabilities)
        {
            var foresight = 1f - 1f / (1f + searchDepth);
            return foresight * Accuracy(decisionProbabilities);
        }

        public static GameDifficulty ToDifficulty(float rating)
        {
            if (rating >= HardFrom) return GameDifficulty.Hard;
            return rating >= MediumFrom ? GameDifficulty.Medium : GameDifficulty.Easy;
        }

        /// <summary>The expected quality of a choice: the best option counts fully, the n-th as 1/n of it.</summary>
        private static float Accuracy(IReadOnlyList<float> probabilities)
        {
            var ranks = AIDecisionPicker.GetRankProbabilities(probabilities, probabilities.Count);
            var accuracy = 0f;
            for (var i = 0; i < ranks.Length; i++) accuracy += ranks[i] / (i + 1);
            return accuracy;
        }
    }
}
