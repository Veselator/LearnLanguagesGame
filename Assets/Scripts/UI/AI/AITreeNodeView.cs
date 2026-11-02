using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.AI
{
    public enum AITreeNodeState
    {
        Waiting,
        Current,
        OnPath,
        Finished,
        Pruned,
        Chosen
    }

    /// <summary>
    /// A node of the displayed decision tree: the option it stands for and the value the search returned for it.
    /// </summary>
    public sealed class AITreeNodeView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private Color _waiting = new Color(0.62f, 0.65f, 0.7f);
        [SerializeField] private Color _current = new Color(0.98f, 0.8f, 0.25f);
        [SerializeField] private Color _onPath = new Color(0.45f, 0.66f, 0.95f);
        [SerializeField] private Color _finished = new Color(0.82f, 0.85f, 0.9f);
        [SerializeField] private Color _pruned = new Color(0.82f, 0.52f, 0.5f);
        [SerializeField] private Color _chosen = new Color(0.45f, 0.85f, 0.45f);

        public RectTransform Rect => _rect;

        public void Bind(string label, string value, AITreeNodeState state, Vector2 position, Vector2 size)
        {
            _label.text = label;
            _value.text = value;
            _background.color = ColorOf(state);
            _rect.anchoredPosition = position;
            _rect.sizeDelta = size;
        }

        private Color ColorOf(AITreeNodeState state)
        {
            switch (state)
            {
                case AITreeNodeState.Current: return _current;
                case AITreeNodeState.OnPath: return _onPath;
                case AITreeNodeState.Finished: return _finished;
                case AITreeNodeState.Pruned: return _pruned;
                case AITreeNodeState.Chosen: return _chosen;
                default: return _waiting;
            }
        }

        public sealed class Pool : MonoMemoryPool<AITreeNodeView> { }
    }
}
