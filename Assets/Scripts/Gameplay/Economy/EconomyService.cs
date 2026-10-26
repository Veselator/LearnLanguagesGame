using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.Economy
{
    public interface IEconomyService
    {
        ResourceBundle CalculateTileIncome(GameTile tile);
        ResourceBundle CalculateIncome(int playerId);
        ResourceBundle CalculateUpkeep(int playerId);
        void ProcessTurnStart(PlayerData player);
    }

    public sealed class EconomyService : IEconomyService
    {
        private readonly EconomyConfig _config;
        private readonly IGameMap _map;
        private readonly IArmiesRegistry _armies;
        private readonly IEventBus _eventBus;

        public EconomyService(EconomyConfig config, IGameMap map, IArmiesRegistry armies, IEventBus eventBus)
        {
            _config = config;
            _map = map;
            _armies = armies;
            _eventBus = eventBus;
        }

        public ResourceBundle CalculateTileIncome(GameTile tile)
        {
            if (!tile.IsLand || tile.IsNeutral) return ResourceBundle.Zero;

            var data = tile.Data;
            var income = data.Production == ProductionType.None
                ? _config.EmptyProvinceYield
                : _config.GetProductionYield(data.Production);
            if (data.Landscape == LandscapeType.Forest) income += _config.ForestYield;
            income += _config.GetSettlementIncome(tile.Settlement);

            foreach (var upgrade in tile.Upgrades)
            {
                if (!upgrade.IsProducing) continue;
                var yield = upgrade.BaseYield;
                if (upgrade.MatchingProduction != ProductionType.None && upgrade.MatchingProduction == data.Production)
                    yield += upgrade.MatchingBonusYield;
                income += yield.Scale(1f + upgrade.NeighbourBonusPerProvince * CountProducingNeighbours(tile));
            }
            return income;
        }

        public ResourceBundle CalculateIncome(int playerId)
        {
            var total = ResourceBundle.Zero;
            foreach (var tile in _map.Tiles)
                if (tile.OwnerId == playerId) total += CalculateTileIncome(tile);
            return total;
        }

        public ResourceBundle CalculateUpkeep(int playerId)
        {
            var total = ResourceBundle.Zero;
            foreach (var army in _armies.All)
            {
                if (army.OwnerId != playerId) continue;
                foreach (var cohort in army.Cohorts) total += cohort.Type.Upkeep;
            }
            return total;
        }

        public void ProcessTurnStart(PlayerData player)
        {
            var balance = player.Resources + CalculateIncome(player.Id) - CalculateUpkeep(player.Id);
            var unpaid = balance.Gold < 0 || balance.Food < 0;
            player.SetResources(balance.ClampMinZero());

            foreach (var army in _armies.All)
            {
                if (army.OwnerId != player.Id) continue;
                RestoreArmy(army, unpaid);
                army.ResetMovement();
                _eventBus.Publish(new ArmyChangedEvent(army));
            }

            _eventBus.Publish(new ResourcesChangedEvent(player));
        }

        private void RestoreArmy(Army army, bool unpaid)
        {
            var tile = _map.Get(army.TileId);
            var inOwnFortress = tile.OwnerId == army.OwnerId &&
                                (tile.Settlement == SettlementType.Castle || tile.Settlement == SettlementType.City);
            var healing = inOwnFortress ? _config.SettlementHealing : _config.FieldHealing;
            var morale = _config.OrganizationRecovery;
            if (tile.OwnerId == army.OwnerId)
            {
                foreach (var upgrade in tile.Upgrades)
                {
                    healing += upgrade.HealingBonus;
                    morale += upgrade.MoraleBonus;
                }
            }
            if (unpaid) morale = -_config.UnpaidUpkeepPenalty;

            foreach (var cohort in army.Cohorts)
            {
                cohort.HealWounded(healing);
                cohort.ChangeOrganization(morale);
            }
        }

        private int CountProducingNeighbours(GameTile tile)
        {
            if (tile.Data.Production == ProductionType.None) return 0;
            var count = 0;
            foreach (var id in tile.Data.Neighbours)
            {
                var neighbour = _map.Get(id);
                if (neighbour.OwnerId == tile.OwnerId && neighbour.Data.Production == tile.Data.Production) count++;
            }
            return count;
        }
    }
}
