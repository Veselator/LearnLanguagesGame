using System.Collections.Generic;

namespace LearnLanguages.Learning.Tasks
{
    public enum LanguageTaskType
    {
        ChooseTranslation = 0,
        TypeTranslation = 1,
        OddWordOut = 2,
        ChooseTense = 3,
        ConjugateVerb = 4
    }

    public interface ILanguageTaskVisitor
    {
        void Visit(ChoiceTaskData task);
        void Visit(TextInputTaskData task);
    }

    /// <summary>
    /// Base of every language exercise. Describes the exercise only; timing and presentation belong to
    /// <see cref="InGameLanguageTasksManager"/> and its visual.
    /// </summary>
    public abstract class BasicLanguageTaskData
    {
        protected BasicLanguageTaskData(LanguageTaskType type, string instructionKey, string prompt, float timeLimit)
        {
            Type = type;
            InstructionKey = instructionKey;
            Prompt = prompt;
            TimeLimit = timeLimit;
        }

        public LanguageTaskType Type { get; }
        public string InstructionKey { get; }
        public string Prompt { get; }
        public float TimeLimit { get; }
        public abstract string CorrectAnswerText { get; }

        public abstract void Accept(ILanguageTaskVisitor visitor);
    }

    public sealed class ChoiceTaskData : BasicLanguageTaskData
    {
        private readonly string[] _options;

        public ChoiceTaskData(LanguageTaskType type, string instructionKey, string prompt, float timeLimit,
            string[] options, int correctIndex) : base(type, instructionKey, prompt, timeLimit)
        {
            _options = options;
            CorrectIndex = correctIndex;
        }

        public IReadOnlyList<string> Options => _options;
        public int CorrectIndex { get; }
        public override string CorrectAnswerText => _options[CorrectIndex];

        public bool IsCorrect(int optionIndex) => optionIndex == CorrectIndex;

        public override void Accept(ILanguageTaskVisitor visitor) => visitor.Visit(this);
    }

    public sealed class TextInputTaskData : BasicLanguageTaskData
    {
        private readonly string[] _acceptedAnswers;

        public TextInputTaskData(LanguageTaskType type, string instructionKey, string prompt, float timeLimit,
            string[] acceptedAnswers) : base(type, instructionKey, prompt, timeLimit)
        {
            _acceptedAnswers = acceptedAnswers;
        }

        public override string CorrectAnswerText => _acceptedAnswers[0];

        public bool IsCorrect(string answer)
        {
            var normalized = AnswerNormalizer.Normalize(answer);
            var withoutAccents = AnswerNormalizer.StripAccents(normalized);
            foreach (var accepted in _acceptedAnswers)
            {
                var expected = AnswerNormalizer.Normalize(accepted);
                if (normalized == expected || withoutAccents == AnswerNormalizer.StripAccents(expected)) return true;
            }
            return false;
        }

        public override void Accept(ILanguageTaskVisitor visitor) => visitor.Visit(this);
    }
}
