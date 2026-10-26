using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Battle.Visual
{
    /// <summary>
    /// Rising, fading number above a cohort (losses, shouts).
    /// </summary>
    public sealed class FloatingTextView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _lifetime = 1.1f;
        [SerializeField] private float _riseSpeed = 0.8f;

        private float _age;
        private Pool _pool;

        public void Show(Pool pool, Vector3 position, string text, Color color)
        {
            _pool = pool;
            _age = 0f;
            transform.position = position;
            _text.text = text;
            _text.color = color;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);
            var color = _text.color;
            color.a = 1f - _age / _lifetime;
            _text.color = color;
            if (_age >= _lifetime && _pool != null)
            {
                var pool = _pool;
                _pool = null;
                pool.Despawn(this);
            }
        }

        public sealed class Pool : MonoMemoryPool<FloatingTextView>
        {
        }
    }
}
