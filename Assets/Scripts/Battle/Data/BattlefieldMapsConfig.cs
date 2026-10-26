using System;
using System.Collections.Generic;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Battle.Data
{
    [Serializable]
    public struct MapPossibility
    {
        [SerializeField] private BattlefieldMapData _battlefieldMapData;
        [Tooltip("Relative weight, must be greater than zero.")]
        [Min(0.0001f)] [SerializeField] private float _probability;

        public MapPossibility(BattlefieldMapData data, float probability)
        {
            _battlefieldMapData = data;
            _probability = probability;
        }

        public BattlefieldMapData BattlefieldMapData => _battlefieldMapData;
        public float Probability => _probability;
    }

    [Serializable]
    public struct LandscapeBattlefieldEntry
    {
        [SerializeField] private LandscapeType _targetLandscape;
        [SerializeField] private MapPossibility[] _possibilities;

        public LandscapeBattlefieldEntry(LandscapeType landscape, MapPossibility[] possibilities)
        {
            _targetLandscape = landscape;
            _possibilities = possibilities;
        }

        public LandscapeType TargetLandscape => _targetLandscape;
        public IReadOnlyList<MapPossibility> Possibilities => _possibilities ?? Array.Empty<MapPossibility>();
    }

    /// <summary>
    /// Which battlefield appears with which probability for each landscape.
    /// </summary>
    [CreateAssetMenu(menuName = "LearnLanguages/Battle/Battlefield Maps Config", fileName = "BattlefieldMapsConfig")]
    public sealed class BattlefieldMapsConfig : ScriptableObject
    {
        [SerializeField] private LandscapeBattlefieldEntry[] _entries = Array.Empty<LandscapeBattlefieldEntry>();
        [SerializeField] private BattlefieldMapData _fallback;

        private List<BattlefieldMapData> _allMaps;

        /// <summary>Every battlefield referenced by this config, in a stable order (used to persist choices).</summary>
        public IReadOnlyList<BattlefieldMapData> AllMaps
        {
            get
            {
                if (_allMaps != null) return _allMaps;
                _allMaps = new List<BattlefieldMapData>();
                if (_fallback != null) _allMaps.Add(_fallback);
                foreach (var entry in _entries)
                foreach (var possibility in entry.Possibilities)
                    if (possibility.BattlefieldMapData != null && !_allMaps.Contains(possibility.BattlefieldMapData))
                        _allMaps.Add(possibility.BattlefieldMapData);
                return _allMaps;
            }
        }

        public BattlefieldMapData Pick(LandscapeType landscape, IRandomService random)
        {
            foreach (var entry in _entries)
            {
                if (entry.TargetLandscape != landscape) continue;

                var total = 0f;
                foreach (var possibility in entry.Possibilities)
                    if (possibility.BattlefieldMapData != null) total += Mathf.Max(0f, possibility.Probability);
                if (total <= 0f) break;

                var roll = random.Range(0f, total);
                foreach (var possibility in entry.Possibilities)
                {
                    if (possibility.BattlefieldMapData == null) continue;
                    roll -= Mathf.Max(0f, possibility.Probability);
                    if (roll <= 0f) return possibility.BattlefieldMapData;
                }
            }
            return _fallback != null ? _fallback : AllMaps.Count > 0 ? AllMaps[0] : null;
        }

        public void SetContent(LandscapeBattlefieldEntry[] entries, BattlefieldMapData fallback)
        {
            _entries = entries;
            _fallback = fallback;
            _allMaps = null;
        }

        private void OnValidate() => _allMaps = null;
    }
}
