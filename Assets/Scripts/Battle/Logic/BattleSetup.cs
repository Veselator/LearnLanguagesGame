using System.Collections.Generic;
using LearnLanguages.Battle.Data;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Battle.Logic
{
    public enum BattleSideId
    {
        Attacker = 0,
        Defender = 1
    }

    public enum BattlePhase
    {
        Deployment = 0,
        Command = 1,
        Action = 2,
        Finished = 3
    }

    public static class BattleSideExtensions
    {
        public static BattleSideId Opposite(this BattleSideId side) =>
            side == BattleSideId.Attacker ? BattleSideId.Defender : BattleSideId.Attacker;
    }

    public sealed class BattleSide
    {
        public BattleSide(BattleSideId id, PlayerData player, IReadOnlyList<Army> armies, IReadOnlyList<Army> reinforcements)
        {
            Id = id;
            Player = player;
            Armies = armies;
            Reinforcements = reinforcements;
        }

        public BattleSideId Id { get; }
        public PlayerData Player { get; }
        public bool IsHuman => Player.IsHuman;
        public IReadOnlyList<Army> Armies { get; }
        public IReadOnlyList<Army> Reinforcements { get; }
    }

    public sealed class BattleSetup
    {
        public BattleSetup(BattleSide attacker, BattleSide defender, BattlefieldMapData battlefield, GameTile tile)
        {
            Attacker = attacker;
            Defender = defender;
            Battlefield = battlefield;
            Tile = tile;
        }

        public BattleSide Attacker { get; }
        public BattleSide Defender { get; }
        public BattlefieldMapData Battlefield { get; }
        public GameTile Tile { get; }
        public bool InvolvesHuman => Attacker.IsHuman || Defender.IsHuman;

        public BattleSide Get(BattleSideId id) => id == BattleSideId.Attacker ? Attacker : Defender;
    }

    public sealed class BattleResult
    {
        public BattleResult(BattleSetup setup, BattleSideId winner, int turns)
        {
            Setup = setup;
            Winner = winner;
            Turns = turns;
        }

        public BattleSetup Setup { get; }
        public BattleSideId Winner { get; }
        public int Turns { get; }
        public bool AttackerWon => Winner == BattleSideId.Attacker;
        public BattleSide WinnerSide => Setup.Get(Winner);
        public BattleSide LoserSide => Setup.Get(Winner.Opposite());
    }
}
