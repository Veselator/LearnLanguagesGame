using System.Collections.Generic;
using LearnLanguages.Core.Randomness;
using LearnLanguages.Gameplay.AI;
using LearnLanguages.Gameplay.Economy;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Localization;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;
using LearnLanguages.Save;
using LearnLanguages.Settings;

namespace LearnLanguages.Gameplay.World
{
    public interface IGameWorldBuilder
    {
        void PrepareMap(GameSettings settings);
        void CreateNew(GameSettings settings);
        void Restore(SaveData save);
        SaveData Capture(GameSettings settings, int round, int currentPlayerIndex);
    }

    /// <summary>
    /// Creates the world state for a new match or restores it from a save, and captures it for saving.
    /// </summary>
    public sealed class GameWorldBuilder : IGameWorldBuilder
    {
        private const int HumanPlayerId = 0;

        private readonly MapsConfig _maps;
        private readonly CurrentMap _currentMap;
        private readonly GameMap _gameMap;
        private readonly PlayersRegistry _players;
        private readonly ArmiesRegistry _armies;
        private readonly IArmyFactory _armyFactory;
        private readonly PlayersConfig _playersConfig;
        private readonly IAIProfilesStore _aiProfiles;
        private readonly EconomyConfig _economy;
        private readonly UnitsConfig _units;
        private readonly UpgradesConfig _upgrades;
        private readonly IRandomService _random;
        private readonly ILocalizationService _localization;

        public GameWorldBuilder(MapsConfig maps, CurrentMap currentMap, GameMap gameMap, PlayersRegistry players,
            ArmiesRegistry armies, IArmyFactory armyFactory, PlayersConfig playersConfig, IAIProfilesStore aiProfiles, EconomyConfig economy,
            UnitsConfig units, UpgradesConfig upgrades, IRandomService random, ILocalizationService localization)
        {
            _maps = maps;
            _currentMap = currentMap;
            _gameMap = gameMap;
            _players = players;
            _armies = armies;
            _armyFactory = armyFactory;
            _playersConfig = playersConfig;
            _aiProfiles = aiProfiles;
            _economy = economy;
            _units = units;
            _upgrades = upgrades;
            _random = random;
            _localization = localization;
        }

        public void PrepareMap(GameSettings settings)
        {
            var map = _maps.GetById(settings.MapId);
            _currentMap.Set(map);
            _gameMap.Build(map.ProvincesData);
            _armies.Clear();
        }

        public void CreateNew(GameSettings settings)
        {
            var map = _currentMap.Map;
            var provinces = map.ProvincesData;
            foreach (var tile in _gameMap.Tiles)
            {
                var twin = provinces.GetMirror(tile.Id);
                tile.SetBattlefield(twin < tile.Id
                    ? _gameMap.Get(twin).Battlefield
                    : map.BattlefieldMapsConfig.Pick(tile.Data.Landscape, _random));
            }

            var spawnPoints = OrderSpawnPoints(provinces);
            var aiCount = UnityEngine.Mathf.Clamp(settings.AiPlayersCount, 1,
                UnityEngine.Mathf.Min(_playersConfig.MaxAiPlayers, spawnPoints.Count - 1));

            var players = new List<PlayerData>();
            for (var i = 0; i <= aiCount; i++)
            {
                var isHuman = i == HumanPlayerId;
                var name = isHuman ? _localization.Get("player.human") : _localization.Get(_playersConfig.GetAiNameKey(i - 1));
                // The strength of an AI player in battles and duels follows from its model.
                var profile = isHuman ? null : _aiProfiles.Pick(settings.Difficulty, _random);
                var difficulty = profile != null ? profile.Difficulty : settings.Difficulty;
                var player = new PlayerData(i, name, _playersConfig.GetColor(i), isHuman, difficulty, spawnPoints[i]);
                player.SetResources(_economy.StartingResources);
                if (profile != null) player.SetAiProfile(profile.Id);
                players.Add(player);
            }
            _players.Initialize(players, CreateNeutral(settings));

            foreach (var player in players)
            {
                _gameMap.Get(player.CapitalTileId).SetOwner(player.Id);
                _armyFactory.CreateArmy(player.Id, player.CapitalTileId, _units.StartingArmy);
            }

            foreach (var tile in _gameMap.Tiles)
                if (tile.IsLand && tile.IsNeutral && tile.HasSettlement) _armyFactory.CreateGarrison(tile);
        }

        public void Restore(SaveData save)
        {
            var battlefields = _currentMap.Map.BattlefieldMapsConfig.AllMaps;

            var players = new List<PlayerData>();
            foreach (var p in save.Players)
            {
                var player = new PlayerData(p.Id, p.Name, p.Color, p.IsHuman, p.Difficulty, p.CapitalTileId);
                player.SetResources(p.Resources);
                player.SetAiProfile(p.AiProfileId);
                player.SetAlive(p.IsAlive);
                players.Add(player);
            }
            _players.Initialize(players, CreateNeutral(save.Settings));

            foreach (var t in save.Tiles)
            {
                var tile = _gameMap.Get(t.Id);
                if (tile == null) continue;
                tile.SetOwner(t.OwnerId);
                if (t.BattlefieldIndex >= 0 && t.BattlefieldIndex < battlefields.Count)
                    tile.SetBattlefield(battlefields[t.BattlefieldIndex]);
                else tile.SetBattlefield(_currentMap.Map.BattlefieldMapsConfig.Pick(tile.Data.Landscape, _random));
                foreach (var upgradeId in t.Upgrades)
                {
                    var upgrade = _upgrades.GetById(upgradeId);
                    if (upgrade != null) tile.AddUpgrade(upgrade);
                }
            }

            foreach (var a in save.Armies)
            {
                var army = new Army(a.Id, a.OwnerId, a.TileId);
                army.SetMovedState(a.HasMoved);
                foreach (var c in a.Cohorts)
                {
                    var type = _units.GetById(c.UnitId);
                    if (type != null) army.AddCohort(new Cohort(type, c.MaxSoldiers, c.Healthy, c.Wounded, c.Organization));
                }
                if (!army.IsEmpty) _armies.Add(army);
            }
            _armies.SetNextId(save.NextArmyId);
        }

        public SaveData Capture(GameSettings settings, int round, int currentPlayerIndex)
        {
            var battlefields = _currentMap.Map.BattlefieldMapsConfig.AllMaps;
            var save = new SaveData
            {
                Settings = settings, Round = round, CurrentPlayerIndex = currentPlayerIndex, NextArmyId = _armies.NextId
            };

            foreach (var player in _players.Players)
            {
                var data = player.Data;
                save.Players.Add(new PlayerSave
                {
                    Id = data.Id, Name = data.Name, Color = data.Color, IsHuman = data.IsHuman,
                    Difficulty = data.AiDifficulty, AiProfileId = data.AiProfileId, CapitalTileId = data.CapitalTileId, IsAlive = data.IsAlive,
                    Resources = data.Resources
                });
            }

            foreach (var tile in _gameMap.Tiles)
            {
                var tileSave = new TileSave
                {
                    Id = tile.Id, OwnerId = tile.OwnerId, BattlefieldIndex = IndexOf(battlefields, tile.Battlefield)
                };
                foreach (var upgrade in tile.Upgrades) tileSave.Upgrades.Add(upgrade.Id);
                save.Tiles.Add(tileSave);
            }

            foreach (var army in _armies.All)
            {
                var armySave = new ArmySave { Id = army.Id, OwnerId = army.OwnerId, TileId = army.TileId, HasMoved = army.HasMoved };
                foreach (var cohort in army.Cohorts)
                {
                    armySave.Cohorts.Add(new CohortSave
                    {
                        UnitId = cohort.Type.Id, MaxSoldiers = cohort.MaxSoldiers, Healthy = cohort.Healthy,
                        Wounded = cohort.Wounded, Organization = cohort.Organization
                    });
                }
                save.Armies.Add(armySave);
            }
            return save;
        }

        /// <summary>
        /// Random order of the spawn points. On a mirrored map they stay in twin pairs, so a duel is always
        /// played from mirrored capitals.
        /// </summary>
        private List<int> OrderSpawnPoints(ProvincesData provinces)
        {
            var spawnPoints = new List<int>(provinces.PossibleSpawnPoints);
            if (!provinces.IsMirrored)
            {
                _random.Shuffle(spawnPoints);
                return spawnPoints;
            }

            var pairs = new List<int>();
            foreach (var spawn in spawnPoints)
            {
                var twin = provinces.GetMirror(spawn);
                if (spawn < twin && spawnPoints.Contains(twin)) pairs.Add(spawn);
            }
            _random.Shuffle(pairs);

            var ordered = new List<int>();
            foreach (var first in pairs)
            {
                var twin = provinces.GetMirror(first);
                var swap = _random.Range(0, 2) == 1;
                ordered.Add(swap ? twin : first);
                ordered.Add(swap ? first : twin);
            }
            foreach (var spawn in spawnPoints)
                if (!ordered.Contains(spawn)) ordered.Add(spawn);
            return ordered;
        }

        private PlayerData CreateNeutral(GameSettings settings) =>
            new PlayerData(PlayerData.NeutralId, _localization.Get("player.neutral"), _playersConfig.NeutralColor, false,
                settings.Difficulty, -1);

        private static int IndexOf<T>(IReadOnlyList<T> list, T item) where T : class
        {
            for (var i = 0; i < list.Count; i++)
                if (list[i] == item) return i;
            return -1;
        }
    }
}
