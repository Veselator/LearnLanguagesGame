using System.Collections.Generic;
using UnityEngine;

namespace LearnLanguages.Battle.Logic
{
    public enum BattleOrderType
    {
        Hold = 0,
        Move = 1,
        Defend = 2,
        Shout = 3,
        Shoot = 4
    }

    /// <summary>
    /// Command given to a cohort during the command phase and carried out during the action phase.
    /// </summary>
    public abstract class BattleOrder
    {
        public abstract BattleOrderType Type { get; }

        /// <summary>Lower values are executed first within a tick (shooting happens before movement).</summary>
        public virtual int Priority => 10;

        public virtual void OnActionStarted(BattleModel model, BattleCohort cohort) { }

        public virtual void ExecuteTick(BattleModel model, BattleCohort cohort) { }
    }

    public sealed class HoldOrder : BattleOrder
    {
        public static readonly HoldOrder Instance = new HoldOrder();
        public override BattleOrderType Type => BattleOrderType.Hold;
    }

    public sealed class DefendOrder : BattleOrder
    {
        public override BattleOrderType Type => BattleOrderType.Defend;

        public override void OnActionStarted(BattleModel model, BattleCohort cohort) => cohort.SetDefending(true);
    }

    public sealed class ShoutOrder : BattleOrder
    {
        public ShoutOrder(bool success)
        {
            Success = success;
        }

        public bool Success { get; }
        public override BattleOrderType Type => BattleOrderType.Shout;

        public override void OnActionStarted(BattleModel model, BattleCohort cohort) => model.ApplyShout(cohort, Success);
    }

    public sealed class MoveOrder : BattleOrder
    {
        private readonly List<Vector2Int> _path;

        public MoveOrder(List<Vector2Int> path)
        {
            _path = path;
        }

        public override BattleOrderType Type => BattleOrderType.Move;
        public override int Priority => 1;
        public IReadOnlyList<Vector2Int> Path => _path;
        public int NextIndex { get; private set; }
        public bool IsCompleted => NextIndex >= _path.Count;
        public Vector2Int Destination => _path.Count > 0 ? _path[_path.Count - 1] : BattleCohort.NoCell;
        public Vector2Int NextCell => _path[NextIndex];

        public void Advance() => NextIndex++;

        public void Complete() => NextIndex = _path.Count;

        public override void ExecuteTick(BattleModel model, BattleCohort cohort) => model.StepAlongPath(cohort, this);
    }

    public sealed class ShootOrder : BattleOrder
    {
        public ShootOrder(BattleCohort target)
        {
            Target = target;
        }

        public BattleCohort Target { get; private set; }

        /// <summary>Language duel of the first volley; its modifiers apply to every volley of this order.</summary>
        public Duel Duel { get; private set; }

        public override BattleOrderType Type => BattleOrderType.Shoot;
        public override int Priority => 0;

        public void Retarget(BattleCohort target) => Target = target;

        public void AttachDuel(Duel duel) => Duel = duel;

        public override void ExecuteTick(BattleModel model, BattleCohort cohort) => model.PrepareVolley(cohort, this);
    }
}
