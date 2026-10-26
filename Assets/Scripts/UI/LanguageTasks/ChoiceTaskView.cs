using LearnLanguages.Learning.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LearnLanguages.UI.LanguageTasks
{
    public sealed class ChoiceTaskView : MonoBehaviour
    {
        [SerializeField] private Button[] _buttons;
        [SerializeField] private TMP_Text[] _labels;
        [SerializeField] private Image[] _backgrounds;
        [SerializeField] private Color _normalColor = new Color(0.95f, 0.92f, 0.85f);
        [SerializeField] private Color _correctColor = new Color(0.45f, 0.85f, 0.45f);

        private ILanguageTaskAnswerSink _answers;
        private ChoiceTaskData _task;

        public void Show(ChoiceTaskData task, ILanguageTaskAnswerSink answers)
        {
            _task = task;
            _answers = answers;
            gameObject.SetActive(true);
            for (var i = 0; i < _buttons.Length; i++)
            {
                var visible = i < task.Options.Count;
                _buttons[i].gameObject.SetActive(visible);
                _buttons[i].interactable = true;
                _backgrounds[i].color = _normalColor;
                if (visible) _labels[i].text = task.Options[i];
            }
        }

        public void Lock()
        {
            if (_task == null) return;
            for (var i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].interactable = false;
                if (i == _task.CorrectIndex) _backgrounds[i].color = _correctColor;
            }
        }

        public void Hide()
        {
            _task = null;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            for (var i = 0; i < _buttons.Length; i++)
            {
                var index = i;
                _buttons[i].onClick.AddListener(() => _answers?.SubmitChoice(index));
            }
        }
    }
}
