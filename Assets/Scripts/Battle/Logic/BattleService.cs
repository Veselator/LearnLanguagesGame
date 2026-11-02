using System;
using LearnLanguages.Settings;

namespace LearnLanguages.Battle.Logic
{
    public interface IBattleService
    {
        bool IsBattleInProgress { get; }
        void StartBattle(BattleSetup setup, Action<BattleResult> onFinished);
    }

    /// <summary>
    /// Entry point used by the global map: when a human takes part, an interactive battle or, in the simplified mode,
    /// a series of questions; auto-resolve otherwise.
    /// </summary>
    public sealed class BattleService : IBattleService
    {
        private readonly IBattleController _controller;
        private readonly IBattleSimulator _simulator;
        private readonly ISimplifiedBattleRunner _simplified;
        private readonly IGameSession _session;

        public BattleService(IBattleController controller, IBattleSimulator simulator, ISimplifiedBattleRunner simplified,
            IGameSession session)
        {
            _controller = controller;
            _simulator = simulator;
            _simplified = simplified;
            _session = session;
        }

        public bool IsBattleInProgress => _controller.IsRunning || _simplified.IsRunning;

        public void StartBattle(BattleSetup setup, Action<BattleResult> onFinished)
        {
            if (!setup.InvolvesHuman) onFinished(_simulator.Simulate(setup));
            else if (_session.Settings.SimplifiedBattles) _simplified.Start(setup, onFinished);
            else _controller.Start(setup, onFinished);
        }
    }
}
