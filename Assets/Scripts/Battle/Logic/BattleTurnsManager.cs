using System;
using LearnLanguages.Gameplay.Players;

namespace LearnLanguages.Battle.Logic
{
    /// <summary>
    /// Hands the command phase to both sides (OnTurn) and waits until both have finished giving orders.
    /// </summary>
    public sealed class BattleTurnsManager
    {
        private readonly bool[] _ready = new bool[2];

        public event Action<PlayerData> OnTurn;
        public event Action AllSidesReady;

        public BattleModel Model { get; private set; }

        public void BeginCommandPhase(BattleModel model)
        {
            Model = model;
            _ready[0] = false;
            _ready[1] = false;
            OnTurn?.Invoke(model.Setup.Attacker.Player);
            OnTurn?.Invoke(model.Setup.Defender.Player);
        }

        public BattleSideId GetSideOf(PlayerData player) =>
            Model.Setup.Attacker.Player == player ? BattleSideId.Attacker : BattleSideId.Defender;

        public bool IsReady(BattleSideId side) => _ready[(int)side];

        public void MarkReady(BattleSideId side)
        {
            if (Model == null || _ready[(int)side]) return;
            _ready[(int)side] = true;
            if (_ready[0] && _ready[1]) AllSidesReady?.Invoke();
        }

        public void Clear() => Model = null;
    }
}
