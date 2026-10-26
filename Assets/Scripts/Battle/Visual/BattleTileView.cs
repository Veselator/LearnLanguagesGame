using UnityEngine;
using Zenject;

namespace LearnLanguages.Battle.Visual
{
    public enum CellHighlight
    {
        None = 0,
        Reachable = 1,
        Attack = 2,
        Shoot = 3,
        Deploy = 4,
        Selected = 5,
        Path = 6
    }

    /// <summary>
    /// One cell of a battle line: ground strip, optional decoration and highlight overlay. Clickable through its collider.
    /// </summary>
    public sealed class BattleTileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _ground;
        [SerializeField] private SpriteRenderer _decoration;
        [SerializeField] private SpriteRenderer _highlight;
        [SerializeField] private Color _reachableColor = new Color(0.3f, 1f, 0.3f, 0.45f);
        [SerializeField] private Color _attackColor = new Color(1f, 0.25f, 0.2f, 0.6f);
        [SerializeField] private Color _shootColor = new Color(1f, 0.6f, 0.1f, 0.6f);
        [SerializeField] private Color _deployColor = new Color(0.4f, 0.7f, 1f, 0.4f);
        [SerializeField] private Color _selectedColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color _pathColor = new Color(1f, 1f, 0.4f, 0.45f);

        public Vector2Int Cell { get; private set; }

        public void Bind(Vector2Int cell, BattleTileVisual visual, int sortingOrder, Color tint)
        {
            Cell = cell;
            _ground.sprite = visual.Ground;
            _ground.color = tint;
            _ground.sortingOrder = sortingOrder;
            _decoration.sprite = visual.Decoration;
            _decoration.gameObject.SetActive(visual.Decoration != null);
            _decoration.sortingOrder = sortingOrder + 2;
            _highlight.sortingOrder = sortingOrder + 1;
            SetHighlight(CellHighlight.None);
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

        public sealed class Pool : MonoMemoryPool<BattleTileView>
        {
        }
    }
}
