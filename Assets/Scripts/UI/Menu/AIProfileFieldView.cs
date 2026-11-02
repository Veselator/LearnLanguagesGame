using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Menu
{
    /// <summary>
    /// A number of an AI model: label, input field and, for weights, the share of the whole after normalization.
    /// </summary>
    public sealed class AIProfileFieldView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_InputField _input;
        [SerializeField] private GameObject _shareRoot;
        [SerializeField] private Image _shareBar;
        [SerializeField] private TMP_Text _shareText;

        private Action<float> _onChanged;

        private void Awake() => _input.onEndEdit.AddListener(OnEndEdit);

        public void Bind(string label, float value, Action<float> onChanged)
        {
            Show(label, value, onChanged);
            _shareRoot.SetActive(false);
        }

        public void Bind(string label, float value, float share, Action<float> onChanged)
        {
            Show(label, value, onChanged);
            _shareRoot.SetActive(true);
            _shareBar.fillAmount = share;
            _shareText.text = share.ToString("P0", CultureInfo.InvariantCulture);
        }

        private void Show(string label, float value, Action<float> onChanged)
        {
            _label.text = label;
            _input.SetTextWithoutNotify(value.ToString("0.##", CultureInfo.InvariantCulture));
            _onChanged = onChanged;
        }

        private void OnEndEdit(string text)
        {
            // Both separators are accepted, whatever the keyboard layout is.
            if (float.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                _onChanged?.Invoke(value);
            else
                _onChanged?.Invoke(float.NaN);
        }

        public sealed class Pool : MonoMemoryPool<AIProfileFieldView>
        {
            protected override void OnDespawned(AIProfileFieldView item)
            {
                item._onChanged = null;
                base.OnDespawned(item);
            }
        }
    }
}
