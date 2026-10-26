using System;
using UnityEngine;

namespace LearnLanguages.Battle.Data
{
    [Serializable]
    public struct BattleTileRules
    {
        [SerializeField] private BattleTileType _type;
        [SerializeField] private bool _passable;
        [SerializeField] private int _moveCost;
        [SerializeField] private float _defenseMultiplier;

        public BattleTileRules(BattleTileType type, bool passable, int moveCost, float defenseMultiplier)
        {
            _type = type;
            _passable = passable;
            _moveCost = moveCost;
            _defenseMultiplier = defenseMultiplier;
        }

        public BattleTileType Type => _type;
        public bool Passable => _passable;
        public int MoveCost => Mathf.Max(1, _moveCost);
        public float DefenseMultiplier => _defenseMultiplier <= 0f ? 1f : _defenseMultiplier;
    }

    [CreateAssetMenu(menuName = "LearnLanguages/Battle/Battle Config", fileName = "BattleConfig")]
    public sealed class BattleConfig : ScriptableObject
    {
        [Header("Flow")]
        [SerializeField] private int _ticksPerTurn = 5;
        [SerializeField] private int _deploymentColumns = 3;
        [SerializeField] private int _reinforcementTurn = 5;
        [SerializeField] private int _maxTurns = 14;
        [SerializeField] private float _tickDuration = 0.7f;

        [Header("Combat")]
        [SerializeField] private float _damageFactor = 0.06f;
        [SerializeField] private float _rangedDamageFactor = 0.05f;
        [Range(0f, 1f)] [SerializeField] private float _killedShare = 0.4f;
        [SerializeField] private float _organizationLossFactor = 1.3f;
        [SerializeField] private float _organizationLossPerFight = 0.02f;
        [SerializeField] private float _routThreshold = 0.15f;
        [SerializeField] private float _armyDefeatMorale = 0.2f;
        [SerializeField] private float _defendMultiplier = 1.5f;
        [SerializeField] private float _duelSuccessMultiplier = 1.35f;
        [SerializeField] private float _duelFailMultiplier = 0.75f;
        [SerializeField] private float _shoutSuccessBonus = 0.25f;
        [SerializeField] private float _shoutFailBonus = 0.08f;
        [SerializeField] private int _shootRowSpread = 2;

        [Header("Terrain")]
        [SerializeField] private BattleTileRules[] _tileRules =
        {
            new BattleTileRules(BattleTileType.Grass, true, 1, 1f),
            new BattleTileRules(BattleTileType.Sand, true, 2, 0.9f),
            new BattleTileRules(BattleTileType.Dirt, true, 2, 1.25f),
            new BattleTileRules(BattleTileType.Planks, true, 1, 1f),
            new BattleTileRules(BattleTileType.Water, false, 1, 1f),
            new BattleTileRules(BattleTileType.Rocks, false, 1, 1f)
        };

        public int TicksPerTurn => _ticksPerTurn;
        public int DeploymentColumns => _deploymentColumns;
        public int ReinforcementTurn => _reinforcementTurn;
        public int MaxTurns => _maxTurns;
        public float TickDuration => _tickDuration;
        public float DamageFactor => _damageFactor;
        public float RangedDamageFactor => _rangedDamageFactor;
        public float KilledShare => _killedShare;
        public float OrganizationLossFactor => _organizationLossFactor;
        public float OrganizationLossPerFight => _organizationLossPerFight;
        public float RoutThreshold => _routThreshold;
        public float ArmyDefeatMorale => _armyDefeatMorale;
        public float DefendMultiplier => _defendMultiplier;
        public float DuelSuccessMultiplier => _duelSuccessMultiplier;
        public float DuelFailMultiplier => _duelFailMultiplier;
        public float ShoutSuccessBonus => _shoutSuccessBonus;
        public float ShoutFailBonus => _shoutFailBonus;
        public int ShootRowSpread => _shootRowSpread;

        public BattleTileRules GetRules(BattleTileType type)
        {
            foreach (var rules in _tileRules)
                if (rules.Type == type) return rules;
            return new BattleTileRules(type, true, 1, 1f);
        }
    }
}
