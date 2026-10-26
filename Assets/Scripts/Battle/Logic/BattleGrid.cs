using System;
using System.Collections.Generic;
using LearnLanguages.Battle.Data;
using UnityEngine;

namespace LearnLanguages.Battle.Logic
{
    /// <summary>
    /// Terrain of the battlefield and grid path finding. Cells are (column, row).
    /// </summary>
    public sealed class BattleGrid
    {
        public const int Rows = BattlefieldMapData.Rows;
        public const int Columns = BattlefieldMapData.Columns;

        private static readonly Vector2Int[] Directions =
            { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private readonly BattlefieldMapData _map;
        private readonly BattleConfig _config;

        public BattleGrid(BattlefieldMapData map, BattleConfig config)
        {
            _map = map;
            _config = config;
        }

        public static IReadOnlyList<Vector2Int> OrthogonalDirections => Directions;

        public bool IsInside(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Columns && cell.y < Rows;

        public BattleTileType GetTile(Vector2Int cell) => _map.Get(cell.y, cell.x);

        public bool IsPassable(Vector2Int cell) => IsInside(cell) && _config.GetRules(GetTile(cell)).Passable;

        public int GetMoveCost(Vector2Int cell) => _config.GetRules(GetTile(cell)).MoveCost;

        public float GetDefenseMultiplier(Vector2Int cell) =>
            IsInside(cell) ? _config.GetRules(GetTile(cell)).DefenseMultiplier : 1f;

        public static int Distance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        public static bool AreAdjacent(Vector2Int a, Vector2Int b) => Distance(a, b) == 1;

        /// <summary>Movement costs of every cell reachable within <paramref name="budget"/>.</summary>
        public Dictionary<Vector2Int, int> GetReachable(Vector2Int from, int budget, Func<Vector2Int, bool> isBlocked)
        {
            var costs = new Dictionary<Vector2Int, int> { [from] = 0 };
            var frontier = new List<Vector2Int> { from };
            while (frontier.Count > 0)
            {
                var bestIndex = 0;
                for (var i = 1; i < frontier.Count; i++)
                    if (costs[frontier[i]] < costs[frontier[bestIndex]]) bestIndex = i;
                var current = frontier[bestIndex];
                frontier.RemoveAt(bestIndex);

                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    if (!IsPassable(next) || isBlocked(next)) continue;
                    var cost = costs[current] + GetMoveCost(next);
                    if (cost > budget || (costs.TryGetValue(next, out var known) && known <= cost)) continue;
                    costs[next] = cost;
                    frontier.Add(next);
                }
            }
            return costs;
        }

        /// <summary>
        /// Cheapest path (without the start cell). <paramref name="goal"/> may be blocked; it is then entered last as an attack.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int from, Vector2Int goal, int budget, Func<Vector2Int, bool> isBlocked)
        {
            if (!IsPassable(goal)) return null;
            var costs = new Dictionary<Vector2Int, int> { [from] = 0 };
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            var frontier = new List<Vector2Int> { from };
            while (frontier.Count > 0)
            {
                var bestIndex = 0;
                for (var i = 1; i < frontier.Count; i++)
                    if (costs[frontier[i]] < costs[frontier[bestIndex]]) bestIndex = i;
                var current = frontier[bestIndex];
                frontier.RemoveAt(bestIndex);
                if (current == goal) break;

                foreach (var direction in Directions)
                {
                    var next = current + direction;
                    if (!IsPassable(next) || (next != goal && isBlocked(next))) continue;
                    var cost = costs[current] + GetMoveCost(next);
                    if (cost > budget || (costs.TryGetValue(next, out var known) && known <= cost)) continue;
                    costs[next] = cost;
                    previous[next] = current;
                    frontier.Add(next);
                }
            }

            if (!previous.ContainsKey(goal)) return null;
            var path = new List<Vector2Int>();
            for (var cell = goal; cell != from; cell = previous[cell]) path.Add(cell);
            path.Reverse();
            return path;
        }
    }
}
