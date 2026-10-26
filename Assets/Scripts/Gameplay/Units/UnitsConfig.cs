using System;
using System.Collections.Generic;
using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Gameplay.Units
{
    [CreateAssetMenu(menuName = "LearnLanguages/Units/Units Config", fileName = "UnitsConfig")]
    public sealed class UnitsConfig : ScriptableObject
    {
        public const int MaxCohortsPerArmy = 10;

        [SerializeField] private UnitTypeDefinition[] _units = Array.Empty<UnitTypeDefinition>();
        [SerializeField] private UnitTypeDefinition _commander;
        [Tooltip("Cohorts (besides the commander) every player starts with.")]
        [SerializeField] private UnitTypeDefinition[] _startingArmy = Array.Empty<UnitTypeDefinition>();
        [SerializeField] private UnitTypeDefinition[] _villageGarrison = Array.Empty<UnitTypeDefinition>();
        [SerializeField] private UnitTypeDefinition[] _castleGarrison = Array.Empty<UnitTypeDefinition>();
        [SerializeField] private UnitTypeDefinition[] _cityGarrison = Array.Empty<UnitTypeDefinition>();
        [Tooltip("Strength of neutral garrisons relative to a full cohort.")]
        [Range(0.1f, 1f)] [SerializeField] private float _garrisonStrength = 0.6f;

        public IReadOnlyList<UnitTypeDefinition> Units => _units;
        public UnitTypeDefinition Commander => _commander;
        public IReadOnlyList<UnitTypeDefinition> StartingArmy => _startingArmy;
        public float GarrisonStrength => _garrisonStrength;

        public UnitTypeDefinition GetById(string id)
        {
            if (_commander != null && _commander.Id == id) return _commander;
            foreach (var unit in _units)
                if (unit != null && unit.Id == id) return unit;
            return null;
        }

        public IReadOnlyList<UnitTypeDefinition> GetGarrison(SettlementType settlement)
        {
            switch (settlement)
            {
                case SettlementType.Village: return _villageGarrison;
                case SettlementType.Castle: return _castleGarrison;
                case SettlementType.City: return _cityGarrison;
                default: return Array.Empty<UnitTypeDefinition>();
            }
        }
    }
}
