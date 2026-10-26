using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.Events
{
    public readonly struct GameReadyEvent
    {
    }

    public readonly struct TurnStartedEvent
    {
        public TurnStartedEvent(PlayerData player, int round)
        {
            Player = player;
            Round = round;
        }

        public PlayerData Player { get; }
        public int Round { get; }
    }

    public readonly struct ResourcesChangedEvent
    {
        public ResourcesChangedEvent(PlayerData player) => Player = player;
        public PlayerData Player { get; }
    }

    public readonly struct TileOwnerChangedEvent
    {
        public TileOwnerChangedEvent(GameTile tile, int previousOwnerId)
        {
            Tile = tile;
            PreviousOwnerId = previousOwnerId;
        }

        public GameTile Tile { get; }
        public int PreviousOwnerId { get; }
    }

    public readonly struct TileUpgradedEvent
    {
        public TileUpgradedEvent(GameTile tile, UpgradeDefinition upgrade)
        {
            Tile = tile;
            Upgrade = upgrade;
        }

        public GameTile Tile { get; }
        public UpgradeDefinition Upgrade { get; }
    }

    public readonly struct ArmyCreatedEvent
    {
        public ArmyCreatedEvent(Army army) => Army = army;
        public Army Army { get; }
    }

    public readonly struct ArmyMovedEvent
    {
        public ArmyMovedEvent(Army army, int fromTileId)
        {
            Army = army;
            FromTileId = fromTileId;
        }

        public Army Army { get; }
        public int FromTileId { get; }
    }

    public readonly struct ArmyChangedEvent
    {
        public ArmyChangedEvent(Army army) => Army = army;
        public Army Army { get; }
    }

    public readonly struct ArmyDestroyedEvent
    {
        public ArmyDestroyedEvent(Army army) => Army = army;
        public Army Army { get; }
    }

    public readonly struct PlayerEliminatedEvent
    {
        public PlayerEliminatedEvent(PlayerData player, PlayerData conqueror)
        {
            Player = player;
            Conqueror = conqueror;
        }

        public PlayerData Player { get; }
        public PlayerData Conqueror { get; }
    }

    public readonly struct GameOverEvent
    {
        public GameOverEvent(PlayerData winner, bool humanWon)
        {
            Winner = winner;
            HumanWon = humanWon;
        }

        public PlayerData Winner { get; }
        public bool HumanWon { get; }
    }

    /// <summary>Text shown to the player as a toast. Already localized.</summary>
    public readonly struct NotificationEvent
    {
        public NotificationEvent(string text) => Text = text;
        public string Text { get; }
    }

    public readonly struct SelectionChangedEvent
    {
        public SelectionChangedEvent(GameTile tile, Army army)
        {
            Tile = tile;
            Army = army;
        }

        public GameTile Tile { get; }
        public Army Army { get; }
    }
}
