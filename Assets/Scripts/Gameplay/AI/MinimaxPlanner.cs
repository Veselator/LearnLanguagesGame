using System.Collections.Generic;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Chooses the destination of one army with minimax and alpha-beta pruning:
    /// our ply moves the army, the opponent ply moves the most dangerous nearby enemy army.
    /// </summary>
    public sealed class MinimaxPlanner
    {
        private const int ThreatRadius = 2;

        private readonly AIContext _context;
        private readonly int _player;
        private readonly int _capital;
        private readonly IReadOnlyList<int> _capitalNeighbours;
        private readonly int[] _goalDistance;
        private readonly int[] _distanceFromArmy;
        private readonly float _noise;

        public MinimaxPlanner(AIContext context, int[] goalDistance, float noise)
        {
            _context = context;
            _player = context.Player.Id;
            _capital = context.Player.CapitalTileId;
            _capitalNeighbours = _capital >= 0 ? context.Map.GetNeighbours(_capital) : new int[0];
            _goalDistance = goalDistance;
            _noise = noise;
            _distanceFromArmy = new int[context.Map.Count];
        }

        public int ChooseDestination(AIWorldState state, int armyIndex, int depth)
        {
            var army = state.Armies[armyIndex];
            var bestTile = army.Tile;
            var bestScore = float.NegativeInfinity;
            var alpha = float.NegativeInfinity;

            foreach (var tile in GetMoves(state, armyIndex))
            {
                var next = state.Clone();
                next.ApplyMove(armyIndex, tile, _context.Map.Get(tile).IsLand);
                var score = Search(next, depth - 1, false, armyIndex, alpha, float.PositiveInfinity);
                if (_noise > 0f) score += _context.Random.Range(-_noise, _noise);
                if (score <= bestScore) continue;
                bestScore = score;
                bestTile = tile;
                if (score > alpha) alpha = score;
            }
            return bestTile;
        }

        private float Search(AIWorldState state, int depth, bool ourPly, int armyIndex, float alpha, float beta)
        {
            if (depth <= 0) return Evaluate(state);

            if (ourPly)
            {
                var best = float.NegativeInfinity;
                foreach (var tile in GetMoves(state, armyIndex))
                {
                    var next = state.Clone();
                    next.ApplyMove(armyIndex, tile, _context.Map.Get(tile).IsLand);
                    var value = Search(next, depth - 1, false, armyIndex, alpha, beta);
                    if (value > best) best = value;
                    if (best > alpha) alpha = best;
                    if (beta <= alpha) break;
                }
                return best;
            }

            var threats = GetThreats(state, armyIndex);
            if (threats.Count == 0) return Search(state, depth - 1, true, armyIndex, alpha, beta);

            var worst = float.PositiveInfinity;
            foreach (var enemyIndex in threats)
            {
                foreach (var tile in GetMoves(state, enemyIndex))
                {
                    var next = state.Clone();
                    next.ApplyMove(enemyIndex, tile, _context.Map.Get(tile).IsLand);
                    var value = Search(next, depth - 1, true, armyIndex, alpha, beta);
                    if (value < worst) worst = value;
                    if (worst < beta) beta = worst;
                    if (beta <= alpha) return worst;
                }
            }
            return worst;
        }

        private float Evaluate(AIWorldState state) =>
            state.Evaluate(_player, _capital, _capitalNeighbours, _goalDistance);

        private List<int> GetMoves(AIWorldState state, int armyIndex)
        {
            var army = state.Armies[armyIndex];
            var moves = new List<int> { army.Tile };
            if (!army.Alive) return moves;

            var from = _context.Map.Get(army.Tile);
            foreach (var id in from.Data.Neighbours)
            {
                var to = _context.Map.Get(id);
                if (_context.Movement.CanStep(army.Owner, from, to)) moves.Add(id);
            }
            return moves;
        }

        /// <summary>Enemy (non-neutral) armies close to our army or our capital.</summary>
        private List<int> GetThreats(AIWorldState state, int armyIndex)
        {
            var result = new List<int>();
            var origin = state.Armies[armyIndex].Tile;
            FillDistances(origin);

            for (var i = 0; i < state.Armies.Count; i++)
            {
                var other = state.Armies[i];
                if (!other.Alive || other.Owner == _player || other.Owner == PlayerData.NeutralId) continue;
                var near = _distanceFromArmy[other.Tile] >= 0 && _distanceFromArmy[other.Tile] <= ThreatRadius;
                var nearCapital = _capital >= 0 && (other.Tile == _capital || Contains(_capitalNeighbours, other.Tile));
                if (near || nearCapital) result.Add(i);
            }
            return result;
        }

        private void FillDistances(int origin)
        {
            for (var i = 0; i < _distanceFromArmy.Length; i++) _distanceFromArmy[i] = -1;
            _distanceFromArmy[origin] = 0;
            var frontier = new Queue<int>();
            frontier.Enqueue(origin);
            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (_distanceFromArmy[current] >= ThreatRadius) continue;
                foreach (var next in _context.Map.GetNeighbours(current))
                {
                    if (_distanceFromArmy[next] >= 0) continue;
                    _distanceFromArmy[next] = _distanceFromArmy[current] + 1;
                    frontier.Enqueue(next);
                }
            }
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            foreach (var item in list)
                if (item == value) return true;
            return false;
        }
    }
}
