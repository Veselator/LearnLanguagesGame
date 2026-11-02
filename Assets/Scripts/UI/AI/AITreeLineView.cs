using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.AI
{
    /// <summary>
    /// An edge of the displayed decision tree.
    /// </summary>
    public sealed class AITreeLineView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _image;
        [SerializeField] private float _thickness = 2f;

        public void Bind(Vector2 from, Vector2 to, Color color)
        {
            var delta = to - from;
            _rect.anchoredPosition = (from + to) * 0.5f;
            _rect.sizeDelta = new Vector2(delta.magnitude, _thickness);
            _rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _image.color = color;
        }

        public sealed class Pool : MonoMemoryPool<AITreeLineView> { }
    }
}
