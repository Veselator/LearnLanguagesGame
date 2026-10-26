using System;
using System.Collections.Generic;
using UnityEngine;

namespace LearnLanguages.Map.Data
{
    /// <summary>
    /// Static description of a province. Its id is the index in <see cref="ProvincesData"/>.
    /// Local positions are in pixels of the map images, origin at the bottom-left corner (Unity convention).
    /// </summary>
    [Serializable]
    public sealed class ProvinceDataEntry
    {
        [SerializeField] private string _name;
        [SerializeField] private Color _mapColor;
        [SerializeField] private ProvinceType _type;
        [SerializeField] private bool _impassable;
        [SerializeField] private LandscapeType _landscape;
        [SerializeField] private SettlementType _settlement;
        [SerializeField] private ProductionType _production;
        [SerializeField] private Vector2 _settlementLocalPos;
        [SerializeField] private Vector2 _portLocalPos;
        [SerializeField] private Vector2 _localUnitPosition;
        [SerializeField] private int[] _neighbours = Array.Empty<int>();

        public ProvinceDataEntry(Color mapColor)
        {
            _mapColor = mapColor;
        }

        public string Name => _name;
        public Color MapColor => _mapColor;
        public ProvinceType Type => _type;
        public bool IsImpassable => _impassable;
        public LandscapeType Landscape => _type == ProvinceType.Sea ? LandscapeType.Sea : _landscape;
        public SettlementType Settlement => _type == ProvinceType.Sea ? SettlementType.None : _settlement;
        public ProductionType Production => _type == ProvinceType.Sea ? ProductionType.None : _production;
        public Vector2 SettlementLocalPos => _settlementLocalPos;
        public Vector2 PortLocalPos => _portLocalPos;
        public Vector2 LocalUnitPosition => _localUnitPosition;
        public IReadOnlyList<int> Neighbours => _neighbours;

        public void SetName(string value) => _name = value;
        public void SetMapColor(Color value) => _mapColor = value;
        public void SetType(ProvinceType value) => _type = value;
        public void SetImpassable(bool value) => _impassable = value;
        public void SetLandscape(LandscapeType value) => _landscape = value;
        public void SetSettlement(SettlementType value) => _settlement = value;
        public void SetProduction(ProductionType value) => _production = value;
        public void SetSettlementLocalPos(Vector2 value) => _settlementLocalPos = value;
        public void SetPortLocalPos(Vector2 value) => _portLocalPos = value;
        public void SetLocalUnitPosition(Vector2 value) => _localUnitPosition = value;
        public void SetNeighbours(int[] value) => _neighbours = value ?? Array.Empty<int>();
    }
}
