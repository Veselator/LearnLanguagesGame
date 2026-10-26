using System.Collections.Generic;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.Economy
{
    public interface IConstructionRules
    {
        /// <summary>Returns null when the upgrade can be built, otherwise a localization key with the reason.</summary>
        string GetBuildBlocker(GameTile tile, UpgradeDefinition upgrade, PlayerData player);
        IReadOnlyList<UpgradeDefinition> GetCandidates(GameTile tile);
        string GetRecruitBlocker(GameTile tile, UnitTypeDefinition unit, PlayerData player);
        ResourceBundle GetRecruitCost(GameTile tile, UnitTypeDefinition unit);
        ResourceBundle GetReinforceCost(Army army);
    }

    public sealed class ConstructionRules : IConstructionRules
    {
        private readonly UpgradesConfig _upgrades;
        private readonly IGameMap _map;
        private readonly List<UpgradeDefinition> _candidates = new List<UpgradeDefinition>();

        public ConstructionRules(UpgradesConfig upgrades, IGameMap map)
        {
            _upgrades = upgrades;
            _map = map;
        }

        public IReadOnlyList<UpgradeDefinition> GetCandidates(GameTile tile)
        {
            _candidates.Clear();
            foreach (var upgrade in _upgrades.Upgrades)
                if (upgrade.AllowedSettlements.Allows(tile.Settlement) && !tile.HasUpgrade(upgrade)) _candidates.Add(upgrade);
            return _candidates;
        }

        public string GetBuildBlocker(GameTile tile, UpgradeDefinition upgrade, PlayerData player)
        {
            if (tile.OwnerId != player.Id) return "reason.not_owner";
            if (!upgrade.AllowedSettlements.Allows(tile.Settlement)) return "reason.wrong_settlement";
            if (tile.HasUpgrade(upgrade)) return "reason.already_built";
            if (tile.Upgrades.Count >= _upgrades.GetSlots(tile.Settlement)) return "reason.no_slots";
            if (upgrade.RequiredLandscape != LandscapeType.Sea && tile.Data.Landscape != upgrade.RequiredLandscape)
                return "reason.wrong_landscape";
            if (upgrade.IsPort && !IsCoastal(tile)) return "reason.not_coastal";
            if (!player.Resources.Covers(upgrade.Cost)) return "reason.no_resources";
            return null;
        }

        public string GetRecruitBlocker(GameTile tile, UnitTypeDefinition unit, PlayerData player)
        {
            if (tile.OwnerId != player.Id) return "reason.not_owner";
            if (!unit.Recruitable) return "reason.not_recruitable";
            if (tile.Settlement < unit.MinimumSettlement) return "reason.wrong_settlement";
            if (unit.RequiresAdvancedBuilding && !tile.HasAdvancedBuilding && tile.Settlement != SettlementType.City)
                return "reason.needs_barracks";
            if (!player.Resources.Covers(GetRecruitCost(tile, unit))) return "reason.no_resources";
            return null;
        }

        public ResourceBundle GetRecruitCost(GameTile tile, UnitTypeDefinition unit)
        {
            var discount = 0;
            foreach (var upgrade in tile.Upgrades)
                if (upgrade.RecruitDiscountPercent > discount) discount = upgrade.RecruitDiscountPercent;
            return unit.RecruitCost.Scale(1f - discount / 100f);
        }

        public ResourceBundle GetReinforceCost(Army army)
        {
            var gold = 0;
            foreach (var cohort in army.Cohorts)
                gold += (cohort.Killed * cohort.Type.ReinforceGoldPerTen + 9) / 10;
            return new ResourceBundle(gold, 0, 0, 0);
        }

        private bool IsCoastal(GameTile tile)
        {
            foreach (var id in tile.Data.Neighbours)
                if (_map.Get(id).IsSea) return true;
            return false;
        }
    }
}
