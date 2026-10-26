using System;
using UnityEngine;

namespace LearnLanguages.UI.Common
{
    public interface IScreenFader
    {
        /// <summary>Fades to black, invokes <paramref name="onDark"/>, then fades back in.</summary>
        void Transition(Action onDark);
    }

    public sealed class ScreenFader : MonoBehaviour, IScreenFader
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _fadeDuration = 0.35f;

        private Action _onDark;
        private int _direction;

        public void Transition(Action onDark)
        {
            if (_direction > 0)
            {
                // Already fading out: run both actions once the screen is dark.
                var previous = _onDark;
                _onDark = () =>
                {
                    previous?.Invoke();
                    onDark?.Invoke();
                };
                return;
            }
            _onDark = onDark;
            _direction = 1;
            _group.blocksRaycasts = true;
        }

        private void Awake()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        private void Update()
        {
            if (_direction == 0) return;
            _group.alpha = Mathf.Clamp01(_group.alpha + _direction * Time.unscaledDeltaTime / _fadeDuration);

            if (_direction > 0 && _group.alpha >= 1f)
            {
                var action = _onDark;
                _onDark = null;
                action?.Invoke();
                _direction = -1;
            }
            else if (_direction < 0 && _group.alpha <= 0f)
            {
                _direction = 0;
                _group.blocksRaycasts = false;
            }
        }
    }
}
