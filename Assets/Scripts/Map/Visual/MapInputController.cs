using LearnLanguages.Cameras;
using LearnLanguages.Core.Input;
using LearnLanguages.Gameplay.Commands;
using LearnLanguages.Gameplay.Selection;
using LearnLanguages.Gameplay.Turns;
using LearnLanguages.Gameplay.Units;
using LearnLanguages.Map.Logic;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Map.Visual
{
    /// <summary>
    /// Mouse interaction with the global map: left click selects, right click moves the selected army.
    /// </summary>
    public sealed class MapInputController : ITickable
    {
        private readonly IInputService _input;
        private readonly CameraController _camera;
        private readonly GameMapView _mapView;
        private readonly IGameMap _map;
        private readonly ISelectionService _selection;
        private readonly IHumanTurnState _turnState;
        private readonly IMovementRules _movement;
        private readonly IMapCommandProcessor _processor;
        private readonly Battle.Logic.IBattleService _battles;

        public MapInputController(IInputService input, CameraController camera, GameMapView mapView, IGameMap map,
            ISelectionService selection, IHumanTurnState turnState, IMovementRules movement,
            IMapCommandProcessor processor, Battle.Logic.IBattleService battles)
        {
            _input = input;
            _camera = camera;
            _mapView = mapView;
            _map = map;
            _selection = selection;
            _turnState = turnState;
            _movement = movement;
            _processor = processor;
            _battles = battles;
        }

        public void Tick()
        {
            if (_battles.IsBattleInProgress || _mapView.Terrain.Painter == null || _input.IsPointerOverUi) return;

            if (_input.PrimaryClicked) HandleSelect();
            else if (_input.SecondaryClicked) HandleMove();
        }

        private void HandleSelect()
        {
            if (!TryGetTile(out var tile))
            {
                _selection.Clear();
                return;
            }

            var human = _turnState.Human;
            Army army = null;
            if (human != null)
            {
                var own = tile.GetArmiesOf(human.Id);
                if (own.Count > 0)
                {
                    // Clicking the same province again cycles through own armies standing there.
                    var index = _selection.SelectedTile == tile && _selection.SelectedArmy != null
                        ? (own.IndexOf(_selection.SelectedArmy) + 1) % own.Count
                        : 0;
                    army = own[index];
                }
            }
            if (army == null && tile.Armies.Count > 0) army = tile.Armies[0];
            _selection.Select(tile, army);
        }

        private void HandleMove()
        {
            var army = _selection.SelectedArmy;
            var human = _turnState.Human;
            if (army == null || human == null || army.OwnerId != human.Id || !_turnState.CanAct) return;
            if (!TryGetTile(out var target) || !_movement.CanMove(army, target)) return;

            _processor.Enqueue(new MoveArmyCommand(human.Id, army.Id, target.Id));
        }

        private bool TryGetTile(out GameTile tile)
        {
            tile = null;
            var ray = _camera.Camera.ScreenPointToRay(_input.PointerPosition);
            if (!_mapView.Terrain.TryRaycastProvince(ray, out var id, out _)) return false;
            tile = _map.Get(id);
            return tile != null;
        }
    }
}
