using UnityEngine;

namespace LearnLanguages.Map.Visual
{
    /// <summary>
    /// Plays sprite frames back and forth (0-1-2-1-0...), e.g. a waving banner.
    /// </summary>
    public sealed class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _framesPerSecond = 6f;

        private float _time;

        private void OnEnable() => _time = Random.value * 10f;

        private void Update()
        {
            if (_frames == null || _frames.Length == 0) return;
            _time += Time.deltaTime * _framesPerSecond;
            var cycle = Mathf.Max(1, _frames.Length * 2 - 2);
            var step = (int)_time % cycle;
            _renderer.sprite = _frames[step < _frames.Length ? step : cycle - step];
        }
    }
}
