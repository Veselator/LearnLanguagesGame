using System.Collections.Generic;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Compact copy of the strategic situation used by minimax: tile owners and army strengths only.
    /// Battles are replaced by their expected outcome.
    /// </summary>
    public sealed class AIWorldState
    {
        public struct ArmyState
        {
            public int Id;
            public int Owner;
            public int Tile;
            public float Strength;
            public bool Alive;
        }

        private readonly int[] _owners;
        private readonly float[] _values;
        private readonly ArmyState[] _armies;

        public AIWorldState(int[] owners, float[] values, ArmyState[] armies)
        {
            _owners = owners;
            _values = values;
            _armies = armies;
        }

        public IReadOnlyList<ArmyState> Armies => _armies;
        public int GetOwner(int tile) => _owners[tile];

        public AIWorldState Clone() =>
            new AIWorldState((int[])_owners.Clone(), _values, (ArmyState[])_armies.Clone());

        public float StrengthAt(int tile, int owner)
        {
            var sum = 0f;
            foreach (var army in _armies)
                if (army.Alive && army.Tile == tile && army.Owner == owner) sum += army.Strength;
            return sum;
        }

        /// <summary>Moves army #index into a tile and applies the expected battle result.</summary>
        public void ApplyMove(int index, int targetTile, bool isLand)
        {
            var mover = _armies[index];
            if (!mover.Alive || mover.Tile == targetTile) return;

            var enemyStrength = 0f;
            var enemyOwner = int.MinValue;
            for (var i = 0; i < _armies.Length; i++)
            {
                var other = _armies[i];
                if (!other.Alive || other.Tile != targetTile || other.Owner == mover.Owner) continue;
                enemyStrength += other.Strength;
                enemyOwner = other.Owner;
            }

            if (enemyStrength <= 0f)
            {
                mover.Tile = targetTile;
                _armies[index] = mover;
                if (isLand) _owners[targetTile] = mover.Owner;
                return;
            }

            var a = mover.Strength * mover.Strength;
            var d = enemyStrength * enemyStrength * 1.15f; // defenders hold slightly better
            var attackerWins = a > d;
            if (attackerWins)
            {
                mover.Strength *= 1f - 0.6f * Mathf.Sqrt(d / a);
                mover.Tile = targetTile;
                _armies[index] = mover;
                for (var i = 0; i < _armies.Length; i++)
                {
                    if (_armies[i].Owner != enemyOwner || _armies[i].Tile != targetTile) continue;
                    var killed = _armies[i];
                    killed.Alive = false;
                    _armies[i] = killed;
                }
                if (isLand) _owners[targetTile] = mover.Owner;
            }
            else
            {
                var ratio = Mathf.Sqrt(a / Mathf.Max(0.0001f, d));
                mover.Strength *= 0.35f;
                _armies[index] = mover;
                for (var i = 0; i < _armies.Length; i++)
                {
                    if (_armies[i].Owner != enemyOwner || _armies[i].Tile != targetTile || !_armies[i].Alive) continue;
                    var hurt = _armies[i];
                    hurt.Strength *= 1f - 0.6f * ratio;
                    _armies[i] = hurt;
                }
            }
        }

        /// <summary>Strategic score of the state from the point of view of <paramref name="player"/>.</summary>
        /// <param name="goalDistance">Steps from each tile to the nearest tile worth conquering (-1 = unreachable).</param>
        public float Evaluate(int player, int capitalTile, IReadOnlyList<int> capitalNeighbours, int[] goalDistance)
        {
            var score = 0f;
            for (var tile = 0; tile < _owners.Length; tile++)
            {
                if (_owners[tile] == player) score += _values[tile];
                else if (_owners[tile] != PlayerData.NeutralId) score -= _values[tile] * 0.3f;
            }

            foreach (var army in _armies)
            {
                if (!army.Alive) continue;
                if (army.Owner == player)
                {
                    score += army.Strength * 0.08f;
                    // Long-term drive: armies far from anything to conquer are worth a little less.
                    if (goalDistance != null && goalDistance[army.Tile] > 0) score -= goalDistance[army.Tile] * 3f;
                }
                else if (army.Owner != PlayerData.NeutralId) score -= army.Strength * 0.04f;
            }

            if (capitalTile >= 0)
            {
                if (_owners[capitalTile] != player) return score - 10000f;
                var defence = StrengthAt(capitalTile, player);
                var threat = 0f;
                foreach (var neighbour in capitalNeighbours)
                foreach (var army in _armies)
                    if (army.Alive && army.Tile == neighbour && army.Owner != player && army.Owner != PlayerData.NeutralId)
                        threat += army.Strength;
                if (threat > defence * 1.1f) score -= (threat - defence) * 0.3f + 300f;
            }
            return score;
        }

        public static float TileValue(SettlementType settlement, bool isCapital, bool isOwnCapital, bool isLand)
        {
            if (!isLand) return 0f;
            var value = 5f;
            switch (settlement)
            {
                case SettlementType.Village: value += 25f; break;
                case SettlementType.Castle: value += 45f; break;
                case SettlementType.City: value += 75f; break;
            }
            if (isCapital) value += 150f;
            if (isOwnCapital) value += 1000f;
            return value;
        }
    }
}
