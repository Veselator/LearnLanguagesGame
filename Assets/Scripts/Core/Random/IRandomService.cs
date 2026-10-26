using System.Collections.Generic;

namespace LearnLanguages.Core.Randomness
{
    public interface IRandomService
    {
        int Range(int minInclusive, int maxExclusive);
        float Range(float minInclusive, float maxInclusive);
        float Value { get; }
        bool Chance(float probability);
        T Pick<T>(IReadOnlyList<T> items);
        void Shuffle<T>(IList<T> items);
    }

    public sealed class RandomService : IRandomService
    {
        private readonly System.Random _random;

        public RandomService() : this(System.Environment.TickCount) { }

        public RandomService(int seed)
        {
            _random = new System.Random(seed);
        }

        public float Value => (float)_random.NextDouble();

        public int Range(int minInclusive, int maxExclusive) =>
            maxExclusive <= minInclusive ? minInclusive : _random.Next(minInclusive, maxExclusive);

        public float Range(float minInclusive, float maxInclusive) =>
            minInclusive + (maxInclusive - minInclusive) * Value;

        public bool Chance(float probability) => Value < probability;

        public T Pick<T>(IReadOnlyList<T> items) => items[Range(0, items.Count)];

        public void Shuffle<T>(IList<T> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var j = _random.Next(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }
    }
}
