using System.Collections.Generic;

namespace LearnLanguages.Battle.Logic
{
    public readonly struct BattleStartedEvent
    {
        public BattleStartedEvent(BattleModel model) => Model = model;
        public BattleModel Model { get; }
    }

    /// <summary>Phase, turn or placement changed; views should resync with the model.</summary>
    public readonly struct BattleStateChangedEvent
    {
        public BattleStateChangedEvent(BattleModel model) => Model = model;
        public BattleModel Model { get; }
    }

    public readonly struct BattleTickResolvedEvent
    {
        public BattleTickResolvedEvent(BattleModel model, IReadOnlyList<BattleCombatReport> reports)
        {
            Model = model;
            Reports = reports;
        }

        public BattleModel Model { get; }
        public IReadOnlyList<BattleCombatReport> Reports { get; }
    }

    public readonly struct BattleFinishedEvent
    {
        public BattleFinishedEvent(BattleResult result) => Result = result;
        public BattleResult Result { get; }
    }

    public readonly struct BattleClosedEvent
    {
        public BattleClosedEvent(BattleResult result) => Result = result;
        public BattleResult Result { get; }
    }
}
