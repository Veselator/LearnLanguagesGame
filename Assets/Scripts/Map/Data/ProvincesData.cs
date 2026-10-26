using System;
using System.Collections.Generic;
using UnityEngine;

namespace LearnLanguages.Map.Data
{
    /// <summary>
    /// Extra image color belonging to a province (tiny anti-aliasing slivers merged into a neighbour).
    /// </summary>
    [Serializable]
    public struct ColorAlias
    {
        [SerializeField] private Color _color;
        [SerializeField] private int _province;

        public ColorAlias(Color color, int province)
        {
            _color = color;
            _province = province;
        }

        public Color Color => _color;
        public int Province => _province;
    }

    [CreateAssetMenu(menuName = "LearnLanguages/Map/Provinces Data", fileName = "ProvincesData")]
    public sealed class ProvincesData : ScriptableObject
    {
        [SerializeField] private ProvinceDataEntry[] _entries = Array.Empty<ProvinceDataEntry>();
        [Tooltip("Province ids where players may start. Each must be a land province with a settlement.")]
        [SerializeField] private int[] _possibleSpawnPoints = Array.Empty<int>();
        [SerializeField] private ColorAlias[] _colorAliases = Array.Empty<ColorAlias>();

        public IReadOnlyList<ProvinceDataEntry> Entries => _entries;
        public IReadOnlyList<int> PossibleSpawnPoints => _possibleSpawnPoints;
        public IReadOnlyList<ColorAlias> ColorAliases => _colorAliases;
        public int Count => _entries.Length;

        public ProvinceDataEntry Get(int id) => _entries[id];

        public void SetEntries(ProvinceDataEntry[] entries) => _entries = entries ?? Array.Empty<ProvinceDataEntry>();

        public void SetColorAliases(ColorAlias[] aliases) => _colorAliases = aliases ?? Array.Empty<ColorAlias>();

        public void SetSpawnPoints(int[] spawnPoints) => _possibleSpawnPoints = spawnPoints ?? Array.Empty<int>();
    }
}
