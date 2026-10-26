using LearnLanguages.Core.Input;
using LearnLanguages.Save;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Global
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _resume;
        [SerializeField] private Button _save;
        [SerializeField] private Button _mainMenu;
        [SerializeField] private Button _quit;

        private IGameSaver _saver;
        private ISceneLoader _scenes;
        private IInputService _input;

        [Inject]
        public void Construct(IGameSaver saver, ISceneLoader scenes, IInputService input)
        {
            _saver = saver;
            _scenes = scenes;
            _input = input;
        }

        public void Toggle() => _root.SetActive(!_root.activeSelf);

        private void Awake()
        {
            _root.SetActive(false);
            _resume.onClick.AddListener(Toggle);
            _save.onClick.AddListener(() =>
            {
                _saver.SaveNow();
                Toggle();
            });
            _mainMenu.onClick.AddListener(() => _scenes.LoadMainMenu());
            _quit.onClick.AddListener(() => _scenes.Quit());
        }

        private void Update()
        {
            if (_input.CancelPressed) Toggle();
            if (_root.activeSelf) _save.interactable = _saver.CanSave;
        }
    }
}
