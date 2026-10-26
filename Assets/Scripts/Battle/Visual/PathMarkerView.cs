using UnityEngine;
using Zenject;

namespace LearnLanguages.Battle.Visual
{
    public enum PathMarkerKind
    {
        Step = 0,
        Destination = 1,
        Attack = 2,
        ShootTarget = 3
    }

    /// <summary>
    /// Primitive drawn on the ground of a battle cell: dots along a path, a cross at its end, a ring on a shooting target.
    /// </summary>
    public sealed class PathMarkerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite _dot;
        [SerializeField] private Sprite _cross;
        [SerializeField] private Sprite _ring;
        [SerializeField] private Color _attackColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color _shootColor = new Color(1f, 0.6f, 0.1f);
        [SerializeField] private float _stepScale = 0.8f;

        public void Show(Vector3 position, PathMarkerKind kind, Color color, int sortingOrder)
        {
            transform.position = position;
            _renderer.sortingOrder = sortingOrder;
            switch (kind)
            {
                case PathMarkerKind.Step:
                    Apply(_dot, color, _stepScale);
                    break;
                case PathMarkerKind.Destination:
                    Apply(_cross, color, 1.3f);
                    break;
                case PathMarkerKind.Attack:
                    Apply(_cross, _attackColor, 1.5f);
                    break;
                default:
                    Apply(_ring, _shootColor, 1.6f);
                    break;
            }
        }

        private void Apply(Sprite sprite, Color color, float scale)
        {
            _renderer.sprite = sprite;
            _renderer.color = color;
            transform.localScale = Vector3.one * scale;
        }

        public sealed class Pool : MonoMemoryPool<PathMarkerView>
        {
        }
    }
}
