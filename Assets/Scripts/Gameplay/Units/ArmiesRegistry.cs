using System.Collections.Generic;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.Gameplay.Units
{
    public interface IArmiesRegistry
    {
        IReadOnlyList<Army> All { get; }
        Army Get(int id);
        List<Army> GetArmiesOf(int ownerId);
    }

    /// <summary>
    /// Owns every army and keeps tile membership consistent.
    /// </summary>
    public sealed class ArmiesRegistry : IArmiesRegistry
    {
        private readonly List<Army> _armies = new List<Army>();
        private readonly Dictionary<int, Army> _byId = new Dictionary<int, Army>();
        private readonly IGameMap _map;
        private int _nextId = 1;

        public ArmiesRegistry(IGameMap map)
        {
            _map = map;
        }

        public IReadOnlyList<Army> All => _armies;
        public int NextId => _nextId;

        public Army Get(int id) => _byId.TryGetValue(id, out var army) ? army : null;

        public List<Army> GetArmiesOf(int ownerId)
        {
            var result = new List<Army>();
            foreach (var army in _armies)
                if (army.OwnerId == ownerId) result.Add(army);
            return result;
        }

        public int ReserveId() => _nextId++;

        public void SetNextId(int nextId) => _nextId = nextId;

        public void Add(Army army)
        {
            _armies.Add(army);
            _byId[army.Id] = army;
            _map.Get(army.TileId)?.AttachArmy(army);
            if (army.Id >= _nextId) _nextId = army.Id + 1;
        }

        public void Remove(Army army)
        {
            _armies.Remove(army);
            _byId.Remove(army.Id);
            _map.Get(army.TileId)?.DetachArmy(army);
        }

        public void Relocate(Army army, int tileId)
        {
            _map.Get(army.TileId)?.DetachArmy(army);
            army.SetTile(tileId);
            _map.Get(tileId)?.AttachArmy(army);
        }

        public void Clear()
        {
            foreach (var army in _armies) _map.Get(army.TileId)?.DetachArmy(army);
            _armies.Clear();
            _byId.Clear();
            _nextId = 1;
        }
    }
}
