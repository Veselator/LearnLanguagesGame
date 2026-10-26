using UnityEngine;

namespace LearnLanguages.Cameras
{
    /// <summary>
    /// Strategy controlling the camera in the current game mode (also owns its projection).
    /// </summary>
    public interface ICurrentCameraMethod
    {
        void Enter(Camera camera);
        void Update(Camera camera, float deltaTime);
    }

    /// <summary>
    /// Owns the main camera and delegates its movement to the current <see cref="ICurrentCameraMethod"/>.
    /// </summary>
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private ICurrentCameraMethod _method;

        public Camera Camera => _camera;

        public void SetMethod(ICurrentCameraMethod method)
        {
            _method = method;
            _method?.Enter(_camera);
        }

        private void Update() => _method?.Update(_camera, Time.unscaledDeltaTime);
    }
}
