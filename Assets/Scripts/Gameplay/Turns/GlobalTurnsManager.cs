using System;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Players;

namespace LearnLanguages.Gameplay.Turns
{
    public interface IGlobalTurnsManager
    {
        event Action<PlayerData> OnTurn;
        PlayerData Current { get; }
        int CurrentIndex { get; }
        int Round { get; }
        void Start(int currentIndex, int round, bool applyTurnStart);
        void EndTurn();
    }

    /// <summary>
    /// Classic turn order on the global map: one player acts, then the next one, round after round.
    /// </summary>
    public sealed class GlobalTurnsManager : IGlobalTurnsManager
    {
        private readonly IPlayersRegistry _players;
        private readonly IEconomyService _economy;
        private readonly IEventBus _eventBus;

        public GlobalTurnsManager(IPlayersRegistry players, IEconomyService economy, IEventBus eventBus)
        {
            _players = players;
            _economy = economy;
            _eventBus = eventBus;
        }

        public event Action<PlayerData> OnTurn;
        public PlayerData Current { get; private set; }
        public int CurrentIndex { get; private set; }
        public int Round { get; private set; }

        public void Start(int currentIndex, int round, bool applyTurnStart)
        {
            CurrentIndex = currentIndex;
            Round = round;
            BeginTurn(applyTurnStart);
        }

        public void EndTurn()
        {
            var count = _players.Players.Count;
            for (var i = 0; i < count; i++)
            {
                CurrentIndex++;
                if (CurrentIndex >= count)
                {
                    CurrentIndex = 0;
                    Round++;
                }
                if (_players.Players[CurrentIndex].Data.IsAlive) break;
            }
            BeginTurn(true);
        }

        private void BeginTurn(bool applyTurnStart)
        {
            var player = _players.Players[CurrentIndex];
            Current = player.Data;
            if (applyTurnStart) _economy.ProcessTurnStart(Current);
            _eventBus.Publish(new TurnStartedEvent(Current, Round));
            OnTurn?.Invoke(Current);
            player.NotifyTurnStarted();
        }
    }
}
