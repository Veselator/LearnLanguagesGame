using System;
using System.Collections.Generic;
using LearnLanguages.Core.Events;
using LearnLanguages.Settings;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Learning.Tasks
{
    public sealed class LanguageTaskRequest
    {
        public LanguageTaskRequest(string contextText, Action<bool> completed)
        {
            ContextText = contextText;
            Completed = completed;
        }

        /// <summary>Short already-localized description of why the exercise appears, e.g. "Swordsmen attack Archers".</summary>
        public string ContextText { get; }
        public Action<bool> Completed { get; }
    }

    public interface ILanguageTaskService
    {
        bool IsBusy { get; }
        void Request(LanguageTaskRequest request);
    }

    public interface ILanguageTaskAnswerSink
    {
        void SubmitChoice(int optionIndex);
        void SubmitText(string text);
    }

    public interface ILanguageTaskView
    {
        void Show(BasicLanguageTaskData task, string contextText, ILanguageTaskAnswerSink answers);
        void SetTimer(float remainingSeconds, float totalSeconds);
        void ShowResult(bool correct, string correctAnswer);
        void Hide();
    }

    public readonly struct LanguageTaskFinishedEvent
    {
        public LanguageTaskFinishedEvent(LanguageTaskType type, bool correct)
        {
            Type = type;
            Correct = correct;
        }

        public LanguageTaskType Type { get; }
        public bool Correct { get; }
    }

    public interface ILanguageStatistics
    {
        int Answered { get; }
        int Correct { get; }
        void Restore(int answered, int correct);
    }

    /// <summary>
    /// Queues exercise requests, runs the timer and evaluates answers. Presentation is delegated to <see cref="ILanguageTaskView"/>.
    /// </summary>
    public sealed class InGameLanguageTasksManager : ILanguageTaskService, ILanguageTaskAnswerSink, ILanguageStatistics, ITickable
    {
        private enum State
        {
            Idle,
            Answering,
            ShowingResult
        }

        private readonly Queue<LanguageTaskRequest> _queue = new Queue<LanguageTaskRequest>();
        private readonly LanguageTasksHandler _handler;
        private readonly ILanguageTaskView _view;
        private readonly IGameSession _session;
        private readonly IEventBus _eventBus;
        private readonly float _resultDisplayTime;

        private State _state;
        private BasicLanguageTaskData _task;
        private LanguageTaskRequest _request;
        private float _remaining;
        private float _resultTimer;
        private bool _lastResult;

        public InGameLanguageTasksManager(LanguageTasksHandler handler, ILanguageTaskView view, IGameSession session,
            IEventBus eventBus, Data.LanguageConfig config)
        {
            _handler = handler;
            _view = view;
            _session = session;
            _eventBus = eventBus;
            _resultDisplayTime = config.TasksSettings.ResultDisplayTime;
        }

        public bool IsBusy => _state != State.Idle || _queue.Count > 0;
        public int Answered { get; private set; }
        public int Correct { get; private set; }

        public void Restore(int answered, int correct)
        {
            Answered = answered;
            Correct = correct;
        }

        public void Request(LanguageTaskRequest request)
        {
            _queue.Enqueue(request);
            if (_state == State.Idle) StartNext();
        }

        public void SubmitChoice(int optionIndex)
        {
            if (_state == State.Answering && _task is ChoiceTaskData choice) Finish(choice.IsCorrect(optionIndex));
        }

        public void SubmitText(string text)
        {
            if (_state == State.Answering && _task is TextInputTaskData input) Finish(input.IsCorrect(text));
        }

        public void Tick()
        {
            var delta = Time.unscaledDeltaTime;
            switch (_state)
            {
                case State.Answering:
                    _remaining -= delta;
                    _view.SetTimer(Mathf.Max(0f, _remaining), _task.TimeLimit);
                    if (_remaining <= 0f) Finish(false);
                    break;
                case State.ShowingResult:
                    _resultTimer -= delta;
                    if (_resultTimer <= 0f) Complete();
                    break;
            }
        }

        private void StartNext()
        {
            if (_queue.Count == 0)
            {
                _state = State.Idle;
                _view.Hide();
                return;
            }

            _request = _queue.Dequeue();
            _task = _handler.GetRandomLanguageTask(_session.Settings);
            _remaining = _task.TimeLimit;
            _state = State.Answering;
            _view.Show(_task, _request.ContextText, this);
            _view.SetTimer(_remaining, _task.TimeLimit);
        }

        private void Finish(bool correct)
        {
            _lastResult = correct;
            Answered++;
            if (correct) Correct++;
            _state = State.ShowingResult;
            _resultTimer = _resultDisplayTime;
            _view.ShowResult(correct, _task.CorrectAnswerText);
            _eventBus.Publish(new LanguageTaskFinishedEvent(_task.Type, correct));
        }

        private void Complete()
        {
            var request = _request;
            _request = null;
            _task = null;
            _state = State.Idle;
            if (_queue.Count == 0) _view.Hide();
            request.Completed?.Invoke(_lastResult);
            if (_state == State.Idle) StartNext();
        }
    }
}
