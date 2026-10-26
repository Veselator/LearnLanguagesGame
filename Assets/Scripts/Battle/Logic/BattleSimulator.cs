using LearnLanguages.Battle.AI;
using LearnLanguages.Battle.Data;
using LearnLanguages.Core.Randomness;

namespace LearnLanguages.Battle.Logic
{
    public interface IBattleSimulator
    {
        BattleResult Simulate(BattleSetup setup);
    }

    /// <summary>
    /// Auto-resolves a battle without a human: the same model, both sides controlled by AI, duels rolled.
    /// </summary>
    public sealed class BattleSimulator : IBattleSimulator
    {
        private const int SafetyTickLimit = 1000;

        private readonly BattleConfig _config;
        private readonly IRandomService _random;
        private readonly IBattleAIProvider _aiProvider;
        private readonly IDuelResolver _duels;

        public BattleSimulator(BattleConfig config, IRandomService random, IBattleAIProvider aiProvider, IDuelResolver duels)
        {
            _config = config;
            _random = random;
            _aiProvider = aiProvider;
            _duels = duels;
        }

        public BattleResult Simulate(BattleSetup setup)
        {
            var model = new BattleModel(setup, _config, _random);
            var attackerPlayer = setup.Attacker.Player;
            var defenderPlayer = setup.Defender.Player;
            var attackerAi = _aiProvider.Get(attackerPlayer.AiDifficulty);
            var defenderAi = _aiProvider.Get(defenderPlayer.AiDifficulty);

            attackerAi.Deploy(model, BattleSideId.Attacker);
            defenderAi.Deploy(model, BattleSideId.Defender);
            model.BeginBattle();

            var ticks = 0;
            while (!model.IsFinished && ticks < SafetyTickLimit)
            {
                if (model.Phase == BattlePhase.Command)
                {
                    attackerAi.GiveOrders(model, BattleSideId.Attacker, _duels.GetAiSuccessChance(attackerPlayer));
                    defenderAi.GiveOrders(model, BattleSideId.Defender, _duels.GetAiSuccessChance(defenderPlayer));
                    model.BeginAction();
                }

                var duels = model.PrepareTick();
                _duels.ResolveDuels(model, duels, false, () => { });
                model.ResolveTick();
                ticks++;
            }

            if (!model.IsFinished) model.Surrender(BattleSideId.Attacker);
            return new BattleResult(setup, model.Winner, model.Turn);
        }
    }
}
