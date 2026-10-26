using System;
using LearnLanguages.Battle.AI;
using LearnLanguages.Battle.Data;
using LearnLanguages.Core.Events;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.Players;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Battle.Logic
{
    public interface IBattleController
    {
        BattleModel Model { get; }
        bool IsRunning { get; }
        bool IsWaitingForHumanOrders { get; }
        BattleSideId HumanSide { get; }
        void Start(BattleSetup setup, Action<BattleResult> onFinished);
        void ConfirmDeployment();
        void ExecuteOrders();
        void RequestShout(BattleCohort cohort);
        void Retreat();
        void Close();
        void NotifyOrdersChanged();
    }

    /// <summary>
    /// Runs an interactive battle: deployment, simultaneous command phase and paced action ticks with language duels.
    /// </summary>
    public sealed class BattleController : IBattleController, IInitializable, IDisposable, ITickable
    {
        private enum Stage
        {
            Idle,
            Deployment,
            Command,
            WaitingTick,
            ResolvingDuels,
            Finished
        }

        private readonly BattleConfig _config;
        private readonly IRandomService _random;
        private readonly IEventBus _eventBus;
        private readonly BattleTurnsManager _turns;
        private readonly IBattleAIProvider _aiProvider;
        private readonly IDuelResolver _duels;

        private Stage _stage;
        private float _timer;
        private Action<BattleResult> _onFinished;
        private BattleResult _result;
        private bool _shoutPending;

        public BattleController(BattleConfig config, IRandomService random, IEventBus eventBus, BattleTurnsManager turns,
            IBattleAIProvider aiProvider, IDuelResolver duels)
        {
            _config = config;
            _random = random;
            _eventBus = eventBus;
            _turns = turns;
            _aiProvider = aiProvider;
            _duels = duels;
        }

        public BattleModel Model { get; private set; }
        public bool IsRunning => _stage != Stage.Idle;
        public BattleSideId HumanSide { get; private set; }

        public bool IsWaitingForHumanOrders =>
            _stage == Stage.Command && !_turns.IsReady(HumanSide) && !_shoutPending;

        public void Initialize()
        {
            _turns.OnTurn += HandleTurn;
            _turns.AllSidesReady += HandleAllReady;
        }

        public void Dispose()
        {
            _turns.OnTurn -= HandleTurn;
            _turns.AllSidesReady -= HandleAllReady;
        }

        public void Start(BattleSetup setup, Action<BattleResult> onFinished)
        {
            _onFinished = onFinished;
            _result = null;
            Model = new BattleModel(setup, _config, _random);
            HumanSide = setup.Attacker.IsHuman ? BattleSideId.Attacker : BattleSideId.Defender;

            var aiSide = HumanSide.Opposite();
            _aiProvider.Get(AiDifficultyOf(setup.Get(aiSide).Player)).Deploy(Model, aiSide);
            Model.AutoDeploy(HumanSide);

            _stage = Stage.Deployment;
            _eventBus.Publish(new BattleStartedEvent(Model));
        }

        public void ConfirmDeployment()
        {
            if (_stage != Stage.Deployment) return;
            Model.BeginBattle();
            BeginCommandPhase();
        }

        public void ExecuteOrders()
        {
            if (_stage == Stage.Command && !_shoutPending) _turns.MarkReady(HumanSide);
        }

        public void RequestShout(BattleCohort cohort)
        {
            if (_stage != Stage.Command || _shoutPending || cohort.Side != HumanSide || !cohort.IsOnField) return;
            _shoutPending = true;
            _duels.ResolveShout(Model, cohort, true, success =>
            {
                _shoutPending = false;
                Model.OrderShout(cohort, success);
                NotifyOrdersChanged();
            });
        }

        public void Retreat()
        {
            if (_stage != Stage.Command && _stage != Stage.Deployment) return;
            Model.Surrender(HumanSide);
            Finish();
        }

        public void Close()
        {
            if (_stage != Stage.Finished) return;
            _stage = Stage.Idle;
            _turns.Clear();
            var callback = _onFinished;
            var result = _result;
            _onFinished = null;
            Model = null;
            _eventBus.Publish(new BattleClosedEvent(result));
            callback?.Invoke(result);
        }

        public void NotifyOrdersChanged()
        {
            if (Model != null) _eventBus.Publish(new BattleStateChangedEvent(Model));
        }

        public void Tick()
        {
            if (_stage != Stage.WaitingTick) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _stage = Stage.ResolvingDuels;
            var duels = Model.PrepareTick();
            _eventBus.Publish(new BattleStateChangedEvent(Model));
            _duels.ResolveDuels(Model, duels, true, OnDuelsResolved);
        }

        private void OnDuelsResolved()
        {
            Model.ResolveTick();
            _eventBus.Publish(new BattleTickResolvedEvent(Model, Model.Reports));

            switch (Model.Phase)
            {
                case BattlePhase.Finished:
                    Finish();
                    break;
                case BattlePhase.Command:
                    BeginCommandPhase();
                    break;
                default:
                    _stage = Stage.WaitingTick;
                    _timer = _config.TickDuration;
                    break;
            }
        }

        private void BeginCommandPhase()
        {
            _stage = Stage.Command;
            _eventBus.Publish(new BattleStateChangedEvent(Model));
            _turns.BeginCommandPhase(Model);
        }

        private void HandleTurn(PlayerData player)
        {
            var side = _turns.GetSideOf(player);
            if (Model.Setup.Get(side).IsHuman) return;
            _aiProvider.Get(AiDifficultyOf(player)).GiveOrders(Model, side, _duels.GetAiSuccessChance(player));
            _turns.MarkReady(side);
        }

        private void HandleAllReady()
        {
            Model.BeginAction();
            _stage = Stage.WaitingTick;
            _timer = 0.2f;
            _eventBus.Publish(new BattleStateChangedEvent(Model));
        }

        private void Finish()
        {
            _stage = Stage.Finished;
            _result = new BattleResult(Model.Setup, Model.Winner, Model.Turn);
            _eventBus.Publish(new BattleStateChangedEvent(Model));
            _eventBus.Publish(new BattleFinishedEvent(_result));
        }

        private static Settings.GameDifficulty AiDifficultyOf(PlayerData player) => player.AiDifficulty;
    }
}
