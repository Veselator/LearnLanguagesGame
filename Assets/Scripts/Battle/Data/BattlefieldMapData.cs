using System;
using UnityEngine;

namespace LearnLanguages.Battle.Data
{
    public enum BattleTileType
    {
        Grass = 0,
        Sand = 1,
        Dirt = 2,
        Planks = 3,
        Water = 4,
        Rocks = 5
    }

    /// <summary>
    /// Tile matrix of one battlefield: <see cref="Rows"/> lines by <see cref="Columns"/> cells. Row 0 is the farthest line.
    /// </summary>
    [CreateAssetMenu(menuName = "LearnLanguages/Battle/Battlefield Map", fileName = "BattlefieldMap")]
    public sealed class BattlefieldMapData : ScriptableObject
    {
        public const int Rows = 6;
        public const int Columns = 30;

        [SerializeField] private BattleTileType[] _tiles = new BattleTileType[Rows * Columns];

        public BattleTileType Get(int row, int column)
        {
            EnsureSize();
            return _tiles[row * Columns + column];
        }

        public void Set(int row, int column, BattleTileType type)
        {
            EnsureSize();
            _tiles[row * Columns + column] = type;
        }

        public void Fill(BattleTileType type)
        {
            EnsureSize();
            for (var i = 0; i < _tiles.Length; i++) _tiles[i] = type;
        }

        private void EnsureSize()
        {
            if (_tiles != null && _tiles.Length == Rows * Columns) return;
            var resized = new BattleTileType[Rows * Columns];
            if (_tiles != null) Array.Copy(_tiles, resized, Math.Min(_tiles.Length, resized.Length));
            _tiles = resized;
        }
    }
}
