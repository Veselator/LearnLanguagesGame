using LearnLanguages.Learning.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LearnLanguages.UI.LanguageTasks
{
    public sealed class TextInputTaskView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private Button _submit;

        private ILanguageTaskAnswerSink _answers;

        public void Show(ILanguageTaskAnswerSink answers)
        {
            _answers = answers;
            gameObject.SetActive(true);
            _input.text = string.Empty;
            _input.interactable = true;
            _submit.interactable = true;
            _input.Select();
            _input.ActivateInputField();
        }

        public void Lock()
        {
            _input.interactable = false;
            _submit.interactable = false;
        }

        public void Hide()
        {
            _answers = null;
            gameObject.SetActive(false);
        }

        private void Submit()
        {
            if (_answers == null || !_input.interactable) return;
            _answers.SubmitText(_input.text);
        }

        private void Awake()
        {
            _submit.onClick.AddListener(Submit);
            _input.onSubmit.AddListener(_ => Submit());
        }
    }
}
