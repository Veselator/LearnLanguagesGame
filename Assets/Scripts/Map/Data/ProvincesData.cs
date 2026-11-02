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
        [Tooltip("Twin province of every province on a mirrored map; empty for ordinary maps.")]
        [SerializeField] private int[] _mirrors = Array.Empty<int>();

        public IReadOnlyList<ProvinceDataEntry> Entries => _entries;
        public IReadOnlyList<int> PossibleSpawnPoints => _possibleSpawnPoints;
        public IReadOnlyList<ColorAlias> ColorAliases => _colorAliases;
        public int Count => _entries.Length;
        public bool IsMirrored => _mirrors.Length == _entries.Length && _entries.Length > 0;

        public ProvinceDataEntry Get(int id) => _entries[id];

        /// <summary>Twin of the province on a mirrored map, the province itself otherwise.</summary>
        public int GetMirror(int id) => IsMirrored && _mirrors[id] >= 0 ? _mirrors[id] : id;

        public void SetEntries(ProvinceDataEntry[] entries) => _entries = entries ?? Array.Empty<ProvinceDataEntry>();

        public void SetColorAliases(ColorAlias[] aliases) => _colorAliases = aliases ?? Array.Empty<ColorAlias>();

        public void SetMirrors(int[] mirrors) => _mirrors = mirrors ?? Array.Empty<int>();

        public void SetSpawnPoints(int[] spawnPoints) => _possibleSpawnPoints = spawnPoints ?? Array.Empty<int>();
    }
}
