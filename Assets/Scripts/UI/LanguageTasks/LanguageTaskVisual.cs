using LearnLanguages.Learning.Tasks;
using LearnLanguages.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.LanguageTasks
{
    /// <summary>
    /// Modal panel showing the current language exercise, its timer and the result.
    /// Picks the sub-view by visiting the task data.
    /// </summary>
    public sealed class LanguageTaskVisual : MonoBehaviour, ILanguageTaskView, ILanguageTaskVisitor
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _context;
        [SerializeField] private TMP_Text _instruction;
        [SerializeField] private TMP_Text _prompt;
        [SerializeField] private Image _timerFill;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private ChoiceTaskView _choiceView;
        [SerializeField] private TextInputTaskView _inputView;
        [SerializeField] private GameObject _resultRoot;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private Color _correctColor = new Color(0.35f, 0.85f, 0.35f);
        [SerializeField] private Color _wrongColor = new Color(0.9f, 0.3f, 0.3f);

        private ILocalizationService _localization;
        private ILanguageTaskAnswerSink _answers;

        [Inject]
        public void Construct(ILocalizationService localization) => _localization = localization;

        public void Show(BasicLanguageTaskData task, string contextText, ILanguageTaskAnswerSink answers)
        {
            _answers = answers;
            _root.SetActive(true);
            _resultRoot.SetActive(false);
            _context.text = contextText;
            _instruction.text = _localization.Get(task.InstructionKey);
            _prompt.text = task.Prompt;
            _prompt.gameObject.SetActive(!string.IsNullOrEmpty(task.Prompt));
            _choiceView.Hide();
            _inputView.Hide();
            task.Accept(this);
        }

        public void Visit(ChoiceTaskData task) => _choiceView.Show(task, _answers);

        public void Visit(TextInputTaskData task) => _inputView.Show(_answers);

        public void SetTimer(float remainingSeconds, float totalSeconds)
        {
            _timerFill.fillAmount = totalSeconds > 0f ? remainingSeconds / totalSeconds : 0f;
            _timerText.text = Mathf.CeilToInt(remainingSeconds).ToString();
        }

        public void ShowResult(bool correct, string correctAnswer)
        {
            _choiceView.Lock();
            _inputView.Lock();
            _resultRoot.SetActive(true);
            _resultText.color = correct ? _correctColor : _wrongColor;
            _resultText.text = correct
                ? _localization.Get("task.correct")
                : _localization.Format("task.wrong", correctAnswer);
        }

        public void Hide()
        {
            _answers = null;
            _root.SetActive(false);
        }

        private void Awake() => _root.SetActive(false);
    }
}
