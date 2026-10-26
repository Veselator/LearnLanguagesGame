using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Localization;
using LearnLanguages.UI.Common;
using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.UI.Global
{
    /// <summary>
    /// Announces whose turn it is and shows that the AI is thinking.
    /// </summary>
    public sealed class TurnBannerView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _banner;
        [SerializeField] private TMP_Text _bannerText;
        [SerializeField] private GameObject _aiIndicator;
        [SerializeField] private TMP_Text _aiIndicatorText;
        [SerializeField] private float _bannerDuration = 1.8f;

        private IEventBus _eventBus;
        private ILocalizationService _localization;
        private float _timer;

        [Inject]
        public void Construct(IEventBus eventBus, ILocalizationService localization)
        {
            _eventBus = eventBus;
            _localization = localization;
            _eventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
            _banner.alpha = 0f;
            _aiIndicator.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            _eventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void OnTurnStarted(TurnStartedEvent e)
        {
            var name = UiFormat.Colored(e.Player.Name, e.Player.Color);
            _bannerText.text = _localization.Format("ui.turn_banner", name);
            _timer = _bannerDuration;
            _aiIndicator.SetActive(!e.Player.IsHuman);
            _aiIndicatorText.text = _localization.Format("ui.ai_thinking", name);
        }

        private void OnGameOver(GameOverEvent e) => _aiIndicator.SetActive(false);

        private void Update()
        {
            if (_timer <= 0f) return;
            _timer -= Time.unscaledDeltaTime;
            _banner.alpha = Mathf.Clamp01(_timer / 0.4f);
        }
    }
}
