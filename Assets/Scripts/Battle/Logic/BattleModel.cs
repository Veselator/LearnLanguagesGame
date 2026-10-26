using System.Collections.Generic;
using LearnLanguages.Battle.Data;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.Units;
using UnityEngine;

namespace LearnLanguages.Battle.Logic
{
    /// <summary>
    /// Pure battle simulation: deployment, orders, ticks, combat and victory conditions.
    /// Each tick runs in two steps so language duels can be resolved (possibly asynchronously) in between:
    /// <see cref="PrepareTick"/> and <see cref="ResolveTick"/>.
    /// </summary>
    public sealed class BattleModel
    {
        private readonly List<BattleCohort> _cohorts = new List<BattleCohort>();
        private readonly List<Engagement> _engagements = new List<Engagement>();
        private readonly List<Duel> _pendingDuels = new List<Duel>();
        private readonly List<(BattleCohort shooter, BattleCohort target, Duel duel)> _volleys =
            new List<(BattleCohort, BattleCohort, Duel)>();
        private readonly List<BattleCombatReport> _reports = new List<BattleCombatReport>();
        private readonly List<BattleCohort> _ordered = new List<BattleCohort>();
        private readonly BattleCohort[] _mainCommanders = new BattleCohort[2];
        private readonly IRandomService _random;

        public BattleModel(BattleSetup setup, BattleConfig config, IRandomService random)
        {
            Setup = setup;
            Config = config;
            _random = random;
            Grid = new BattleGrid(setup.Battlefield, config);
            Phase = BattlePhase.Deployment;

            AddSide(setup.Attacker);
            AddSide(setup.Defender);
        }

        public BattleSetup Setup { get; }
        public BattleConfig Config { get; }
        public BattleGrid Grid { get; }
        public BattlePhase Phase { get; private set; }
        public int Turn { get; private set; }
        public int Tick { get; private set; }
        public BattleSideId Winner { get; private set; }
        public IReadOnlyList<BattleCohort> Cohorts => _cohorts;
        public IReadOnlyList<Engagement> Engagements => _engagements;
        public IReadOnlyList<BattleCombatReport> Reports => _reports;
        public bool IsFinished => Phase == BattlePhase.Finished;

        #region Queries

        public BattleCohort GetCohortAt(Vector2Int cell)
        {
            foreach (var cohort in _cohorts)
                if (cohort.IsOnField && cohort.Cell == cell) return cohort;
            return null;
        }

        public IEnumerable<BattleCohort> GetSide(BattleSideId side)
        {
            foreach (var cohort in _cohorts)
                if (cohort.Side == side) yield return cohort;
        }

        public BattleCohort GetMainCommander(BattleSideId side) => _mainCommanders[(int)side];

        public float GetSideMorale(BattleSideId side)
        {
            var sum = 0f;
            var count = 0;
            foreach (var cohort in _cohorts)
            {
                if (cohort.Side != side || cohort.State == CohortBattleState.Reserve) continue;
                count++;
                if (cohort.IsActive) sum += cohort.Organization * cohort.StrengthRatio;
            }
            return count == 0 ? 0f : sum / count;
        }

        public bool IsDeploymentCell(BattleSideId side, Vector2Int cell)
        {
            if (!Grid.IsInside(cell)) return false;
            return side == BattleSideId.Attacker
                ? cell.x < Config.DeploymentColumns
                : cell.x >= BattleGrid.Columns - Config.DeploymentColumns;
        }

        public Dictionary<Vector2Int, int> GetReachableCells(BattleCohort cohort) =>
            Grid.GetReachable(cohort.Cell, cohort.Type.Speed, cell => GetCohortAt(cell) != null);

        /// <summary>Enemies that can be attacked this turn (adjacent to a reachable cell).</summary>
        public List<BattleCohort> GetAttackTargets(BattleCohort cohort)
        {
            var result = new List<BattleCohort>();
            var reachable = GetReachableCells(cohort);
            foreach (var enemy in _cohorts)
            {
                if (enemy.Side == cohort.Side || !enemy.IsOnField) continue;
                foreach (var direction in BattleGrid.OrthogonalDirections)
                {
                    if (!reachable.ContainsKey(enemy.Cell + direction)) continue;
                    result.Add(enemy);
                    break;
                }
            }
            return result;
        }

        public bool CanShoot(BattleCohort shooter, BattleCohort target) =>
            shooter.Type.IsRanged && target != null && target.IsOnField && target.Side != shooter.Side &&
            BattleGrid.Distance(shooter.Cell, target.Cell) <= shooter.Type.Range &&
            Mathf.Abs(shooter.Cell.y - target.Cell.y) <= Config.ShootRowSpread;

        public List<BattleCohort> GetShootTargets(BattleCohort shooter)
        {
            var result = new List<BattleCohort>();
            foreach (var enemy in _cohorts)
                if (CanShoot(shooter, enemy)) result.Add(enemy);
            return result;
        }

        public bool IsEngaged(BattleCohort cohort)
        {
            foreach (var engagement in _engagements)
                if (engagement.Involves(cohort)) return true;
            return false;
        }

        #endregion

        #region Deployment

        public bool CanDeploy(BattleCohort cohort, Vector2Int cell) =>
            Phase == BattlePhase.Deployment && cohort.IsActive && IsDeploymentCell(cohort.Side, cell) &&
            Grid.IsPassable(cell);

        /// <summary>Places a cohort; swaps with a friendly cohort already standing there.</summary>
        public bool Deploy(BattleCohort cohort, Vector2Int cell)
        {
            if (!CanDeploy(cohort, cell)) return false;
            var occupant = GetCohortAt(cell);
            if (occupant != null && occupant != cohort)
            {
                if (occupant.Side != cohort.Side) return false;
                occupant.PlaceAt(cohort.Cell);
            }
            cohort.PlaceAt(cell);
            return true;
        }

        public void AutoDeploy(BattleSideId side)
        {
            foreach (var cohort in _cohorts)
                if (cohort.Side == side && cohort.IsActive) cohort.PlaceAt(BattleCohort.NoCell);

            foreach (var cohort in _cohorts)
                if (cohort.Side == side && cohort.State == CohortBattleState.Active)
                    cohort.PlaceAt(FindDeploymentCell(side, cohort.Type.Class, true));
        }

        public void BeginBattle()
        {
            if (Phase != BattlePhase.Deployment) return;
            foreach (var cohort in _cohorts)
                if (cohort.IsActive && cohort.Cell == BattleCohort.NoCell)
                    cohort.PlaceAt(FindDeploymentCell(cohort.Side, cohort.Type.Class, true));
            Turn = 1;
            Phase = BattlePhase.Command;
        }

        #endregion

        #region Orders

        public bool TryOrderMove(BattleCohort cohort, Vector2Int target)
        {
            if (Phase != BattlePhase.Command || !cohort.IsOnField || target == cohort.Cell) return false;
            var path = Grid.FindPath(cohort.Cell, target, cohort.Type.Speed + AttackStepAllowance(target, cohort),
                cell => GetCohortAt(cell) != null);
            if (path == null || path.Count == 0) return false;

            var occupant = GetCohortAt(target);
            if (occupant != null && occupant.Side == cohort.Side) return false;
            cohort.SetOrder(new MoveOrder(path));
            return true;
        }

        public void OrderHold(BattleCohort cohort) => SetCommandOrder(cohort, HoldOrder.Instance);

        public void OrderDefend(BattleCohort cohort) => SetCommandOrder(cohort, new DefendOrder());

        public void OrderShout(BattleCohort cohort, bool success) => SetCommandOrder(cohort, new ShoutOrder(success));

        public bool TryOrderShoot(BattleCohort shooter, BattleCohort target)
        {
            if (!CanShoot(shooter, target)) return false;
            SetCommandOrder(shooter, new ShootOrder(target));
            return true;
        }

        private void SetCommandOrder(BattleCohort cohort, BattleOrder order)
        {
            if (Phase == BattlePhase.Command && cohort.IsOnField) cohort.SetOrder(order);
        }

        // The enemy cell itself costs a step when attacking, but entering it never happens.
        private int AttackStepAllowance(Vector2Int target, BattleCohort cohort)
        {
            var occupant = GetCohortAt(target);
            return occupant != null && occupant.Side != cohort.Side ? Grid.GetMoveCost(target) : 0;
        }

        #endregion

        #region Action phase

        public void BeginAction()
        {
            if (Phase != BattlePhase.Command) return;
            Phase = BattlePhase.Action;
            Tick = 0;
            _reports.Clear();
            foreach (var cohort in _cohorts)
            {
                cohort.SetDefending(false);
                if (cohort.IsOnField) cohort.Order.OnActionStarted(this, cohort);
            }
            DropBrokenEngagements();
        }

        /// <summary>Executes orders of the next tick and returns duels that must be resolved before <see cref="ResolveTick"/>.</summary>
        public IReadOnlyList<Duel> PrepareTick()
        {
            Tick++;
            if (Tick > 1) _reports.Clear();
            _pendingDuels.Clear();
            _volleys.Clear();

            _ordered.Clear();
            foreach (var cohort in _cohorts)
            {
                cohort.SetMovedThisTick(false);
                if (cohort.IsOnField) _ordered.Add(cohort);
            }
            _ordered.Sort(CompareInitiative);

            foreach (var cohort in _ordered)
                if (cohort.IsOnField && cohort.Order.Priority == 0) cohort.Order.ExecuteTick(this, cohort);
            foreach (var cohort in _ordered)
                if (cohort.IsOnField && cohort.Order.Priority > 0) cohort.Order.ExecuteTick(this, cohort);

            return _pendingDuels;
        }

        public void ResolveTick()
        {
            foreach (var (shooter, target, duel) in _volleys)
            {
                if (!shooter.IsOnField || !target.IsOnField) continue;
                var power = Power(shooter, shooter.Type.RangedAttack, duel.GetModifier(shooter));
                var losses = Losses(power * Config.RangedDamageFactor, target, duel.GetModifier(target));
                ApplyLosses(target, losses);
                _reports.Add(new BattleCombatReport(CombatReportKind.Ranged, shooter, target, losses));
            }

            foreach (var engagement in _engagements)
            {
                var a = engagement.Attacker;
                var b = engagement.Defender;
                if (!a.IsOnField || !b.IsOnField || !BattleGrid.AreAdjacent(a.Cell, b.Cell)) continue;

                var charge = engagement.ChargePending ? 1f + a.Type.ChargeBonus : 1f;
                engagement.ConsumeCharge();
                var duel = engagement.Duel;
                var lossesB = Losses(Power(a, a.Type.Attack * charge, duel.GetModifier(a)) * Config.DamageFactor, b,
                    duel.GetModifier(b));
                var lossesA = Losses(Power(b, b.Type.Attack, duel.GetModifier(b)) * Config.DamageFactor, a,
                    duel.GetModifier(a));
                ApplyLosses(b, lossesB);
                ApplyLosses(a, lossesA);
                _reports.Add(new BattleCombatReport(CombatReportKind.Melee, a, b, lossesB));
                _reports.Add(new BattleCombatReport(CombatReportKind.Melee, b, a, lossesA));
            }

            foreach (var cohort in _cohorts)
            {
                if (cohort.State == CohortBattleState.Active && cohort.Organization < Config.RoutThreshold)
                {
                    cohort.Rout();
                    _reports.Add(new BattleCombatReport(CombatReportKind.Routed, cohort, null, 0));
                }
            }
            DropBrokenEngagements();

            if (CheckVictory()) return;
            if (Tick >= Config.TicksPerTurn) EndTurn();
        }

        public void ApplyShout(BattleCohort shouter, bool success)
        {
            var bonus = success ? Config.ShoutSuccessBonus : Config.ShoutFailBonus;
            foreach (var cohort in _cohorts)
            {
                if (cohort.Side != shouter.Side || !cohort.IsOnField) continue;
                var delta = cohort.Cell - shouter.Cell;
                if (Mathf.Abs(delta.x) <= 1 && Mathf.Abs(delta.y) <= 1) cohort.ChangeOrganization(bonus);
            }
            _reports.Add(new BattleCombatReport(CombatReportKind.Shout, shouter, null, success ? 1 : 0));
        }

        public void StepAlongPath(BattleCohort cohort, MoveOrder order)
        {
            var steps = Mathf.Max(1, Mathf.CeilToInt(cohort.Type.Speed / (float)Config.TicksPerTurn));
            for (var i = 0; i < steps && !order.IsCompleted; i++)
            {
                var next = order.NextCell;
                var occupant = GetCohortAt(next);
                if (occupant == null)
                {
                    if (!Grid.IsPassable(next))
                    {
                        order.Complete();
                        return;
                    }
                    if (IsEngaged(cohort)) LeaveEngagements(cohort);
                    cohort.PlaceAt(next);
                    cohort.SetMovedThisTick(true);
                    order.Advance();
                    continue;
                }

                if (occupant.Side != cohort.Side && BattleGrid.AreAdjacent(cohort.Cell, next))
                {
                    StartEngagement(cohort, occupant, cohort.MovedThisTick);
                    order.Complete();
                }
                return;
            }
        }

        public void PrepareVolley(BattleCohort shooter, ShootOrder order)
        {
            if (IsEngaged(shooter)) return;
            var target = CanShoot(shooter, order.Target) ? order.Target : FindNearestShootTarget(shooter);
            if (target == null) return;
            if (target != order.Target) order.Retarget(target);

            if (order.Duel == null || order.Duel.Target != target)
            {
                order.AttachDuel(new Duel(DuelKind.Ranged, shooter, target));
                _pendingDuels.Add(order.Duel);
            }
            _volleys.Add((shooter, target, order.Duel));
        }

        private void StartEngagement(BattleCohort attacker, BattleCohort defender, bool charge)
        {
            foreach (var existing in _engagements)
                if (existing.Involves(attacker, defender)) return;
            var duel = new Duel(DuelKind.Melee, attacker, defender);
            _engagements.Add(new Engagement(attacker, defender, duel, charge));
            _pendingDuels.Add(duel);
        }

        private void LeaveEngagements(BattleCohort cohort) => _engagements.RemoveAll(e => e.Involves(cohort));

        private void DropBrokenEngagements() =>
            _engagements.RemoveAll(e => !e.Attacker.IsOnField || !e.Defender.IsOnField ||
                                        !BattleGrid.AreAdjacent(e.Attacker.Cell, e.Defender.Cell));

        private float Power(BattleCohort cohort, float attack, float modifier) =>
            cohort.Healthy * attack * modifier * (0.5f + 0.5f * cohort.Organization);

        private int Losses(float damage, BattleCohort target, float targetModifier)
        {
            var defense = target.Type.Defense * Grid.GetDefenseMultiplier(target.Cell) * targetModifier;
            if (target.IsDefending) defense *= Config.DefendMultiplier;
            var losses = damage * _random.Range(0.85f, 1.15f) / Mathf.Max(1f, defense);
            return Mathf.Clamp(Mathf.RoundToInt(losses), 0, target.Healthy);
        }

        private void ApplyLosses(BattleCohort target, int losses)
        {
            if (losses <= 0 || !target.IsOnField) return;
            var lossShare = (float)losses / target.MaxSoldiers;
            target.TakeLosses(losses, Config.KilledShare);
            target.ChangeOrganization(-(lossShare * Config.OrganizationLossFactor + Config.OrganizationLossPerFight));
            if (target.State == CohortBattleState.Destroyed)
                _reports.Add(new BattleCombatReport(CombatReportKind.Destroyed, target, null, 0));
        }

        private BattleCohort FindNearestShootTarget(BattleCohort shooter)
        {
            BattleCohort best = null;
            var bestDistance = int.MaxValue;
            foreach (var enemy in _cohorts)
            {
                if (!CanShoot(shooter, enemy)) continue;
                var distance = BattleGrid.Distance(shooter.Cell, enemy.Cell);
                if (distance >= bestDistance) continue;
                best = enemy;
                bestDistance = distance;
            }
            return best;
        }

        private static int CompareInitiative(BattleCohort a, BattleCohort b)
        {
            var bySpeed = b.Type.Speed.CompareTo(a.Type.Speed);
            return bySpeed != 0 ? bySpeed : a.Id.CompareTo(b.Id);
        }

        #endregion

        #region Turn flow

        private void EndTurn()
        {
            Turn++;
            if (Turn > Config.MaxTurns)
            {
                Finish(BattleSideId.Defender);
                return;
            }

            // Orders live for one turn only; every command phase starts from scratch.
            foreach (var cohort in _cohorts) cohort.SetOrder(HoldOrder.Instance);

            if (Turn == Config.ReinforcementTurn) BringReinforcements();
            Phase = BattlePhase.Command;
        }

        private void BringReinforcements()
        {
            foreach (var cohort in _cohorts)
            {
                if (cohort.State != CohortBattleState.Reserve) continue;
                var cell = FindDeploymentCell(cohort.Side, cohort.Type.Class, false);
                if (cell == BattleCohort.NoCell) continue;
                cohort.Activate(cell);
                _reports.Add(new BattleCombatReport(CombatReportKind.Arrived, cohort, null, 0));
            }
        }

        private bool CheckVictory()
        {
            var attackerLost = IsDefeated(BattleSideId.Attacker);
            var defenderLost = IsDefeated(BattleSideId.Defender);
            if (!attackerLost && !defenderLost) return false;
            Finish(attackerLost ? BattleSideId.Defender : BattleSideId.Attacker);
            return true;
        }

        private bool IsDefeated(BattleSideId side)
        {
            var commander = _mainCommanders[(int)side];
            if (commander != null && (commander.State == CohortBattleState.Routed ||
                                      commander.State == CohortBattleState.Destroyed))
                return true;

            var anyLeft = false;
            foreach (var cohort in _cohorts)
                if (cohort.Side == side && (cohort.IsActive || cohort.State == CohortBattleState.Reserve)) anyLeft = true;
            return !anyLeft || GetSideMorale(side) < Config.ArmyDefeatMorale;
        }

        /// <summary>Ends the battle immediately, e.g. when the player retreats.</summary>
        public void Surrender(BattleSideId side)
        {
            if (!IsFinished) Finish(side.Opposite());
        }

        private void Finish(BattleSideId winner)
        {
            Winner = winner;
            Phase = BattlePhase.Finished;
            _engagements.Clear();
            foreach (var cohort in _cohorts) cohort.WriteBack(cohort.Side != winner);
        }

        #endregion

        #region Setup

        private void AddSide(BattleSide side)
        {
            foreach (var army in side.Armies) AddArmy(side.Id, army, false);
            foreach (var army in side.Reinforcements) AddArmy(side.Id, army, true);
        }

        private void AddArmy(BattleSideId side, Army army, bool reserve)
        {
            foreach (var cohort in army.Cohorts)
            {
                if (cohort.Healthy <= 0) continue;
                var battleCohort = new BattleCohort(_cohorts.Count, side, cohort, army, reserve);
                _cohorts.Add(battleCohort);
                if (!reserve && cohort.IsCommander && _mainCommanders[(int)side] == null)
                    _mainCommanders[(int)side] = battleCohort;
            }
        }

        private Vector2Int FindDeploymentCell(BattleSideId side, UnitClass unitClass, bool preferFormation)
        {
            var columns = Config.DeploymentColumns;
            // Column offsets from the front line: 0 = front, columns-1 = rear.
            int preferredDepth;
            switch (unitClass)
            {
                case UnitClass.Swordsmen: preferredDepth = 0; break;
                case UnitClass.Archers: preferredDepth = 1; break;
                case UnitClass.Cavalry: preferredDepth = 1; break;
                default: preferredDepth = columns - 1; break;
            }
            if (!preferFormation) preferredDepth = 0;

            var rowsOrder = unitClass == UnitClass.Cavalry
                ? new[] { 0, 5, 1, 4, 2, 3 }
                : new[] { 2, 3, 1, 4, 0, 5 };

            for (var depthShift = 0; depthShift < columns; depthShift++)
            {
                var depth = (preferredDepth + depthShift) % columns;
                var column = side == BattleSideId.Attacker ? columns - 1 - depth : BattleGrid.Columns - columns + depth;
                foreach (var row in rowsOrder)
                {
                    var cell = new Vector2Int(column, row);
                    if (Grid.IsPassable(cell) && GetCohortAt(cell) == null) return cell;
                }
            }
            return BattleCohort.NoCell;
        }

        #endregion
    }
}
