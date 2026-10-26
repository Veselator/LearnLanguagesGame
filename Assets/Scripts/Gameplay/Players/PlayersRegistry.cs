using System.Collections.Generic;

namespace LearnLanguages.Gameplay.Players
{
    public interface IPlayersRegistry
    {
        IReadOnlyList<Player> Players { get; }
        PlayerData Neutral { get; }
        Player HumanPlayer { get; }
        PlayerData Get(int id);
        Player GetPlayer(int id);
        int AliveCount { get; }
    }

    public sealed class PlayersRegistry : IPlayersRegistry
    {
        private readonly List<Player> _players = new List<Player>();
        private readonly Dictionary<int, Player> _byId = new Dictionary<int, Player>();

        public IReadOnlyList<Player> Players => _players;
        public PlayerData Neutral { get; private set; }
        public Player HumanPlayer { get; private set; }

        public int AliveCount
        {
            get
            {
                var count = 0;
                foreach (var player in _players)
                    if (player.Data.IsAlive) count++;
                return count;
            }
        }

        public void Initialize(IEnumerable<PlayerData> players, PlayerData neutral)
        {
            _players.Clear();
            _byId.Clear();
            Neutral = neutral;
            HumanPlayer = null;
            foreach (var data in players)
            {
                var player = new Player(data);
                _players.Add(player);
                _byId.Add(data.Id, player);
                if (data.IsHuman && HumanPlayer == null) HumanPlayer = player;
            }
        }

        public PlayerData Get(int id) => id == PlayerData.NeutralId ? Neutral : _byId.TryGetValue(id, out var p) ? p.Data : Neutral;

        public Player GetPlayer(int id) => _byId.TryGetValue(id, out var player) ? player : null;
    }
}
