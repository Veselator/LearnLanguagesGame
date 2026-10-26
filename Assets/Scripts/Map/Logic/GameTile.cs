using System.Collections.Generic;
using LearnLanguages.Battle.Data;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Data;

namespace LearnLanguages.Map.Logic
{
    /// <summary>
    /// Runtime state of a province.
    /// </summary>
    public sealed class GameTile
    {
        private readonly List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>();
        private readonly List<Army> _armies = new List<Army>();

        public GameTile(int id, ProvinceDataEntry data)
        {
            Id = id;
            Data = data;
            OwnerId = PlayerData.NeutralId;
        }

        public int Id { get; }
        public ProvinceDataEntry Data { get; }
        public int OwnerId { get; private set; }
        public BattlefieldMapData Battlefield { get; private set; }
        public IReadOnlyList<UpgradeDefinition> Upgrades => _upgrades;
        public IReadOnlyList<Army> Armies => _armies;

        public bool IsLand => Data.Type == ProvinceType.Land;
        public bool IsSea => Data.Type == ProvinceType.Sea;
        public bool IsPassable => !Data.IsImpassable;
        public SettlementType Settlement => Data.Settlement;
        public bool HasSettlement => Settlement != SettlementType.None;
        public bool IsNeutral => OwnerId == PlayerData.NeutralId;

        public bool HasPort
        {
            get
            {
                foreach (var upgrade in _upgrades)
                    if (upgrade.IsPort) return true;
                return false;
            }
        }

        public bool HasUpgrade(UpgradeDefinition upgrade) => _upgrades.Contains(upgrade);

        public bool HasAdvancedBuilding
        {
            get
            {
                foreach (var upgrade in _upgrades)
                    if (upgrade.UnlocksAdvancedUnits) return true;
                return false;
            }
        }

        public void SetOwner(int ownerId) => OwnerId = ownerId;

        public void SetBattlefield(BattlefieldMapData battlefield) => Battlefield = battlefield;

        public void AddUpgrade(UpgradeDefinition upgrade)
        {
            if (!_upgrades.Contains(upgrade)) _upgrades.Add(upgrade);
        }

        internal void AttachArmy(Army army)
        {
            if (!_armies.Contains(army)) _armies.Add(army);
        }

        internal void DetachArmy(Army army) => _armies.Remove(army);

        public List<Army> GetArmiesOf(int ownerId, List<Army> buffer = null)
        {
            buffer = buffer ?? new List<Army>();
            buffer.Clear();
            foreach (var army in _armies)
                if (army.OwnerId == ownerId) buffer.Add(army);
            return buffer;
        }

        public bool HasHostileArmy(int playerId)
        {
            foreach (var army in _armies)
                if (army.OwnerId != playerId) return true;
            return false;
        }
    }
}
