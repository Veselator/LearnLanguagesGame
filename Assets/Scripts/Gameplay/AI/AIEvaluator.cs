using LearnLanguages.Gameplay.Players;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Scores a position: the ten criteria, each in [-1; 1], multiplied by the normalized priorities of the AI model.
    /// A finished game scores beyond any regular position.
    /// </summary>
    public sealed class AIEvaluator
    {
        public const float TerminalScore = 10f;

        private const float IncomeScale = 40f;
        private const float TreasuryScale = 300f;
        private const float InfrastructureScale = 10f;
        private const float ArmySizeScale = 12f;
        /// <summary>About four full cohorts: the strength at which an army starts to matter on the map.</summary>
        private const float StrengthScale = 400f;

        private readonly float[] _weights;
        private readonly float[] _buffer = new float[AICriteria.Count];

        public AIEvaluator(float[] normalizedPriorities)
        {
            _weights = normalizedPriorities;
        }

        public float[] Weights => _weights;

        /// <summary>Score of a position that ended the game; sooner endings count more.</summary>
        public static float TerminalValue(AISimulation state, int remainingDepth)
        {
            var value = TerminalScore + remainingDepth;
            return state.OwnCapitalLost ? -value : value;
        }

        public float Score(AISimulation state) => Score(state, _buffer);

        /// <summary>Fills <paramref name="criteria"/> with the value of each criterion and returns their weighted sum.</summary>
        public float Score(AISimulation state, float[] criteria)
        {
            Compute(state, criteria);
            var score = 0f;
            for (var i = 0; i < AICriteria.Count; i++) score += criteria[i] * _weights[i];
            return score;
        }

        private static void Compute(AISimulation state, float[] criteria)
        {
            var world = state.World;
            var player = world.PlayerId;

            int ownLand = 0, enemyLand = 0, upgrades = 0;
            float settlements = 0f, income = 0f;
            for (var tile = 0; tile < world.TileCount; tile++)
            {
                if (!world.IsLand[tile]) continue;
                var owner = state.GetOwner(tile);
                if (owner == player)
                {
                    ownLand++;
                    settlements += world.SettlementWeight[tile];
                    income += state.GetYield(tile);
                    upgrades += state.GetUpgradeCount(tile);
                }
                else if (owner != PlayerData.NeutralId) enemyLand++;
            }

            float ownStrength = 0f, enemyStrength = 0f, advance = 0f, threat = 0f, defence = 0f;
            var cohorts = 0;
            var capital = world.CapitalTile;
            for (var i = 0; i < state.ArmyCount; i++)
            {
                var army = state.GetArmy(i);
                if (!army.Alive) continue;
                if (army.Owner == player)
                {
                    ownStrength += army.Strength;
                    cohorts += army.Cohorts;
                    income -= army.Upkeep;
                    var distance = state.GetGoalDistance(army.Tile);
                    if (distance >= 0) advance += army.Strength / (1f + distance);
                    if (army.Tile == capital) defence += army.Strength;
                }
                else if (army.Owner != PlayerData.NeutralId)
                {
                    enemyStrength += army.Strength;
                    if (capital >= 0 && IsAdjacent(world, capital, army.Tile)) threat += army.Strength;
                }
            }

            var capturedCapitals = 0;
            foreach (var enemyCapital in world.EnemyCapitals)
                if (state.GetOwner(enemyCapital.Tile) == player) capturedCapitals++;

            var treasury = state.Resources.Value;
            criteria[(int)AICriterion.Territory] = world.LandCount > 0 ? (float)(ownLand - enemyLand) / world.LandCount : 0f;
            criteria[(int)AICriterion.Settlements] = world.TotalSettlementWeight > 0f ? settlements / world.TotalSettlementWeight : 0f;
            criteria[(int)AICriterion.Income] = income / (System.Math.Abs(income) + IncomeScale);
            criteria[(int)AICriterion.Treasury] = treasury / (treasury + TreasuryScale);
            criteria[(int)AICriterion.Infrastructure] = upgrades / (upgrades + InfrastructureScale);
            criteria[(int)AICriterion.ArmyStrength] = ownStrength / (ownStrength + enemyStrength + StrengthScale);
            criteria[(int)AICriterion.ArmySize] = cohorts / (cohorts + ArmySizeScale);
            criteria[(int)AICriterion.CapitalSafety] = CapitalSafety(state, defence, threat);
            criteria[(int)AICriterion.EnemyCapitals] = world.EnemyCapitals.Count > 0 ? (float)capturedCapitals / world.EnemyCapitals.Count : 1f;
            criteria[(int)AICriterion.Advance] = advance / (advance + StrengthScale);
        }

        private static float CapitalSafety(AISimulation state, float defence, float threat)
        {
            if (state.OwnCapitalLost) return 0f;
            return threat > 0f ? defence / (defence + threat) : 1f;
        }

        private static bool IsAdjacent(AIWorld world, int tile, int other)
        {
            if (tile == other) return true;
            foreach (var neighbour in world.Neighbours[tile])
                if (neighbour == other) return true;
            return false;
        }
    }
}
