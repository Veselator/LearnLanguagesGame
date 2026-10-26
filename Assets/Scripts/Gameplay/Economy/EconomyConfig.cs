using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Gameplay.Economy
{
    [CreateAssetMenu(menuName = "LearnLanguages/Economy/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Header("Start")]
        [SerializeField] private ResourceBundle _startingResources = new ResourceBundle(150, 60, 40, 30);

        [Header("Province yields")]
        [SerializeField] private ResourceBundle _wheatYield = new ResourceBundle(0, 3, 0, 0);
        [SerializeField] private ResourceBundle _vineyardYield = new ResourceBundle(2, 1, 0, 0);
        [SerializeField] private ResourceBundle _pastureYield = new ResourceBundle(1, 2, 0, 0);
        [SerializeField] private ResourceBundle _quarryYield = new ResourceBundle(0, 0, 0, 3);
        [SerializeField] private ResourceBundle _forestYield = new ResourceBundle(0, 0, 2, 0);
        [SerializeField] private ResourceBundle _emptyProvinceYield = new ResourceBundle(1, 0, 0, 0);

        [Header("Settlement income")]
        [SerializeField] private ResourceBundle _villageIncome = new ResourceBundle(2, 1, 0, 0);
        [SerializeField] private ResourceBundle _castleIncome = new ResourceBundle(3, 0, 1, 1);
        [SerializeField] private ResourceBundle _cityIncome = new ResourceBundle(6, 0, 0, 0);

        [Header("Armies")]
        [Tooltip("Share of wounded soldiers recovering per turn in the field.")]
        [SerializeField] private float _fieldHealing = 0.1f;
        [Tooltip("Share of wounded soldiers recovering per turn inside an own castle or city.")]
        [SerializeField] private float _settlementHealing = 0.3f;
        [SerializeField] private float _organizationRecovery = 0.15f;
        [Tooltip("Organization lost by every cohort when upkeep cannot be paid.")]
        [SerializeField] private float _unpaidUpkeepPenalty = 0.1f;

        public ResourceBundle StartingResources => _startingResources;
        public ResourceBundle EmptyProvinceYield => _emptyProvinceYield;
        public ResourceBundle ForestYield => _forestYield;
        public float FieldHealing => _fieldHealing;
        public float SettlementHealing => _settlementHealing;
        public float OrganizationRecovery => _organizationRecovery;
        public float UnpaidUpkeepPenalty => _unpaidUpkeepPenalty;

        public ResourceBundle GetProductionYield(ProductionType production)
        {
            switch (production)
            {
                case ProductionType.Wheat: return _wheatYield;
                case ProductionType.Vineyard: return _vineyardYield;
                case ProductionType.Pasture: return _pastureYield;
                case ProductionType.Quarry: return _quarryYield;
                default: return ResourceBundle.Zero;
            }
        }

        public ResourceBundle GetSettlementIncome(SettlementType settlement)
        {
            switch (settlement)
            {
                case SettlementType.Village: return _villageIncome;
                case SettlementType.Castle: return _castleIncome;
                case SettlementType.City: return _cityIncome;
                default: return ResourceBundle.Zero;
            }
        }
    }
}
