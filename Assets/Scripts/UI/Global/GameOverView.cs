using LearnLanguages.Core.Events;
using LearnLanguages.Gameplay.Events;
using LearnLanguages.Learning.Tasks;
using LearnLanguages.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Global
{
    public sealed class GameOverView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _details;
        [SerializeField] private Button _mainMenu;

        private IEventBus _eventBus;
        private ILocalizationService _localization;
        private ILanguageStatistics _statistics;
        private ISceneLoader _scenes;

        [Inject]
        public void Construct(IEventBus eventBus, ILocalizationService localization, ILanguageStatistics statistics,
            ISceneLoader scenes)
        {
            _eventBus = eventBus;
            _localization = localization;
            _statistics = statistics;
            _scenes = scenes;
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
            _root.SetActive(false);
        }

        private void Awake() => _mainMenu.onClick.AddListener(() => _scenes.LoadMainMenu());

        private void OnDestroy() => _eventBus?.Unsubscribe<GameOverEvent>(OnGameOver);

        private void OnGameOver(GameOverEvent e)
        {
            _root.SetActive(true);
            _title.text = _localization.Get(e.HumanWon ? "gameover.victory" : "gameover.defeat");
            _details.text = _localization.Format("gameover.statistics", _statistics.Correct, _statistics.Answered);
        }
    }
}
