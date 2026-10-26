using LearnLanguages.Battle.Logic;
using LearnLanguages.Cameras;
using LearnLanguages.Core.Events;
using LearnLanguages.Map.Visual;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.Battle.Visual
{
    /// <summary>
    /// Entering a battle: the main camera zooms into the province while a second camera, already standing at the
    /// battlefield, renders into a texture that fades in over the screen. Then the main camera takes its place.
    /// </summary>
    public sealed class BattleTransitionView : MonoBehaviour
    {
        [SerializeField] private Camera _battlefieldCamera;
        [SerializeField] private RawImage _screen;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _duration = 1.3f;
        [SerializeField] private float _zoomDistance = 18f;

        private IEventBus _eventBus;
        private CameraController _cameraController;
        private BattleView _battleView;
        private GameMapView _mapView;
        private BattleCameraMethod _battleCamera;
        private TransitionCameraMethod _transition;
        private CameraSettings _settings;
        private RenderTexture _texture;
        private bool _running;

        [Inject]
        public void Construct(IEventBus eventBus, CameraController cameraController, BattleView battleView,
            GameMapView mapView, BattleCameraMethod battleCamera, TransitionCameraMethod transition, CameraSettings settings)
        {
            _eventBus = eventBus;
            _cameraController = cameraController;
            _battleView = battleView;
            _mapView = mapView;
            _battleCamera = battleCamera;
            _transition = transition;
            _settings = settings;
            _eventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            _eventBus.Subscribe<BattleClosedEvent>(OnBattleClosed);
            Hide();
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            _eventBus?.Unsubscribe<BattleClosedEvent>(OnBattleClosed);
            if (_texture != null) _texture.Release();
        }

        private void OnBattleStarted(BattleStartedEvent e)
        {
            var setup = e.Model.Setup;
            var startColumn = setup.Attacker.IsHuman ? 4 : BattleGrid.Columns - 5;
            _battleCamera.Configure(_battleView.transform.position, 5f, _battleView.FieldWidth - 5f,
                startColumn * _battleView.CellWidth, _battleView.RowsCenterY);

            _battleCamera.Apply(_battlefieldCamera);
            _battlefieldCamera.targetTexture = GetTexture();
            _battlefieldCamera.enabled = true;
            _screen.texture = _texture;
            _group.alpha = 0f;
            _screen.gameObject.SetActive(true);

            var rotation = Quaternion.Euler(_settings.MapPitch, 0f, 0f);
            var target = _mapView.GetTileCenter(setup.Tile) - rotation * Vector3.forward * _zoomDistance;
            _transition.Configure(target, rotation, _duration);
            _cameraController.SetMethod(_transition);
            _running = true;
        }

        // Retreating during the flight must not drag the camera into a finished battle.
        private void OnBattleClosed(BattleClosedEvent e)
        {
            _running = false;
            Hide();
        }

        private void Update()
        {
            if (!_running) return;
            _group.alpha = Mathf.SmoothStep(0f, 1f, _transition.Progress);
            if (!_transition.IsComplete) return;

            _running = false;
            _cameraController.SetMethod(_battleCamera);
            Hide();
        }

        private void Hide()
        {
            _group.alpha = 0f;
            _screen.gameObject.SetActive(false);
            _battlefieldCamera.enabled = false;
            _battlefieldCamera.targetTexture = null;
        }

        private RenderTexture GetTexture()
        {
            if (_texture != null && _texture.width == Screen.width && _texture.height == Screen.height) return _texture;
            if (_texture != null) _texture.Release();
            _texture = new RenderTexture(Screen.width, Screen.height, 24) { name = "BattleTransition" };
            return _texture;
        }
    }
}
