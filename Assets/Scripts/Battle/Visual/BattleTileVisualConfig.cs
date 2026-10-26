using System;
using LearnLanguages.Battle.Data;
using UnityEngine;

namespace LearnLanguages.Battle.Visual
{
    [Serializable]
    public struct BattleTileVisual
    {
        [SerializeField] private BattleTileType _type;
        [SerializeField] private Sprite _ground;
        [Tooltip("Optional object drawn on top of the ground (rocks).")]
        [SerializeField] private Sprite _decoration;
        [SerializeField] private Color _gridColor;

        public BattleTileType Type => _type;
        public Sprite Ground => _ground;
        public Sprite Decoration => _decoration;
        public Color GridColor => _gridColor;
    }

    [CreateAssetMenu(menuName = "LearnLanguages/Battle/Battle Tile Visuals", fileName = "BattleTileVisualConfig")]
    public sealed class BattleTileVisualConfig : ScriptableObject
    {
        [SerializeField] private BattleTileVisual[] _visuals = Array.Empty<BattleTileVisual>();

        public BattleTileVisual Get(BattleTileType type)
        {
            foreach (var visual in _visuals)
                if (visual.Type == type) return visual;
            return _visuals.Length > 0 ? _visuals[0] : default;
        }
    }
}
