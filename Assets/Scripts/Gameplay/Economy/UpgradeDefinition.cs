using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Gameplay.Economy
{
    /// <summary>
    /// A building that can be constructed in a settlement.
    /// </summary>
    [CreateAssetMenu(menuName = "LearnLanguages/Economy/Upgrade", fileName = "Upgrade")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField] private string _descriptionKey;
        [SerializeField] private ResourceBundle _cost;
        [SerializeField] private SettlementMask _allowedSettlements = SettlementMask.Village | SettlementMask.Castle | SettlementMask.City;

        [Header("Production")]
        [SerializeField] private ResourceBundle _baseYield;
        [Tooltip("Province production that gives the bonus yield below. None = no requirement.")]
        [SerializeField] private ProductionType _matchingProduction;
        [SerializeField] private ResourceBundle _matchingBonusYield;
        [Tooltip("Required landscape (e.g. a sawmill needs a forest). Sea = any landscape.")]
        [SerializeField] private LandscapeType _requiredLandscape = LandscapeType.Sea;
        [Tooltip("Yield multiplier bonus for each owned neighbouring province with the same production.")]
        [SerializeField] private float _neighbourBonusPerProvince = 0.25f;

        [Header("Military")]
        [SerializeField] private bool _isPort;
        [SerializeField] private bool _unlocksAdvancedUnits;
        [Range(0, 90)] [SerializeField] private int _recruitDiscountPercent;
        [Tooltip("Extra share of wounded that recover each turn.")]
        [SerializeField] private float _healingBonus;
        [Tooltip("Organization restored each turn for armies standing here.")]
        [SerializeField] private float _moraleBonus;

        public string Id => _id;
        public string NameKey => _nameKey;
        public string DescriptionKey => _descriptionKey;
        public ResourceBundle Cost => _cost;
        public SettlementMask AllowedSettlements => _allowedSettlements;
        public ResourceBundle BaseYield => _baseYield;
        public ProductionType MatchingProduction => _matchingProduction;
        public ResourceBundle MatchingBonusYield => _matchingBonusYield;
        public LandscapeType RequiredLandscape => _requiredLandscape;
        public float NeighbourBonusPerProvince => _neighbourBonusPerProvince;
        public bool IsPort => _isPort;
        public bool UnlocksAdvancedUnits => _unlocksAdvancedUnits;
        public int RecruitDiscountPercent => _recruitDiscountPercent;
        public float HealingBonus => _healingBonus;
        public float MoraleBonus => _moraleBonus;
        public bool IsProducing => !_baseYield.IsZero || !_matchingBonusYield.IsZero;
    }
}
