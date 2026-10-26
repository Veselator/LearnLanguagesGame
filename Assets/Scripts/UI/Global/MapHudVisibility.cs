using LearnLanguages.Battle.Logic;
using LearnLanguages.Core.Events;
using UnityEngine;
using Zenject;

namespace LearnLanguages.UI.Global
{
    /// <summary>
    /// Hides the global-map interface while a battle is shown.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class MapHudVisibility : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;

        private IEventBus _eventBus;

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            _eventBus.Subscribe<BattleClosedEvent>(OnBattleClosed);
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            _eventBus.Unsubscribe<BattleClosedEvent>(OnBattleClosed);
        }

        private void OnBattleStarted(BattleStartedEvent e) => SetVisible(false);

        private void OnBattleClosed(BattleClosedEvent e) => SetVisible(true);

        private void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
            _group.interactable = visible;
        }

        private void Reset() => _group = GetComponent<CanvasGroup>();
    }
}
