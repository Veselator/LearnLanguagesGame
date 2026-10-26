using System.Collections.Generic;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.Units
{
    public interface IMovementRules
    {
        /// <summary>Whether an army of <paramref name="ownerId"/> may step between two neighbouring tiles.</summary>
        bool CanStep(int ownerId, GameTile from, GameTile to);
        bool CanMove(Army army, GameTile to);
        List<GameTile> GetTargets(Army army);
    }

    /// <summary>
    /// One province per turn. Leaving land for the sea requires an own port.
    /// </summary>
    public sealed class MovementRules : IMovementRules
    {
        private readonly IGameMap _map;

        public MovementRules(IGameMap map)
        {
            _map = map;
        }

        public bool CanStep(int ownerId, GameTile from, GameTile to)
        {
            if (!to.IsPassable) return false;
            if (from.IsLand && to.IsSea) return from.HasPort && from.OwnerId == ownerId;
            return true;
        }

        public bool CanMove(Army army, GameTile to)
        {
            if (army.HasMoved || to == null) return false;
            if (!_map.AreNeighbours(army.TileId, to.Id)) return false;
            return CanStep(army.OwnerId, _map.Get(army.TileId), to);
        }

        public List<GameTile> GetTargets(Army army)
        {
            var result = new List<GameTile>();
            if (army.HasMoved) return result;
            var from = _map.Get(army.TileId);
            foreach (var id in from.Data.Neighbours)
            {
                var to = _map.Get(id);
                if (CanStep(army.OwnerId, from, to)) result.Add(to);
            }
            return result;
        }
    }
}
