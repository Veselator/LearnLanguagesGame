using System;
using System.Collections;
using LearnLanguages.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnLanguages.UI.LanguageTasks
{
    /// <summary>
    /// Animations of the exercise panel: the window appears and disappears by scale, the dimmer under it
    /// fades to its target, and an answer is answered with a pulse or a shake and a flash of color.
    /// </summary>
    public sealed class LanguageTaskPanelAnimator : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private RectTransform _window;
        [SerializeField] private float _appearDuration = 0.35f;
        [SerializeField] private float _disappearDuration = 0.2f;

        [Header("Dimmer")]
        [SerializeField] private Image _dimmer;
        [Tooltip("Alpha of the dimmer when it appears; it animates from here to the target.")]
        [Range(0f, 1f)]
        [SerializeField] private float _dimmerStartAlpha = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float _dimmerTargetAlpha = 0.75f;
        [SerializeField] private float _dimmerDuration = 0.45f;

        [Header("Answer")]
        [SerializeField] private Image _flash;
        [SerializeField] private RectTransform _result;
        [SerializeField] private Color _correctFlash = new Color(0.35f, 0.9f, 0.35f, 0.45f);
        [SerializeField] private Color _wrongFlash = new Color(0.95f, 0.25f, 0.25f, 0.45f);
        [SerializeField] private float _correctDuration = 0.45f;
        [SerializeField] private float _correctPulseScale = 0.08f;
        [SerializeField] private float _wrongDuration = 0.5f;
        [SerializeField] private float _wrongShakeDistance = 26f;
        [SerializeField] private float _wrongShakes = 4f;

        private Vector2 _windowPosition;
        private Coroutine _windowRoutine;
        private Coroutine _dimmerRoutine;
        private Coroutine _flashRoutine;
        private bool _isDimmed;

        private void Awake()
        {
            _windowPosition = _window.anchoredPosition;
            SetFlash(Color.clear);
        }

        public void PlayAppear()
        {
            ResetWindow();
            SetFlash(Color.clear);
            RunWindow(UiEasing.Animate(_appearDuration, t => _window.localScale = Vector3.one * UiEasing.OutBack(t)));
            if (_isDimmed) return;
            _isDimmed = true;
            RunDimmer(_dimmerStartAlpha, _dimmerTargetAlpha, _dimmerDuration);
        }

        /// <param name="keepDimmer">Another exercise follows right away: the dimmer stays for it.</param>
        public void PlayDisappear(bool keepDimmer, Action onFinished)
        {
            ResetWindow();
            if (!keepDimmer)
            {
                _isDimmed = false;
                RunDimmer(_dimmer.color.a, 0f, _disappearDuration);
            }
            RunWindow(Disappear(onFinished));
        }

        public void PlayCorrect()
        {
            ResetWindow();
            RunWindow(UiEasing.Animate(_correctDuration,
                t => _window.localScale = Vector3.one * (1f + _correctPulseScale * UiEasing.Pulse(t))));
            RunFlash(_correctFlash, _correctDuration);
            PopResult();
        }

        public void PlayWrong()
        {
            ResetWindow();
            RunWindow(UiEasing.Animate(_wrongDuration, t =>
                _window.anchoredPosition = _windowPosition +
                                           Vector2.right * (_wrongShakeDistance * UiEasing.DampedShake(t, _wrongShakes))));
            RunFlash(_wrongFlash, _wrongDuration);
            PopResult();
        }

        private IEnumerator Disappear(Action onFinished)
        {
            yield return UiEasing.Animate(_disappearDuration, t => _window.localScale = Vector3.one * (1f - UiEasing.InBack(t)));
            _window.localScale = Vector3.zero;
            onFinished?.Invoke();
        }

        private void PopResult() => StartCoroutine(UiEasing.Animate(_appearDuration,
            t => _result.localScale = Vector3.one * UiEasing.OutBack(t)));

        private void ResetWindow()
        {
            if (_windowRoutine != null) StopCoroutine(_windowRoutine);
            _window.anchoredPosition = _windowPosition;
            _window.localScale = Vector3.one;
        }

        private void RunWindow(IEnumerator routine) => _windowRoutine = StartCoroutine(routine);

        private void RunDimmer(float from, float to, float duration)
        {
            if (_dimmerRoutine != null) StopCoroutine(_dimmerRoutine);
            _dimmerRoutine = StartCoroutine(UiEasing.Animate(duration, t =>
            {
                var color = _dimmer.color;
                color.a = Mathf.Lerp(from, to, UiEasing.OutCubic(t));
                _dimmer.color = color;
            }));
        }

        private void RunFlash(Color color, float duration)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(UiEasing.Animate(duration, t =>
                SetFlash(new Color(color.r, color.g, color.b, color.a * (1f - UiEasing.OutCubic(t))))));
        }

        private void SetFlash(Color color) => _flash.color = color;
    }
}
