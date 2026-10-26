using System;
using LearnLanguages.Battle.Visual;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace LearnLanguages.UI.Battle
{
    /// <summary>
    /// One cell of the 6x30 command grid (1066-style panel).
    /// </summary>
    public sealed class BattleGridCellView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _highlight;
        [SerializeField] private Image _marker;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Color _reachableColor = new Color(0.3f, 1f, 0.3f, 0.45f);
        [SerializeField] private Color _attackColor = new Color(1f, 0.25f, 0.2f, 0.7f);
        [SerializeField] private Color _shootColor = new Color(1f, 0.6f, 0.1f, 0.7f);
        [SerializeField] private Color _deployColor = new Color(0.4f, 0.7f, 1f, 0.35f);
        [SerializeField] private Color _selectedColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color _pathColor = new Color(1f, 1f, 0.4f, 0.45f);

        private Action<Vector2Int> _onClick;

        public Vector2Int Cell { get; private set; }

        public void Bind(Vector2Int cell, Color groundColor, Action<Vector2Int> onClick)
        {
            Cell = cell;
            _background.color = groundColor;
            _onClick = onClick;
            SetCohort(false, Color.clear, string.Empty);
            SetHighlight(CellHighlight.None);
        }

        public void SetCohort(bool present, Color color, string label)
        {
            _marker.gameObject.SetActive(present);
            _marker.color = color;
            _label.text = label;
        }

        public void SetHighlight(CellHighlight highlight)
        {
            _highlight.gameObject.SetActive(highlight != CellHighlight.None);
            switch (highlight)
            {
                case CellHighlight.Reachable: _highlight.color = _reachableColor; break;
                case CellHighlight.Attack: _highlight.color = _attackColor; break;
                case CellHighlight.Shoot: _highlight.color = _shootColor; break;
                case CellHighlight.Deploy: _highlight.color = _deployColor; break;
                case CellHighlight.Selected: _highlight.color = _selectedColor; break;
                case CellHighlight.Path: _highlight.color = _pathColor; break;
            }
        }

        private void Awake() => _button.onClick.AddListener(() => _onClick?.Invoke(Cell));

        public sealed class Pool : MonoMemoryPool<BattleGridCellView>
        {
        }
    }
}
