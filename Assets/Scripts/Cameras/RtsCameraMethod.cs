using LearnLanguages.Core.Input;
using UnityEngine;

namespace LearnLanguages.Cameras
{
    /// <summary>
    /// Classic RTS camera over the 3D map: keyboard/edge/middle-drag panning and height zoom. No rotation.
    /// </summary>
    public sealed class RtsCameraMethod : ICurrentCameraMethod
    {
        private readonly IInputService _input;
        private readonly CameraSettings _settings;
        private Rect _bounds;
        private Vector3 _target;
        private bool _hasTarget;

        public RtsCameraMethod(IInputService input, CameraSettings settings)
        {
            _input = input;
            _settings = settings;
        }

        /// <summary>Area (x, z) the camera focus may move within.</summary>
        public void SetBounds(Rect bounds) => _bounds = bounds;

        public void FocusOn(Vector3 worldPoint)
        {
            var height = _hasTarget ? _target.y : _settings.MapStartHeight;
            _target = new Vector3(worldPoint.x, height, worldPoint.z - OffsetForHeight(height));
            _hasTarget = true;
            Clamp();
        }

        public void Enter(Camera camera)
        {
            camera.orthographic = false;
            var cameraTransform = camera.transform;
            cameraTransform.rotation = Quaternion.Euler(_settings.MapPitch, 0f, 0f);
            if (!_hasTarget) FocusOn(new Vector3(_bounds.center.x, 0f, _bounds.center.y));
            cameraTransform.position = _target;
        }

        public void Update(Camera camera, float deltaTime)
        {
            var cameraTransform = camera.transform;
            var heightFactor = _target.y / _settings.MapStartHeight;
            var move = _input.MoveAxis;

            if (!_input.IsPointerOverUi)
            {
                var pointer = _input.PointerPosition;
                var border = _settings.EdgePanBorder;
                if (Application.isFocused && pointer.x > 0 && pointer.y > 0 && pointer.x < Screen.width && pointer.y < Screen.height)
                {
                    if (pointer.x < border) move.x -= 1f;
                    else if (pointer.x > Screen.width - border) move.x += 1f;
                    if (pointer.y < border) move.y -= 1f;
                    else if (pointer.y > Screen.height - border) move.y += 1f;
                }

                var scroll = _input.Scroll;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    var newHeight = Mathf.Clamp(_target.y - scroll * _settings.MapZoomSpeed, _settings.MapMinHeight, _settings.MapMaxHeight);
                    _target.z += OffsetForHeight(_target.y) - OffsetForHeight(newHeight);
                    _target.y = newHeight;
                }
            }

            move = Vector2.ClampMagnitude(move, 1f);
            _target.x += move.x * _settings.MapPanSpeed * heightFactor * deltaTime;
            _target.z += move.y * _settings.MapPanSpeed * heightFactor * deltaTime;

            if (_input.MiddleHeld)
            {
                var delta = _input.PointerDelta * _settings.DragSpeed * heightFactor;
                _target.x -= delta.x;
                _target.z -= delta.y;
            }

            Clamp();
            cameraTransform.rotation = Quaternion.Euler(_settings.MapPitch, 0f, 0f);
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, _target, 1f - Mathf.Exp(-_settings.Smoothing * deltaTime));
        }

        private float OffsetForHeight(float height) => height / Mathf.Tan(_settings.MapPitch * Mathf.Deg2Rad);

        private void Clamp()
        {
            var offset = OffsetForHeight(_target.y);
            _target.x = Mathf.Clamp(_target.x, _bounds.xMin, _bounds.xMax);
            _target.z = Mathf.Clamp(_target.z, _bounds.yMin - offset, _bounds.yMax - offset);
        }
    }
}
