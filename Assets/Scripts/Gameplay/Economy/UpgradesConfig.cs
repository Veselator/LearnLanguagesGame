using System;
using System.Collections.Generic;
using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Gameplay.Economy
{
    [CreateAssetMenu(menuName = "LearnLanguages/Economy/Upgrades Config", fileName = "UpgradesConfig")]
    public sealed class UpgradesConfig : ScriptableObject
    {
        [SerializeField] private UpgradeDefinition[] _upgrades = Array.Empty<UpgradeDefinition>();
        [SerializeField] private int _villageSlots = 2;
        [SerializeField] private int _castleSlots = 3;
        [SerializeField] private int _citySlots = 5;

        public IReadOnlyList<UpgradeDefinition> Upgrades => _upgrades;

        public UpgradeDefinition GetById(string id)
        {
            foreach (var upgrade in _upgrades)
                if (upgrade != null && upgrade.Id == id) return upgrade;
            return null;
        }

        public int GetSlots(SettlementType settlement)
        {
            switch (settlement)
            {
                case SettlementType.Village: return _villageSlots;
                case SettlementType.Castle: return _castleSlots;
                case SettlementType.City: return _citySlots;
                default: return 0;
            }
        }
    }
}
