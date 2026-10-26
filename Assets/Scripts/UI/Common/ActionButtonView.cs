using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Common
{
    /// <summary>
    /// Generic list button with a title and a secondary line (e.g. a cost).
    /// </summary>
    public sealed class ActionButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;

        private Action _onClick;

        public void Bind(string title, string subtitle, bool interactable, Action onClick)
        {
            _title.text = title;
            _subtitle.text = subtitle;
            _subtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            _button.interactable = interactable;
            _onClick = onClick;
        }

        private void Awake() => _button.onClick.AddListener(() => _onClick?.Invoke());

        public sealed class Pool : MonoMemoryPool<ActionButtonView>
        {
            protected override void OnDespawned(ActionButtonView item)
            {
                item._onClick = null;
                base.OnDespawned(item);
            }
        }
    }
}
