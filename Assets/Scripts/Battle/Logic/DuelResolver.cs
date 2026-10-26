using System;
using System.Collections.Generic;
using LearnLanguages.Battle.Data;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Localization;
using LearnLanguages.Settings;

namespace LearnLanguages.Battle.Logic
{
    /// <summary>
    /// Decides whether one side succeeds in a language duel (strategy: human answers an exercise, AI rolls a die).
    /// </summary>
    public interface IDuelParticipant
    {
        void Resolve(string contextText, Action<bool> callback);
    }

    public sealed class HumanDuelParticipant : IDuelParticipant
    {
        private readonly ILanguageTaskService _tasks;

        public HumanDuelParticipant(ILanguageTaskService tasks)
        {
            _tasks = tasks;
        }

        public void Resolve(string contextText, Action<bool> callback) =>
            _tasks.Request(new LanguageTaskRequest(contextText, callback));
    }

    public sealed class AiDuelParticipant : IDuelParticipant
    {
        private readonly IRandomService _random;
        private readonly float _successChance;

        public AiDuelParticipant(IRandomService random, float successChance)
        {
            _random = random;
            _successChance = successChance;
        }

        public void Resolve(string contextText, Action<bool> callback) => callback(_random.Chance(_successChance));
    }

    public interface IDuelResolver
    {
        /// <summary>Resolves all duels; <paramref name="onComplete"/> may be called synchronously.</summary>
        void ResolveDuels(BattleModel model, IReadOnlyList<Duel> duels, bool interactive, Action onComplete);

        void ResolveShout(BattleModel model, BattleCohort cohort, bool interactive, Action<bool> callback);

        float GetAiSuccessChance(PlayerData player);
    }

    public sealed class DuelResolver : IDuelResolver
    {
        private readonly ILanguageTaskService _tasks;
        private readonly IRandomService _random;
        private readonly PlayersConfig _playersConfig;
        private readonly BattleConfig _battleConfig;
        private readonly ILocalizationService _localization;
        private readonly IGameSession _session;

        public DuelResolver(ILanguageTaskService tasks, IRandomService random, PlayersConfig playersConfig,
            BattleConfig battleConfig, ILocalizationService localization, IGameSession session)
        {
            _tasks = tasks;
            _random = random;
            _playersConfig = playersConfig;
            _battleConfig = battleConfig;
            _localization = localization;
            _session = session;
        }

        public float GetAiSuccessChance(PlayerData player)
        {
            var difficulty = player.IsNeutral ? _session.Settings?.Difficulty ?? GameDifficulty.Medium : player.AiDifficulty;
            return _playersConfig.GetAiDuelSuccess(difficulty);
        }

        public void ResolveDuels(BattleModel model, IReadOnlyList<Duel> duels, bool interactive, Action onComplete)
        {
            var pending = duels.Count * 2;
            if (pending == 0)
            {
                onComplete();
                return;
            }

            // Copy: the model reuses its list on the next tick.
            foreach (var duel in new List<Duel>(duels))
            {
                ResolveSide(model, duel, duel.Initiator, duel.Target, interactive, Done);
                ResolveSide(model, duel, duel.Target, duel.Initiator, interactive, Done);
            }

            void Done()
            {
                pending--;
                if (pending == 0) onComplete();
            }
        }

        public void ResolveShout(BattleModel model, BattleCohort cohort, bool interactive, Action<bool> callback)
        {
            var text = _localization.Format("duel.shout", _localization.Get(cohort.Type.NameKey));
            GetParticipant(model.Setup.Get(cohort.Side).Player, interactive).Resolve(text, callback);
        }

        private void ResolveSide(BattleModel model, Duel duel, BattleCohort cohort, BattleCohort opponent,
            bool interactive, Action done)
        {
            var key = duel.Kind == DuelKind.Ranged
                ? cohort == duel.Initiator ? "duel.shoot" : "duel.under_fire"
                : cohort == duel.Initiator ? "duel.attack" : "duel.defend";
            var text = _localization.Format(key, _localization.Get(cohort.Type.NameKey), _localization.Get(opponent.Type.NameKey));

            GetParticipant(model.Setup.Get(cohort.Side).Player, interactive).Resolve(text, success =>
            {
                duel.SetResult(cohort, success ? _battleConfig.DuelSuccessMultiplier : _battleConfig.DuelFailMultiplier);
                done();
            });
        }

        private IDuelParticipant GetParticipant(PlayerData player, bool interactive) =>
            interactive && player.IsHuman
                ? new HumanDuelParticipant(_tasks)
                : (IDuelParticipant)new AiDuelParticipant(_random, GetAiSuccessChance(player));
    }
}
