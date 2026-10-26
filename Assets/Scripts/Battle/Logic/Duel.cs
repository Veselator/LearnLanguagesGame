namespace LearnLanguages.Battle.Logic
{
    public enum DuelKind
    {
        Melee = 0,
        Ranged = 1
    }

    /// <summary>
    /// The language mini-game played when two cohorts start fighting. Each side's success changes its combat power.
    /// </summary>
    public sealed class Duel
    {
        public Duel(DuelKind kind, BattleCohort initiator, BattleCohort target)
        {
            Kind = kind;
            Initiator = initiator;
            Target = target;
        }

        public DuelKind Kind { get; }
        public BattleCohort Initiator { get; }
        public BattleCohort Target { get; }
        public float InitiatorModifier { get; private set; } = 1f;
        public float TargetModifier { get; private set; } = 1f;
        public bool InitiatorResolved { get; private set; }
        public bool TargetResolved { get; private set; }
        public bool IsResolved => InitiatorResolved && TargetResolved;

        public BattleCohort Get(BattleSideId side) => Initiator.Side == side ? Initiator : Target;

        public float GetModifier(BattleCohort cohort) => cohort == Initiator ? InitiatorModifier : TargetModifier;

        public void SetResult(BattleCohort cohort, float modifier)
        {
            if (cohort == Initiator)
            {
                InitiatorModifier = modifier;
                InitiatorResolved = true;
            }
            else
            {
                TargetModifier = modifier;
                TargetResolved = true;
            }
        }
    }

    public sealed class Engagement
    {
        public Engagement(BattleCohort attacker, BattleCohort defender, Duel duel, bool charge)
        {
            Attacker = attacker;
            Defender = defender;
            Duel = duel;
            ChargePending = charge;
        }

        public BattleCohort Attacker { get; }
        public BattleCohort Defender { get; }
        public Duel Duel { get; }
        public bool ChargePending { get; private set; }

        public bool Involves(BattleCohort a, BattleCohort b) =>
            (Attacker == a && Defender == b) || (Attacker == b && Defender == a);

        public bool Involves(BattleCohort cohort) => Attacker == cohort || Defender == cohort;

        public void ConsumeCharge() => ChargePending = false;
    }

    public enum CombatReportKind
    {
        Melee = 0,
        Ranged = 1,
        Routed = 2,
        Destroyed = 3,
        Shout = 4,
        Arrived = 5
    }

    public readonly struct BattleCombatReport
    {
        public BattleCombatReport(CombatReportKind kind, BattleCohort source, BattleCohort target, int losses)
        {
            Kind = kind;
            Source = source;
            Target = target;
            Losses = losses;
        }

        public CombatReportKind Kind { get; }
        public BattleCohort Source { get; }
        public BattleCohort Target { get; }
        public int Losses { get; }
    }
}
