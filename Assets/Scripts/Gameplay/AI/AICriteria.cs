using System.Collections.Generic;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// The fixed set of criteria every position is scored by. Each value lies in [-1; 1], so priorities are comparable.
    /// </summary>
    public enum AICriterion
    {
        Territory = 0,
        Settlements = 1,
        Income = 2,
        Treasury = 3,
        Infrastructure = 4,
        ArmyStrength = 5,
        ArmySize = 6,
        CapitalSafety = 7,
        EnemyCapitals = 8,
        Advance = 9
    }

    public static class AICriteria
    {
        public const int Count = 10;
    }

    public static class WeightVector
    {
        /// <summary>Scales the numbers so they sum up to 1: number / sum. Negative numbers count as 0.</summary>
        public static float[] Normalize(IReadOnlyList<float> values)
        {
            var result = new float[values.Count];
            var sum = 0f;
            foreach (var value in values)
                if (value > 0f) sum += value;

            for (var i = 0; i < result.Length; i++)
                result[i] = sum > 0f ? (values[i] > 0f ? values[i] / sum : 0f) : 1f / result.Length;
            return result;
        }
    }

    /// <summary>
    /// Turns the probability array of a profile into a choice among ranked options:
    /// index 0 is the chance of taking the best option, index 1 of the second best, and so on.
    /// </summary>
    public static class AIDecisionPicker
    {
        /// <summary>Probabilities of the first <paramref name="optionCount"/> ranks, rescaled to sum up to 1.</summary>
        public static float[] GetRankProbabilities(IReadOnlyList<float> probabilities, int optionCount)
        {
            var available = new float[optionCount];
            var sum = 0f;
            for (var i = 0; i < optionCount && i < probabilities.Count; i++)
            {
                available[i] = probabilities[i] > 0f ? probabilities[i] : 0f;
                sum += available[i];
            }

            if (sum <= 0f)
            {
                if (optionCount > 0) available[0] = 1f;
                return available;
            }
            for (var i = 0; i < optionCount; i++) available[i] /= sum;
            return available;
        }

        /// <param name="roll">Random number in [0; 1).</param>
        public static int Pick(float[] rankProbabilities, float roll)
        {
            var accumulated = 0f;
            var last = 0;
            for (var i = 0; i < rankProbabilities.Length; i++)
            {
                if (rankProbabilities[i] <= 0f) continue;
                last = i;
                accumulated += rankProbabilities[i];
                if (roll < accumulated) return i;
            }
            return last;
        }
    }
}
