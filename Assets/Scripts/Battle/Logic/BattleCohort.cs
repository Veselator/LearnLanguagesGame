using LearnLanguages.Gameplay.Units;
using UnityEngine;

namespace LearnLanguages.Battle.Logic
{
    public enum CohortBattleState
    {
        Reserve = 0,
        Active = 1,
        Routed = 2,
        Destroyed = 3
    }

    /// <summary>
    /// A cohort deployed on the battlefield. Works on a copy of the global cohort and writes back when the battle ends.
    /// </summary>
    public sealed class BattleCohort
    {
        public static readonly Vector2Int NoCell = new Vector2Int(-1, -1);

        public BattleCohort(int id, BattleSideId side, Cohort source, Army army, bool isReserve)
        {
            Id = id;
            Side = side;
            Source = source;
            Army = army;
            Healthy = source.Healthy;
            Wounded = source.Wounded;
            Organization = source.Organization;
            State = isReserve ? CohortBattleState.Reserve : CohortBattleState.Active;
            Cell = NoCell;
            Order = HoldOrder.Instance;
        }

        public int Id { get; }
        public BattleSideId Side { get; }
        public Cohort Source { get; }
        public Army Army { get; }
        public UnitTypeDefinition Type => Source.Type;
        public int MaxSoldiers => Source.MaxSoldiers;
        public int Healthy { get; private set; }
        public int Wounded { get; private set; }
        public float Organization { get; private set; }
        public CohortBattleState State { get; private set; }

        /// <summary>x = column, y = row.</summary>
        public Vector2Int Cell { get; private set; }

        public BattleOrder Order { get; private set; }
        public bool IsDefending { get; private set; }
        public bool MovedThisTick { get; private set; }

        public bool IsCommander => Source.IsCommander;
        public bool IsActive => State == CohortBattleState.Active;
        public bool IsOnField => State == CohortBattleState.Active && Cell != NoCell;
        public float StrengthRatio => MaxSoldiers > 0 ? (float)Healthy / MaxSoldiers : 0f;

        public void PlaceAt(Vector2Int cell) => Cell = cell;

        public void SetOrder(BattleOrder order) => Order = order ?? HoldOrder.Instance;

        public void SetDefending(bool defending) => IsDefending = defending;

        public void SetMovedThisTick(bool moved) => MovedThisTick = moved;

        public void Activate(Vector2Int cell)
        {
            State = CohortBattleState.Active;
            Cell = cell;
        }

        public void ChangeOrganization(float delta) => Organization = Mathf.Clamp01(Organization + delta);

        /// <summary>Applies losses. Returns the number of soldiers removed from the fight.</summary>
        public int TakeLosses(int losses, float killedShare)
        {
            losses = Mathf.Clamp(losses, 0, Healthy);
            var killed = Mathf.RoundToInt(losses * killedShare);
            Healthy -= losses;
            Wounded += losses - killed;
            if (Healthy <= 0)
            {
                State = CohortBattleState.Destroyed;
                Cell = NoCell;
            }
            return losses;
        }

        public void Rout()
        {
            if (State != CohortBattleState.Active) return;
            State = CohortBattleState.Routed;
            Cell = NoCell;
            Order = HoldOrder.Instance;
        }

        public void WriteBack(bool lostBattle)
        {
            var organization = State == CohortBattleState.Routed || lostBattle ? Mathf.Min(Organization, 0.2f) : Organization;
            Source.SetState(Healthy, Wounded, organization);
        }
    }
}
