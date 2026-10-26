using LearnLanguages.Battle.Logic;
using LearnLanguages.Cameras;
using LearnLanguages.Core.Input;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Battle.Visual
{
    /// <summary>
    /// Clicks on the battle lines in the scene; they go through the same <see cref="IBattleSelection"/> as the grid panel.
    /// </summary>
    public sealed class BattleInputController : ITickable
    {
        private const float MaxRayDistance = 200f;

        private readonly IInputService _input;
        private readonly CameraController _camera;
        private readonly BattleView _view;
        private readonly IBattleSelection _selection;
        private readonly IBattleController _controller;

        public BattleInputController(IInputService input, CameraController camera, BattleView view,
            IBattleSelection selection, IBattleController controller)
        {
            _input = input;
            _camera = camera;
            _view = view;
            _selection = selection;
            _controller = controller;
        }

        public void Tick()
        {
            if (!_controller.IsRunning || _controller.Model == null || _input.IsPointerOverUi) return;

            if (_input.SecondaryClicked)
            {
                _selection.Select(null);
                return;
            }
            if (!_input.PrimaryClicked) return;

            var ray = _camera.Camera.ScreenPointToRay(_input.PointerPosition);
            if (Physics.Raycast(ray, out var hit, MaxRayDistance) && _view.TryGetCell(hit.collider, out var cell))
                _selection.Click(cell);
        }
    }
}
