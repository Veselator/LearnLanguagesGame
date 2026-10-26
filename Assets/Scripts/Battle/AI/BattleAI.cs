using System.Collections.Generic;
using LearnLanguages.Battle.Logic;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Battle.AI
{
    /// <summary>
    /// Battle behaviour of a computer-controlled side (strategy per difficulty).
    /// </summary>
    public interface IBattleAI
    {
        GameDifficulty Difficulty { get; }
        void Deploy(BattleModel model, BattleSideId side);
        void GiveOrders(BattleModel model, BattleSideId side, float duelSuccessChance);
    }

    public abstract class BattleAIBase : IBattleAI
    {
        protected readonly IRandomService Random;

        protected BattleAIBase(IRandomService random)
        {
            Random = random;
        }

        public abstract GameDifficulty Difficulty { get; }

        public virtual void Deploy(BattleModel model, BattleSideId side) => model.AutoDeploy(side);

        public void GiveOrders(BattleModel model, BattleSideId side, float duelSuccessChance)
        {
            var own = new List<BattleCohort>();
            foreach (var cohort in model.GetSide(side))
                if (cohort.IsOnField) own.Add(cohort);
            var enemies = new List<BattleCohort>();
            foreach (var cohort in model.GetSide(side.Opposite()))
                if (cohort.IsOnField) enemies.Add(cohort);
            if (enemies.Count == 0) return;

            BeforeOrders(model, side, own, enemies);
            foreach (var cohort in own) OrderCohort(model, cohort, own, enemies, duelSuccessChance);
        }

        protected virtual void BeforeOrders(BattleModel model, BattleSideId side, List<BattleCohort> own,
            List<BattleCohort> enemies) { }

        protected abstract void OrderCohort(BattleModel model, BattleCohort cohort, List<BattleCohort> own,
            List<BattleCohort> enemies, float duelSuccessChance);

        protected static BattleCohort Nearest(BattleCohort from, List<BattleCohort> candidates)
        {
            BattleCohort best = null;
            var bestDistance = int.MaxValue;
            foreach (var candidate in candidates)
            {
                if (!candidate.IsOnField) continue;
                var distance = BattleGrid.Distance(from.Cell, candidate.Cell);
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        protected static BattleCohort Weakest(List<BattleCohort> candidates)
        {
            BattleCohort best = null;
            foreach (var candidate in candidates)
                if (best == null || candidate.Healthy * candidate.Organization < best.Healthy * best.Organization)
                    best = candidate;
            return best;
        }

        /// <summary>Moves to the reachable cell closest to <paramref name="target"/>, keeping <paramref name="keepDistance"/>.</summary>
        protected static bool MoveTowards(BattleModel model, BattleCohort cohort, Vector2Int target, int keepDistance = 0,
            int maxColumn = -1)
        {
            var reachable = model.GetReachableCells(cohort);
            var best = cohort.Cell;
            var bestScore = Score(cohort.Cell);
            foreach (var pair in reachable)
            {
                if (maxColumn >= 0 && !IsWithinColumnLimit(cohort, pair.Key, maxColumn)) continue;
                var score = Score(pair.Key) + pair.Value * 0.01f;
                if (score >= bestScore) continue;
                best = pair.Key;
                bestScore = score;
            }
            return best != cohort.Cell && model.TryOrderMove(cohort, best);

            float Score(Vector2Int cell) => Mathf.Abs(BattleGrid.Distance(cell, target) - keepDistance);
        }

        protected static bool IsWithinColumnLimit(BattleCohort cohort, Vector2Int cell, int limitColumn) =>
            cohort.Side == BattleSideId.Attacker ? cell.x <= limitColumn : cell.x >= limitColumn;

        protected static bool TryAttack(BattleModel model, BattleCohort cohort, BattleCohort target) =>
            target != null && model.TryOrderMove(cohort, target.Cell);

        protected bool TryShootOrApproach(BattleModel model, BattleCohort archer, List<BattleCohort> enemies, bool weakestFirst)
        {
            var targets = model.GetShootTargets(archer);
            if (targets.Count > 0)
                return model.TryOrderShoot(archer, weakestFirst ? Weakest(targets) : Nearest(archer, targets));
            var nearest = Nearest(archer, enemies);
            return nearest != null && MoveTowards(model, archer, nearest.Cell, archer.Type.Range - 1);
        }
    }

    public sealed class EasyBattleAI : BattleAIBase
    {
        public EasyBattleAI(IRandomService random) : base(random) { }

        public override GameDifficulty Difficulty => GameDifficulty.Easy;

        protected override void OrderCohort(BattleModel model, BattleCohort cohort, List<BattleCohort> own,
            List<BattleCohort> enemies, float duelSuccessChance)
        {
            if (cohort.IsCommander || Random.Chance(0.2f))
            {
                model.OrderHold(cohort);
                return;
            }

            if (cohort.Type.IsRanged)
            {
                TryShootOrApproach(model, cohort, enemies, false);
                return;
            }

            var targets = model.GetAttackTargets(cohort);
            if (targets.Count > 0 && TryAttack(model, cohort, Random.Pick(targets))) return;
            var nearest = Nearest(cohort, enemies);
            if (nearest != null) MoveTowards(model, cohort, nearest.Cell);
        }
    }

    public class MediumBattleAI : BattleAIBase
    {
        public MediumBattleAI(IRandomService random) : base(random) { }

        public override GameDifficulty Difficulty => GameDifficulty.Medium;

        protected float SideMorale { get; private set; }

        protected override void BeforeOrders(BattleModel model, BattleSideId side, List<BattleCohort> own,
            List<BattleCohort> enemies) => SideMorale = model.GetSideMorale(side);

        protected override void OrderCohort(BattleModel model, BattleCohort cohort, List<BattleCohort> own,
            List<BattleCohort> enemies, float duelSuccessChance)
        {
            if (cohort.IsCommander)
            {
                OrderCommander(model, cohort, own, duelSuccessChance);
                return;
            }

            if (cohort.Type.IsRanged)
            {
                TryShootOrApproach(model, cohort, enemies, true);
                return;
            }

            var targets = model.GetAttackTargets(cohort);
            if (targets.Count > 0)
            {
                var target = ChooseMeleeTarget(cohort, targets);
                if (LocalBalance(cohort, own, enemies) < 0.6f && !model.IsEngaged(cohort))
                {
                    model.OrderDefend(cohort);
                    return;
                }
                if (TryAttack(model, cohort, target)) return;
            }

            AdvanceTowardsEnemy(model, cohort, own, enemies);
        }

        protected virtual void OrderCommander(BattleModel model, BattleCohort commander, List<BattleCohort> own,
            float duelSuccessChance)
        {
            if (SideMorale < 0.55f) model.OrderShout(commander, Random.Chance(duelSuccessChance));
            else model.OrderDefend(commander);
        }

        protected virtual BattleCohort ChooseMeleeTarget(BattleCohort cohort, List<BattleCohort> targets) => Weakest(targets);

        protected virtual void AdvanceTowardsEnemy(BattleModel model, BattleCohort cohort, List<BattleCohort> own,
            List<BattleCohort> enemies)
        {
            var nearest = Nearest(cohort, enemies);
            if (nearest != null) MoveTowards(model, cohort, nearest.Cell);
        }

        /// <summary>Own strength around the cohort divided by enemy strength around it.</summary>
        protected static float LocalBalance(BattleCohort cohort, List<BattleCohort> own, List<BattleCohort> enemies)
        {
            const int radius = 3;
            var ours = 0f;
            var theirs = 0.01f;
            foreach (var c in own)
                if (BattleGrid.Distance(c.Cell, cohort.Cell) <= radius) ours += c.Healthy * c.Organization * c.Type.Defense;
            foreach (var e in enemies)
                if (BattleGrid.Distance(e.Cell, cohort.Cell) <= radius) theirs += e.Healthy * e.Organization * e.Type.Attack;
            return ours / theirs;
        }
    }

    public sealed class HardBattleAI : MediumBattleAI
    {
        private BattleCohort _focusTarget;
        private int _frontColumn;

        public HardBattleAI(IRandomService random) : base(random) { }

        public override GameDifficulty Difficulty => GameDifficulty.Hard;

        protected override void BeforeOrders(BattleModel model, BattleSideId side, List<BattleCohort> own,
            List<BattleCohort> enemies)
        {
            base.BeforeOrders(model, side, own, enemies);
            _focusTarget = Weakest(enemies);

            // The infantry line advances together: nobody runs further than two columns ahead of the rearmost swordsmen.
            _frontColumn = side == BattleSideId.Attacker ? int.MaxValue : int.MinValue;
            foreach (var cohort in own)
            {
                if (cohort.Type.Class != UnitClass.Swordsmen) continue;
                _frontColumn = side == BattleSideId.Attacker
                    ? Mathf.Min(_frontColumn, cohort.Cell.x)
                    : Mathf.Max(_frontColumn, cohort.Cell.x);
            }
            if (_frontColumn == int.MaxValue || _frontColumn == int.MinValue) _frontColumn = -1;
            else _frontColumn += side == BattleSideId.Attacker ? 2 : -2;
        }

        protected override BattleCohort ChooseMeleeTarget(BattleCohort cohort, List<BattleCohort> targets)
        {
            if (_focusTarget != null && targets.Contains(_focusTarget)) return _focusTarget;
            if (cohort.Type.Class == UnitClass.Cavalry)
            {
                foreach (var target in targets)
                    if (target.Type.IsRanged || target.IsCommander) return target;
            }
            return Weakest(targets);
        }

        protected override void AdvanceTowardsEnemy(BattleModel model, BattleCohort cohort, List<BattleCohort> own,
            List<BattleCohort> enemies)
        {
            if (cohort.Type.Class == UnitClass.Cavalry)
            {
                // Flank: go for archers or the commander first.
                BattleCohort prey = null;
                foreach (var enemy in enemies)
                    if (enemy.Type.IsRanged || enemy.IsCommander) prey = prey == null ? enemy : Nearest(cohort, new List<BattleCohort> { prey, enemy });
                var goal = prey ?? Nearest(cohort, enemies);
                if (goal != null) MoveTowards(model, cohort, goal.Cell);
                return;
            }

            var nearest = Nearest(cohort, enemies);
            if (nearest == null) return;
            var closeEnough = BattleGrid.Distance(cohort.Cell, nearest.Cell) <= cohort.Type.Speed + 2;
            MoveTowards(model, cohort, nearest.Cell, 0, closeEnough ? -1 : _frontColumn);
        }

        protected override void OrderCommander(BattleModel model, BattleCohort commander, List<BattleCohort> own,
            float duelSuccessChance)
        {
            var friendsAround = 0;
            foreach (var cohort in own)
            {
                var delta = cohort.Cell - commander.Cell;
                if (cohort != commander && Mathf.Abs(delta.x) <= 1 && Mathf.Abs(delta.y) <= 1) friendsAround++;
            }
            if (SideMorale < 0.7f && friendsAround >= 2) model.OrderShout(commander, Random.Chance(duelSuccessChance));
            else model.OrderDefend(commander);
        }
    }
}
