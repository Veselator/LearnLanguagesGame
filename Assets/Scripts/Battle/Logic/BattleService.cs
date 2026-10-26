using System;

namespace LearnLanguages.Battle.Logic
{
    public interface IBattleService
    {
        bool IsBattleInProgress { get; }
        void StartBattle(BattleSetup setup, Action<BattleResult> onFinished);
    }

    /// <summary>
    /// Entry point used by the global map: interactive battle when a human takes part, auto-resolve otherwise.
    /// </summary>
    public sealed class BattleService : IBattleService
    {
        private readonly IBattleController _controller;
        private readonly IBattleSimulator _simulator;

        public BattleService(IBattleController controller, IBattleSimulator simulator)
        {
            _controller = controller;
            _simulator = simulator;
        }

        public bool IsBattleInProgress => _controller.IsRunning;

        public void StartBattle(BattleSetup setup, Action<BattleResult> onFinished)
        {
            if (setup.InvolvesHuman) _controller.Start(setup, onFinished);
            else onFinished(_simulator.Simulate(setup));
        }
    }
}
