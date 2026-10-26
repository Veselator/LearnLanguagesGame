using System;
using UnityEngine;

namespace LearnLanguages.Gameplay.Economy
{
    public enum ResourceType
    {
        Gold = 0,
        Food = 1,
        Wood = 2,
        Stone = 3
    }

    /// <summary>
    /// Immutable amount of the four resources.
    /// </summary>
    [Serializable]
    public struct ResourceBundle : IEquatable<ResourceBundle>
    {
        [SerializeField] private int _gold;
        [SerializeField] private int _food;
        [SerializeField] private int _wood;
        [SerializeField] private int _stone;

        public ResourceBundle(int gold, int food, int wood, int stone)
        {
            _gold = gold;
            _food = food;
            _wood = wood;
            _stone = stone;
        }

        public static ResourceBundle Zero => default;

        public int Gold => _gold;
        public int Food => _food;
        public int Wood => _wood;
        public int Stone => _stone;
        public bool IsZero => _gold == 0 && _food == 0 && _wood == 0 && _stone == 0;

        public int Get(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Gold: return _gold;
                case ResourceType.Food: return _food;
                case ResourceType.Wood: return _wood;
                default: return _stone;
            }
        }

        public bool Covers(ResourceBundle cost) =>
            _gold >= cost._gold && _food >= cost._food && _wood >= cost._wood && _stone >= cost._stone;

        public ResourceBundle ClampMinZero() =>
            new ResourceBundle(Mathf.Max(0, _gold), Mathf.Max(0, _food), Mathf.Max(0, _wood), Mathf.Max(0, _stone));

        public ResourceBundle Scale(float factor) =>
            new ResourceBundle(Mathf.RoundToInt(_gold * factor), Mathf.RoundToInt(_food * factor),
                Mathf.RoundToInt(_wood * factor), Mathf.RoundToInt(_stone * factor));

        public static ResourceBundle operator +(ResourceBundle a, ResourceBundle b) =>
            new ResourceBundle(a._gold + b._gold, a._food + b._food, a._wood + b._wood, a._stone + b._stone);

        public static ResourceBundle operator -(ResourceBundle a, ResourceBundle b) =>
            new ResourceBundle(a._gold - b._gold, a._food - b._food, a._wood - b._wood, a._stone - b._stone);

        public static ResourceBundle operator *(ResourceBundle a, int factor) =>
            new ResourceBundle(a._gold * factor, a._food * factor, a._wood * factor, a._stone * factor);

        public bool Equals(ResourceBundle other) =>
            _gold == other._gold && _food == other._food && _wood == other._wood && _stone == other._stone;

        public override bool Equals(object obj) => obj is ResourceBundle other && Equals(other);

        public override int GetHashCode() => (_gold, _food, _wood, _stone).GetHashCode();

        /// <summary>Single number used by the AI to compare bundles.</summary>
        public float Value => _gold + _food * 0.8f + _wood * 0.9f + _stone * 1.1f;
    }
}
