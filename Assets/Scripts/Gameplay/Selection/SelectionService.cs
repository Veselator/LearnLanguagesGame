using System;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Logic;
using Zenject;

namespace LearnLanguages.Gameplay.Selection
{
    public interface ISelectionService
    {
        GameTile SelectedTile { get; }
        Army SelectedArmy { get; }
        void Select(GameTile tile, Army army);
        void Clear();
        void Refresh();
    }

    /// <summary>
    /// Currently selected province and army. Follows the selected army when it moves.
    /// </summary>
    public sealed class SelectionService : ISelectionService, IInitializable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IGameMap _map;

        public SelectionService(IEventBus eventBus, IGameMap map)
        {
            _eventBus = eventBus;
            _map = map;
        }

        public GameTile SelectedTile { get; private set; }
        public Army SelectedArmy { get; private set; }

        public void Initialize()
        {
            _eventBus.Subscribe<ArmyMovedEvent>(OnArmyMoved);
            _eventBus.Subscribe<ArmyDestroyedEvent>(OnArmyDestroyed);
            _eventBus.Subscribe<ArmyChangedEvent>(OnArmyChanged);
            _eventBus.Subscribe<TileUpgradedEvent>(OnTileChanged);
            _eventBus.Subscribe<TileOwnerChangedEvent>(OnOwnerChanged);
            _eventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            _eventBus.Subscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ArmyMovedEvent>(OnArmyMoved);
            _eventBus.Unsubscribe<ArmyDestroyedEvent>(OnArmyDestroyed);
            _eventBus.Unsubscribe<ArmyChangedEvent>(OnArmyChanged);
            _eventBus.Unsubscribe<TileUpgradedEvent>(OnTileChanged);
            _eventBus.Unsubscribe<TileOwnerChangedEvent>(OnOwnerChanged);
            _eventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            _eventBus.Unsubscribe<ResourcesChangedEvent>(OnResourcesChanged);
        }

        public void Select(GameTile tile, Army army)
        {
            SelectedTile = tile;
            SelectedArmy = army;
            Refresh();
        }

        public void Clear() => Select(null, null);

        public void Refresh() => _eventBus.Publish(new SelectionChangedEvent(SelectedTile, SelectedArmy));

        private void OnArmyMoved(ArmyMovedEvent e)
        {
            if (e.Army == SelectedArmy) Select(_map.Get(e.Army.TileId), e.Army);
            else if (SelectedTile != null && (SelectedTile.Id == e.FromTileId || SelectedTile.Id == e.Army.TileId)) Refresh();
        }

        private void OnArmyDestroyed(ArmyDestroyedEvent e)
        {
            if (e.Army == SelectedArmy) Select(SelectedTile, null);
            else if (SelectedTile != null && SelectedTile.Id == e.Army.TileId) Refresh();
        }

        private void OnArmyChanged(ArmyChangedEvent e)
        {
            if (e.Army == SelectedArmy || (SelectedTile != null && SelectedTile.Id == e.Army.TileId)) Refresh();
        }

        private void OnTileChanged(TileUpgradedEvent e)
        {
            if (e.Tile == SelectedTile) Refresh();
        }

        private void OnOwnerChanged(TileOwnerChangedEvent e)
        {
            if (e.Tile == SelectedTile) Refresh();
        }

        private void OnTurnStarted(TurnStartedEvent e) => Refresh();

        private void OnResourcesChanged(ResourcesChangedEvent e)
        {
            if (SelectedTile != null) Refresh();
        }
    }
}
