using LearnLanguages.Core.Input;
using UnityEngine;

namespace LearnLanguages.Cameras
{
    /// <summary>
    /// Flat 2D battle camera: orthographic, slides along the lines, zooms by its size.
    /// </summary>
    public sealed class BattleCameraMethod : ICurrentCameraMethod
    {
        private const float CameraDistance = 20f;

        private readonly IInputService _input;
        private readonly CameraSettings _settings;
        private Vector3 _origin;
        private float _minX;
        private float _maxX;
        private float _x;
        private float _centerY;
        private float _size;

        public BattleCameraMethod(IInputService input, CameraSettings settings)
        {
            _input = input;
            _settings = settings;
        }

        /// <param name="origin">Battlefield origin (left end of the nearest line).</param>
        /// <param name="centerY">Height of the middle of the lines above the origin.</param>
        public void Configure(Vector3 origin, float minX, float maxX, float startX, float centerY)
        {
            _origin = origin;
            _minX = minX;
            _maxX = maxX;
            _x = Mathf.Clamp(startX, minX, maxX);
            _centerY = centerY;
            _size = (_settings.BattleMinZoom + _settings.BattleMaxZoom) * 0.5f;
        }

        /// <summary>Puts any camera (e.g. the transition camera) exactly where this method starts.</summary>
        public void Apply(Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = _size;
            camera.transform.SetPositionAndRotation(TargetPosition(), Quaternion.identity);
        }

        public void Enter(Camera camera) => Apply(camera);

        public void Update(Camera camera, float deltaTime)
        {
            var move = _input.MoveAxis;
            if (!_input.IsPointerOverUi)
            {
                var pointer = _input.PointerPosition;
                var border = _settings.EdgePanBorder;
                if (Application.isFocused && pointer.y > Screen.height * 0.35f && pointer.y < Screen.height)
                {
                    if (pointer.x > 0 && pointer.x < border) move.x -= 1f;
                    else if (pointer.x < Screen.width && pointer.x > Screen.width - border) move.x += 1f;
                }
                _size -= _input.Scroll * _settings.BattleZoomSpeed * 0.15f;
                if (_input.MiddleHeld) _x -= _input.PointerDelta.x * camera.orthographicSize * 2f / Screen.height;
            }

            _x = Mathf.Clamp(_x + move.x * _settings.BattlePanSpeed * deltaTime, _minX, _maxX);
            _size = Mathf.Clamp(_size - move.y * _settings.BattleZoomSpeed * deltaTime,
                _settings.BattleMinZoom, _settings.BattleMaxZoom);

            var blend = 1f - Mathf.Exp(-_settings.Smoothing * deltaTime);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, _size, blend);
            camera.transform.SetPositionAndRotation(Vector3.Lerp(camera.transform.position, TargetPosition(), blend),
                Quaternion.identity);
        }

        // The lines sit a bit above the screen centre: the command panel covers the lower part of the screen.
        private Vector3 TargetPosition() =>
            new Vector3(_origin.x + _x, _origin.y + _centerY - _size * _settings.BattleVerticalOffset, _origin.z - CameraDistance);
    }
}
