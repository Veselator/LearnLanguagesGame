using System.Collections.Generic;
using LearnLanguages.Settings;

namespace LearnLanguages.Battle.AI
{
    public interface IBattleAIProvider
    {
        IBattleAI Get(GameDifficulty difficulty);
    }

    public sealed class BattleAIProvider : IBattleAIProvider
    {
        private readonly Dictionary<GameDifficulty, IBattleAI> _strategies = new Dictionary<GameDifficulty, IBattleAI>();

        public BattleAIProvider(List<IBattleAI> strategies)
        {
            foreach (var strategy in strategies) _strategies[strategy.Difficulty] = strategy;
        }

        public IBattleAI Get(GameDifficulty difficulty) =>
            _strategies.TryGetValue(difficulty, out var strategy) ? strategy : _strategies[GameDifficulty.Medium];
    }
}
