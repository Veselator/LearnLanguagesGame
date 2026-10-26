using UnityEngine;

namespace LearnLanguages.Cameras
{
    [CreateAssetMenu(menuName = "LearnLanguages/Camera Settings", fileName = "CameraSettings")]
    public sealed class CameraSettings : ScriptableObject
    {
        [Header("Global map")]
        [SerializeField] private float _mapPitch = 55f;
        [SerializeField] private float _mapPanSpeed = 60f;
        [SerializeField] private float _mapZoomSpeed = 25f;
        [SerializeField] private float _mapMinHeight = 25f;
        [SerializeField] private float _mapMaxHeight = 140f;
        [SerializeField] private float _mapStartHeight = 70f;
        [SerializeField] private float _edgePanBorder = 8f;
        [SerializeField] private float _dragSpeed = 0.15f;

        [Header("Battle")]
        [SerializeField] private float _battlePanSpeed = 10f;
        [SerializeField] private float _battleZoomSpeed = 4f;
        [Tooltip("Orthographic size range of the battle camera.")]
        [SerializeField] private float _battleMinZoom = 2.6f;
        [SerializeField] private float _battleMaxZoom = 5.5f;
        [Tooltip("How far (in camera sizes) the camera sits below the lines, leaving room for the command panel.")]
        [SerializeField] private float _battleVerticalOffset = 0.22f;
        [SerializeField] private float _smoothing = 10f;

        public float MapPitch => _mapPitch;
        public float MapPanSpeed => _mapPanSpeed;
        public float MapZoomSpeed => _mapZoomSpeed;
        public float MapMinHeight => _mapMinHeight;
        public float MapMaxHeight => _mapMaxHeight;
        public float MapStartHeight => _mapStartHeight;
        public float EdgePanBorder => _edgePanBorder;
        public float DragSpeed => _dragSpeed;
        public float BattlePanSpeed => _battlePanSpeed;
        public float BattleZoomSpeed => _battleZoomSpeed;
        public float BattleMinZoom => _battleMinZoom;
        public float BattleMaxZoom => _battleMaxZoom;
        public float BattleVerticalOffset => _battleVerticalOffset;
        public float Smoothing => _smoothing;
    }
}
