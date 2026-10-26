using UnityEngine;

namespace LearnLanguages.Cameras
{
    /// <summary>
    /// Smoothly flies the camera from its current pose to a target pose (used for the zoom into a battle).
    /// </summary>
    public sealed class TransitionCameraMethod : ICurrentCameraMethod
    {
        private Vector3 _fromPosition;
        private Quaternion _fromRotation;
        private Vector3 _toPosition;
        private Quaternion _toRotation;
        private float _duration = 1f;
        private float _elapsed;

        public float Progress => Mathf.Clamp01(_elapsed / _duration);
        public bool IsComplete => _elapsed >= _duration;

        public void Configure(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            _toPosition = targetPosition;
            _toRotation = targetRotation;
            _duration = Mathf.Max(0.01f, duration);
        }

        public void Enter(Camera camera)
        {
            _fromPosition = camera.transform.position;
            _fromRotation = camera.transform.rotation;
            _elapsed = 0f;
        }

        public void Update(Camera camera, float deltaTime)
        {
            _elapsed += deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, Progress);
            camera.transform.SetPositionAndRotation(Vector3.Lerp(_fromPosition, _toPosition, t),
                Quaternion.Slerp(_fromRotation, _toRotation, t));
        }
    }
}
