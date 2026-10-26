using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Localization
{
    /// <summary>
    /// Keeps a static TMP label in sync with the interface language.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private TMP_Text _text;

        private ILocalizationService _localization;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;
            _localization.LanguageChanged += Refresh;
            Refresh();
        }

        public void SetKey(string key)
        {
            _key = key;
            Refresh();
        }

        private void Refresh()
        {
            if (_localization != null && _text != null) _text.text = _localization.Get(_key);
        }

        private void OnDestroy()
        {
            if (_localization != null) _localization.LanguageChanged -= Refresh;
        }

        private void Reset() => _text = GetComponent<TMP_Text>();
    }
}
