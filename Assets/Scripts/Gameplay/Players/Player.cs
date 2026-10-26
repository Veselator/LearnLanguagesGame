using System;

namespace LearnLanguages.Gameplay.Players
{
    /// <summary>
    /// Represents a participant, human or AI. Raises <see cref="OnPlayersTurn"/> when its turn begins.
    /// </summary>
    public sealed class Player
    {
        public Player(PlayerData data)
        {
            Data = data;
        }

        public PlayerData Data { get; }
        public bool IsHuman => Data.IsHuman;

        public event Action<Player> OnPlayersTurn;

        public void NotifyTurnStarted() => OnPlayersTurn?.Invoke(this);
    }
}
