using System;
using System.Collections.Generic;
using LearnLanguages.Battle.Logic;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Localization;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.Conquest
{
    public interface IWarService
    {
        /// <summary>Moves an army into a neighbouring tile, fighting whoever stands there.</summary>
        void EnterTile(Army army, GameTile target, Action onComplete);

        /// <summary>Own armies standing next to the target that would support an attack on it.</summary>
        List<Army> GetSupport(int ownerId, GameTile target, Army exclude);
    }

    public sealed class WarService : IWarService
    {
        private readonly IGameMap _map;
        private readonly ArmiesRegistry _armies;
        private readonly IPlayersRegistry _players;
        private readonly IConquestService _conquest;
        private readonly IBattleService _battles;
        private readonly IArmyFactory _armyFactory;
        private readonly IMovementRules _movement;
        private readonly IEventBus _eventBus;
        private readonly ILocalizationService _localization;

        public WarService(IGameMap map, ArmiesRegistry armies, IPlayersRegistry players, IConquestService conquest,
            IBattleService battles, IArmyFactory armyFactory, IMovementRules movement, IEventBus eventBus,
            ILocalizationService localization)
        {
            _map = map;
            _armies = armies;
            _players = players;
            _conquest = conquest;
            _battles = battles;
            _armyFactory = armyFactory;
            _movement = movement;
            _eventBus = eventBus;
            _localization = localization;
        }

        public List<Army> GetSupport(int ownerId, GameTile target, Army exclude)
        {
            var result = new List<Army>();
            foreach (var neighbourId in target.Data.Neighbours)
            foreach (var army in _map.Get(neighbourId).Armies)
                if (army.OwnerId == ownerId && army != exclude && !army.IsEmpty) result.Add(army);
            return result;
        }

        public void EnterTile(Army army, GameTile target, Action onComplete)
        {
            var defenderOwner = FindHostileOwner(target, army.OwnerId);
            if (defenderOwner == null)
            {
                MoveInto(army, target);
                onComplete();
                return;
            }

            var defenders = target.GetArmiesOf(defenderOwner.Value);
            var support = GetSupport(army.OwnerId, target, army);

            if (defenderOwner.Value == PlayerData.NeutralId && support.Count > 0)
            {
                // "Crushing victory": a neutral settlement surrounded by two armies surrenders without losses.
                foreach (var garrison in defenders) _conquest.DestroyArmy(garrison);
                MoveInto(army, target);
                Notify("notify.crushing_victory", _players.Get(army.OwnerId).Name, target.Data.Name);
                onComplete();
                return;
            }

            var attackerPlayer = _players.Get(army.OwnerId);
            var defenderPlayer = _players.Get(defenderOwner.Value);
            var defenderSupport = defenderOwner.Value == PlayerData.NeutralId
                ? new List<Army>()
                : GetSupport(defenderOwner.Value, target, null);

            var setup = new BattleSetup(
                new BattleSide(BattleSideId.Attacker, attackerPlayer, new List<Army> { army }, support),
                new BattleSide(BattleSideId.Defender, defenderPlayer, defenders, defenderSupport),
                target.Battlefield, target);

            army.MarkMoved();
            _battles.StartBattle(setup, result =>
            {
                ApplyResult(result, army, target);
                onComplete();
            });
        }

        private void ApplyResult(BattleResult result, Army attacker, GameTile target)
        {
            var setup = result.Setup;
            CleanUp(setup.Attacker);
            CleanUp(setup.Defender);

            var attackerName = setup.Attacker.Player.Name;
            var defenderName = setup.Defender.Player.Name;
            if (result.AttackerWon)
            {
                foreach (var defender in new List<Army>(target.Armies))
                    if (defender.OwnerId == setup.Defender.Player.Id) Retreat(defender);

                if (_armies.Get(attacker.Id) != null && FindHostileOwner(target, attacker.OwnerId) == null)
                    MoveInto(attacker, target);
                Notify("notify.battle_won", attackerName, defenderName, target.Data.Name);
            }
            else
            {
                Notify("notify.battle_lost", attackerName, defenderName, target.Data.Name);
            }
        }

        private void CleanUp(BattleSide side)
        {
            foreach (var army in side.Armies) CleanUp(army);
            foreach (var army in side.Reinforcements) CleanUp(army);
        }

        private void CleanUp(Army army)
        {
            if (_armies.Get(army.Id) == null) return;
            army.RemoveDestroyedCohorts();
            if (army.IsEmpty)
            {
                _conquest.DestroyArmy(army);
                return;
            }
            _armyFactory.EnsureCommander(army);
            _eventBus.Publish(new ArmyChangedEvent(army));
        }

        private void Retreat(Army army)
        {
            if (_armies.Get(army.Id) == null) return;
            var from = _map.Get(army.TileId);
            GameTile best = null;
            foreach (var id in from.Data.Neighbours)
            {
                var tile = _map.Get(id);
                if (!_movement.CanStep(army.OwnerId, from, tile) || tile.HasHostileArmy(army.OwnerId)) continue;
                if (tile.OwnerId == army.OwnerId) best = tile;
                else if (best == null && tile.IsLand && army.OwnerId != PlayerData.NeutralId) best = tile;
            }

            if (best == null)
            {
                _conquest.DestroyArmy(army);
                return;
            }
            var fromId = army.TileId;
            _armies.Relocate(army, best.Id);
            army.MarkMoved();
            _eventBus.Publish(new ArmyMovedEvent(army, fromId));
        }

        private void MoveInto(Army army, GameTile target)
        {
            var fromId = army.TileId;
            _armies.Relocate(army, target.Id);
            army.MarkMoved();
            _eventBus.Publish(new ArmyMovedEvent(army, fromId));
            if (target.IsLand && target.OwnerId != army.OwnerId) _conquest.Capture(target, army.OwnerId);
        }

        private static int? FindHostileOwner(GameTile tile, int playerId)
        {
            foreach (var army in tile.Armies)
                if (army.OwnerId != playerId) return army.OwnerId;
            return null;
        }

        private void Notify(string key, params object[] args) =>
            _eventBus.Publish(new NotificationEvent(_localization.Format(key, args)));
    }
}
