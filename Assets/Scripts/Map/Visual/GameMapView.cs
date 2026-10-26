using System;
using System.Collections.Generic;
using LearnLanguages.Cameras;
using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Gameplay.Players;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Logic;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Map.Visual
{
    /// <summary>
    /// Visualises the global map state: terrain colors, settlements and armies. Reacts to game events only.
    /// </summary>
    public sealed class GameMapView : MonoBehaviour
    {
        [SerializeField] private MapTerrainView _terrain;
        [SerializeField] private Transform _tilesRoot;
        [SerializeField] private float _spriteFacingPitch = 55f;
        [SerializeField] private float _armySpacing = 2.2f;

        private readonly Dictionary<int, VisualGameTile> _tileViews = new Dictionary<int, VisualGameTile>();
        private readonly Dictionary<int, ArmyView> _armyViews = new Dictionary<int, ArmyView>();
        private readonly Dictionary<int, ProvinceHighlight> _desiredHighlights = new Dictionary<int, ProvinceHighlight>();
        private readonly Dictionary<int, ProvinceHighlight> _currentHighlights = new Dictionary<int, ProvinceHighlight>();
        private readonly ProvinceLookup _lookup = new ProvinceLookup();

        private IEventBus _eventBus;
        private IGameMap _map;
        private ICurrentMap _currentMap;
        private IArmiesRegistry _armies;
        private IPlayersRegistry _players;
        private IMovementRules _movement;
        private VisualGameTile.Factory _tileFactory;
        private ArmyView.Pool _armyPool;
        private CameraController _cameraController;
        private RtsCameraMethod _rtsCamera;
        private ArmyView _selectedArmyView;

        public MapTerrainView Terrain => _terrain;
        private Quaternion Facing => Quaternion.Euler(_spriteFacingPitch, 0f, 0f);

        [Inject]
        public void Construct(IEventBus eventBus, IGameMap map, ICurrentMap currentMap, IArmiesRegistry armies,
            IPlayersRegistry players, IMovementRules movement, VisualGameTile.Factory tileFactory, ArmyView.Pool armyPool,
            CameraController cameraController, RtsCameraMethod rtsCamera)
        {
            _eventBus = eventBus;
            _map = map;
            _currentMap = currentMap;
            _armies = armies;
            _players = players;
            _movement = movement;
            _tileFactory = tileFactory;
            _armyPool = armyPool;
            _cameraController = cameraController;
            _rtsCamera = rtsCamera;

            _eventBus.Subscribe<GameReadyEvent>(OnGameReady);
            _eventBus.Subscribe<ArmyCreatedEvent>(OnArmyCreated);
            _eventBus.Subscribe<ArmyMovedEvent>(OnArmyMoved);
            _eventBus.Subscribe<ArmyChangedEvent>(OnArmyChanged);
            _eventBus.Subscribe<ArmyDestroyedEvent>(OnArmyDestroyed);
            _eventBus.Subscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
            _eventBus.Subscribe<TileUpgradedEvent>(OnTileUpgraded);
            _eventBus.Subscribe<SelectionChangedEvent>(OnSelectionChanged);
            _eventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<GameReadyEvent>(OnGameReady);
            _eventBus.Unsubscribe<ArmyCreatedEvent>(OnArmyCreated);
            _eventBus.Unsubscribe<ArmyMovedEvent>(OnArmyMoved);
            _eventBus.Unsubscribe<ArmyChangedEvent>(OnArmyChanged);
            _eventBus.Unsubscribe<ArmyDestroyedEvent>(OnArmyDestroyed);
            _eventBus.Unsubscribe<TileOwnerChangedEvent>(OnTileOwnerChanged);
            _eventBus.Unsubscribe<TileUpgradedEvent>(OnTileUpgraded);
            _eventBus.Unsubscribe<SelectionChangedEvent>(OnSelectionChanged);
            _eventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
        }

        public Vector3 GetTileCenter(GameTile tile) => _terrain.PixelToWorld(tile.Data.LocalUnitPosition);

        private void OnGameReady(GameReadyEvent e)
        {
            var map = _currentMap.Map;
            _lookup.Build(map.Provinces, map.ProvincesData);
            _terrain.Build(map, _lookup);

            foreach (var tile in _map.Tiles)
            {
                if (!tile.IsLand) continue;
                PaintOwner(tile);
                if (!tile.HasSettlement) continue;

                var view = _tileFactory.Create();
                view.transform.SetParent(_tilesRoot, false);
                view.Bind(tile, _terrain.PixelToWorld(tile.Data.SettlementLocalPos),
                    _terrain.PixelToWorld(tile.Data.PortLocalPos), Facing);
                view.Refresh(_players.Get(tile.OwnerId).Color, tile.IsNeutral);
                _tileViews[tile.Id] = view;
            }
            _terrain.Painter.Apply();

            foreach (var army in _armies.All) SpawnArmy(army);
            foreach (var tile in _map.Tiles) LayoutArmies(tile, true);

            var size = map.WorldSize;
            _rtsCamera.SetBounds(new Rect(0f, 0f, size.x, size.y));
            var human = _players.HumanPlayer;
            if (human != null) _rtsCamera.FocusOn(GetTileCenter(_map.Get(human.Data.CapitalTileId)));
            _cameraController.SetMethod(_rtsCamera);
        }

        private void PaintOwner(GameTile tile)
        {
            var owner = _players.Get(tile.OwnerId);
            _terrain.Painter.SetTint(tile.Id, owner.Color, !tile.IsNeutral);
        }

        private void SpawnArmy(Army army)
        {
            if (_armyViews.ContainsKey(army.Id)) return;
            var view = _armyPool.Spawn();
            view.Bind(army, _players.Get(army.OwnerId).Color, Facing);
            _armyViews[army.Id] = view;
        }

        private void LayoutArmies(GameTile tile, bool immediate)
        {
            var count = tile.Armies.Count;
            var center = _terrain.PixelToWorld(tile.Data.LocalUnitPosition);
            for (var i = 0; i < count; i++)
            {
                var army = tile.Armies[i];
                if (!_armyViews.TryGetValue(army.Id, out var view)) continue;
                var offset = (i - (count - 1) * 0.5f) * _armySpacing;
                var position = _terrain.PixelToWorld(_terrain.WorldToPixel(center + Vector3.right * offset));
                if (immediate) view.PlaceImmediately(position);
                else view.MoveTo(position);
                RefreshArmy(view);
            }
        }

        private void RefreshArmy(ArmyView view)
        {
            var army = view.Army;
            var showMoved = army.HasMoved && _players.Get(army.OwnerId).IsHuman;
            view.Refresh(_map.Get(army.TileId).IsSea, showMoved);
        }

        private void OnArmyCreated(ArmyCreatedEvent e)
        {
            if (_terrain.Painter == null) return;
            SpawnArmy(e.Army);
            LayoutArmies(_map.Get(e.Army.TileId), true);
        }

        private void OnArmyMoved(ArmyMovedEvent e)
        {
            LayoutArmies(_map.Get(e.FromTileId), false);
            LayoutArmies(_map.Get(e.Army.TileId), false);
        }

        private void OnArmyChanged(ArmyChangedEvent e)
        {
            if (_armyViews.TryGetValue(e.Army.Id, out var view)) RefreshArmy(view);
        }

        private void OnArmyDestroyed(ArmyDestroyedEvent e)
        {
            if (!_armyViews.TryGetValue(e.Army.Id, out var view)) return;
            _armyViews.Remove(e.Army.Id);
            if (_selectedArmyView == view) _selectedArmyView = null;
            _armyPool.Despawn(view);
            LayoutArmies(_map.Get(e.Army.TileId), false);
        }

        private void OnTileOwnerChanged(TileOwnerChangedEvent e)
        {
            PaintOwner(e.Tile);
            _terrain.Painter.Apply();
            if (_tileViews.TryGetValue(e.Tile.Id, out var view))
                view.Refresh(_players.Get(e.Tile.OwnerId).Color, e.Tile.IsNeutral);
        }

        private void OnTileUpgraded(TileUpgradedEvent e)
        {
            if (_tileViews.TryGetValue(e.Tile.Id, out var view))
                view.Refresh(_players.Get(e.Tile.OwnerId).Color, e.Tile.IsNeutral);
        }

        private void OnTurnStarted(TurnStartedEvent e)
        {
            foreach (var view in _armyViews.Values) RefreshArmy(view);
        }

        private void OnSelectionChanged(SelectionChangedEvent e)
        {
            if (_terrain.Painter == null) return;

            if (_selectedArmyView != null) _selectedArmyView.SetSelected(false);
            _selectedArmyView = null;

            _desiredHighlights.Clear();
            if (e.Tile != null) _desiredHighlights[e.Tile.Id] = ProvinceHighlight.Selected;
            if (e.Army != null && _armyViews.TryGetValue(e.Army.Id, out var armyView))
            {
                _selectedArmyView = armyView;
                armyView.SetSelected(true);
                if (_players.Get(e.Army.OwnerId).IsHuman)
                    foreach (var target in _movement.GetTargets(e.Army))
                        if (!_desiredHighlights.ContainsKey(target.Id)) _desiredHighlights[target.Id] = ProvinceHighlight.MoveTarget;
            }

            // Repainting the texture is costly, so skip it when nothing changed.
            if (SameHighlights()) return;
            foreach (var id in _currentHighlights.Keys) _terrain.Painter.SetHighlight(id, ProvinceHighlight.None);
            _currentHighlights.Clear();
            foreach (var pair in _desiredHighlights)
            {
                _terrain.Painter.SetHighlight(pair.Key, pair.Value);
                _currentHighlights[pair.Key] = pair.Value;
            }
            _terrain.Painter.Apply();
        }

        private bool SameHighlights()
        {
            if (_desiredHighlights.Count != _currentHighlights.Count) return false;
            foreach (var pair in _desiredHighlights)
                if (!_currentHighlights.TryGetValue(pair.Key, out var current) || current != pair.Value) return false;
            return true;
        }
    }
}
