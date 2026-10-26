using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Map.Data;
using UnityEngine;

namespace LearnLanguages.Gameplay.Units
{
    public enum UnitClass
    {
        Swordsmen = 0,
        Archers = 1,
        Cavalry = 2,
        Commander = 3
    }

    [CreateAssetMenu(menuName = "LearnLanguages/Units/Unit Type", fileName = "UnitType")]
    public sealed class UnitTypeDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _nameKey;
        [SerializeField] private UnitClass _class;

        [Header("Stats")]
        [SerializeField] private int _maxSoldiers = 100;
        [SerializeField] private float _attack = 10f;
        [SerializeField] private float _defense = 10f;
        [Tooltip("Battle cells per turn.")]
        [SerializeField] private int _speed = 3;
        [Tooltip("Shooting range in cells, 0 for melee only.")]
        [SerializeField] private int _range;
        [SerializeField] private float _rangedAttack;
        [Tooltip("Attack multiplier bonus when engaging right after moving.")]
        [SerializeField] private float _chargeBonus;

        [Header("Economy")]
        [SerializeField] private bool _recruitable = true;
        [SerializeField] private SettlementType _minimumSettlement = SettlementType.Village;
        [SerializeField] private bool _requiresAdvancedBuilding;
        [SerializeField] private ResourceBundle _recruitCost;
        [SerializeField] private ResourceBundle _upkeep;
        [Tooltip("Gold per 10 soldiers when replenishing the killed.")]
        [SerializeField] private int _reinforceGoldPerTen = 3;

        public string Id => _id;
        public string NameKey => _nameKey;
        public UnitClass Class => _class;
        public int MaxSoldiers => _maxSoldiers;
        public float Attack => _attack;
        public float Defense => _defense;
        public int Speed => _speed;
        public int Range => _range;
        public float RangedAttack => _rangedAttack;
        public float ChargeBonus => _chargeBonus;
        public bool Recruitable => _recruitable;
        public SettlementType MinimumSettlement => _minimumSettlement;
        public bool RequiresAdvancedBuilding => _requiresAdvancedBuilding;
        public ResourceBundle RecruitCost => _recruitCost;
        public ResourceBundle Upkeep => _upkeep;
        public int ReinforceGoldPerTen => _reinforceGoldPerTen;
        public bool IsRanged => _range > 0;

        /// <summary>Rough combat value of a full-strength cohort, used by the AI and auto-resolve heuristics.</summary>
        public float BaseStrength => _maxSoldiers * (_attack + _defense + _rangedAttack * 0.6f) * 0.05f;
    }
}
