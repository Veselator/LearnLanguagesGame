using System;
using System.Collections.Generic;
using LearnLanguages.Battle.Data;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Localization;
using UnityEngine;

namespace LearnLanguages.Battle.Logic
{
    public interface ISimplifiedBattleRunner
    {
        bool IsRunning { get; }
        void Start(BattleSetup setup, Action<BattleResult> onFinished);
    }

    /// <summary>
    /// A battle without the battlefield: a series of questions, the answers decide the outcome.
    /// </summary>
    public sealed class SimplifiedBattleRunner : ISimplifiedBattleRunner
    {
        private readonly ILanguageTaskService _tasks;
        private readonly ILocalizationService _localization;
        private readonly IEventBus _eventBus;
        private readonly SimplifiedBattleSettings _settings;

        public SimplifiedBattleRunner(ILanguageTaskService tasks, ILocalizationService localization, IEventBus eventBus,
            BattleConfig config)
        {
            _tasks = tasks;
            _localization = localization;
            _eventBus = eventBus;
            _settings = config.Simplified;
        }

        public bool IsRunning { get; private set; }

        public void Start(BattleSetup setup, Action<BattleResult> onFinished)
        {
            IsRunning = true;
            var total = _settings.Questions;
            var answered = 0;
            var correct = 0;
            for (var i = 0; i < total; i++)
            {
                var context = _localization.Format("battle.simple.context", setup.Tile.Data.Name, i + 1, total);
                _tasks.Request(new LanguageTaskRequest(context, success =>
                {
                    if (success) correct++;
                    if (++answered < total) return;
                    IsRunning = false;
                    _eventBus.Publish(new NotificationEvent(_localization.Format("battle.simple.result", correct, total)));
                    onFinished(SimplifiedBattleRules.Resolve(setup, correct, _settings));
                }));
            }
        }
    }

    /// <summary>
    /// What a number of correct answers means for the armies: enough of them win the battle,
    /// and the better the human answered, the less their side and the more the enemy suffers.
    /// </summary>
    public static class SimplifiedBattleRules
    {
        public static BattleResult Resolve(BattleSetup setup, int correct, SimplifiedBattleSettings settings)
        {
            var human = setup.Attacker.IsHuman ? setup.Attacker : setup.Defender;
            var enemy = setup.Get(human.Id.Opposite());
            ApplyLosses(human, settings.GetHumanLoss(correct), settings.KilledShare);
            ApplyLosses(enemy, settings.GetEnemyLoss(correct), settings.KilledShare);
            var winner = correct >= settings.CorrectToWin ? human.Id : enemy.Id;
            return new BattleResult(setup, winner, 1);
        }

        private static void ApplyLosses(BattleSide side, float loss, float killedShare)
        {
            ApplyLosses(side.Armies, loss, killedShare);
            ApplyLosses(side.Reinforcements, loss, killedShare);
        }

        private static void ApplyLosses(IReadOnlyList<Army> armies, float loss, float killedShare)
        {
            foreach (var army in armies)
            foreach (var cohort in army.Cohorts)
            {
                var lost = Mathf.RoundToInt(cohort.Healthy * Mathf.Clamp01(loss));
                var killed = Mathf.RoundToInt(lost * Mathf.Clamp01(killedShare));
                cohort.ApplyCasualties(killed, lost - killed);
            }
        }
    }
}
