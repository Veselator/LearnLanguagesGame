using System;
using System.Collections.Generic;
using LearnLanguages.Map.Data;

namespace LearnLanguages.Map.Logic
{
    public interface IGameMap
    {
        int Count { get; }
        IReadOnlyList<GameTile> Tiles { get; }
        GameTile Get(int id);
        IReadOnlyList<int> GetNeighbours(int id);
        bool AreNeighbours(int a, int b);
        int[] GetDistances(int from, Func<GameTile, GameTile, bool> canPass);
        List<int> FindPath(int from, int to, Func<GameTile, GameTile, bool> canPass);
    }

    /// <summary>
    /// Graph of provinces with breadth-first search.
    /// </summary>
    public sealed class GameMap : IGameMap
    {
        private GameTile[] _tiles = Array.Empty<GameTile>();
        private readonly Queue<int> _queue = new Queue<int>();

        public int Count => _tiles.Length;
        public IReadOnlyList<GameTile> Tiles => _tiles;

        public void Build(ProvincesData provinces)
        {
            _tiles = new GameTile[provinces.Count];
            for (var i = 0; i < provinces.Count; i++) _tiles[i] = new GameTile(i, provinces.Get(i));
        }

        public GameTile Get(int id) => id >= 0 && id < _tiles.Length ? _tiles[id] : null;

        public IReadOnlyList<int> GetNeighbours(int id) => _tiles[id].Data.Neighbours;

        public bool AreNeighbours(int a, int b)
        {
            foreach (var neighbour in GetNeighbours(a))
                if (neighbour == b) return true;
            return false;
        }

        /// <summary>Number of steps from <paramref name="from"/> to every tile, -1 if unreachable.</summary>
        public int[] GetDistances(int from, Func<GameTile, GameTile, bool> canPass)
        {
            var distances = new int[_tiles.Length];
            for (var i = 0; i < distances.Length; i++) distances[i] = -1;
            distances[from] = 0;
            _queue.Clear();
            _queue.Enqueue(from);
            while (_queue.Count > 0)
            {
                var current = _queue.Dequeue();
                foreach (var next in GetNeighbours(current))
                {
                    if (distances[next] >= 0 || !canPass(_tiles[current], _tiles[next])) continue;
                    distances[next] = distances[current] + 1;
                    _queue.Enqueue(next);
                }
            }
            return distances;
        }

        public List<int> FindPath(int from, int to, Func<GameTile, GameTile, bool> canPass)
        {
            var previous = new int[_tiles.Length];
            for (var i = 0; i < previous.Length; i++) previous[i] = -2;
            previous[from] = -1;
            _queue.Clear();
            _queue.Enqueue(from);
            while (_queue.Count > 0)
            {
                var current = _queue.Dequeue();
                if (current == to) break;
                foreach (var next in GetNeighbours(current))
                {
                    if (previous[next] != -2 || !canPass(_tiles[current], _tiles[next])) continue;
                    previous[next] = current;
                    _queue.Enqueue(next);
                }
            }

            if (previous[to] == -2) return null;
            var path = new List<int>();
            for (var node = to; node != -1; node = previous[node]) path.Add(node);
            path.Reverse();
            return path;
        }
    }
}
