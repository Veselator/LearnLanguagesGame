using UnityEngine;

namespace LearnLanguages.Battle.Visual
{
    /// <summary>
    /// Figures of one unit class. Body parts listed in <see cref="_tinted"/> take the owner color.
    /// </summary>
    public sealed class CohortFigureSet : MonoBehaviour
    {
        [SerializeField] private GameObject[] _figures;
        [SerializeField] private SpriteRenderer[] _tinted;
        [SerializeField] private SpriteRenderer[] _allRenderers;
        [Tooltip("Sorting offset of each renderer in All Renderers (draw order inside a figure).")]
        [SerializeField] private int[] _sortingOffsets;

        public void Setup(Color ownerColor, int sortingOrder)
        {
            foreach (var spriteRenderer in _tinted) spriteRenderer.color = ownerColor;
            SetSortingOrder(sortingOrder);
        }

        public void SetSortingOrder(int sortingOrder)
        {
            for (var i = 0; i < _allRenderers.Length; i++)
                _allRenderers[i].sortingOrder = sortingOrder + (i < _sortingOffsets.Length ? _sortingOffsets[i] : 1);
        }

        public void ShowStrength(float ratio)
        {
            var visible = ratio <= 0f ? 0 : Mathf.Clamp(Mathf.CeilToInt(ratio * _figures.Length), 1, _figures.Length);
            for (var i = 0; i < _figures.Length; i++) _figures[i].SetActive(i < visible);
        }
    }
}
