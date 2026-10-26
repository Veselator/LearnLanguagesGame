using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.UI.Global
{
    public sealed class NotificationItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _lifetime = 5f;

        private float _age;
        private System.Action<NotificationItemView> _onExpired;

        public void Show(string text, System.Action<NotificationItemView> onExpired)
        {
            _text.text = text;
            _age = 0f;
            _group.alpha = 1f;
            _onExpired = onExpired;
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Clamp01((_lifetime - _age) / 0.5f);
            if (_age >= _lifetime) _onExpired?.Invoke(this);
        }

        public sealed class Pool : MonoMemoryPool<NotificationItemView>
        {
        }
    }
}
