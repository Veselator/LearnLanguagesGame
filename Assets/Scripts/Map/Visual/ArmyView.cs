using LearnLanguages.Gameplay.Units;
using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Map.Visual
{
    /// <summary>
    /// Army marker on the global map: a soldier tinted with the owner color, or a ship at sea.
    /// </summary>
    public sealed class ArmyView : MonoBehaviour
    {
        [SerializeField] private GameObject _landVisual;
        [SerializeField] private GameObject _seaVisual;
        [SerializeField] private SpriteRenderer[] _tintedRenderers;
        [SerializeField] private GameObject _selection;
        [SerializeField] private GameObject _movedMarker;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _moveSpeed = 12f;

        private Vector3 _targetPosition;

        public Army Army { get; private set; }

        public void Bind(Army army, Color color, Quaternion facing)
        {
            Army = army;
            transform.rotation = facing;
            foreach (var spriteRenderer in _tintedRenderers) spriteRenderer.color = color;
            SetSelected(false);
        }

        public void PlaceImmediately(Vector3 position)
        {
            _targetPosition = position;
            transform.position = position;
        }

        public void MoveTo(Vector3 position) => _targetPosition = position;

        public void Refresh(bool atSea, bool showMoved)
        {
            _landVisual.SetActive(!atSea);
            _seaVisual.SetActive(atSea);
            _movedMarker.SetActive(showMoved);

            var soldiers = 0;
            foreach (var cohort in Army.Cohorts) soldiers += cohort.Healthy;
            _label.text = $"{Army.Cohorts.Count} | {soldiers}";
        }

        public void SetSelected(bool selected) => _selection.SetActive(selected);

        private void Update()
        {
            if (transform.position != _targetPosition)
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);
        }

        public sealed class Pool : MonoMemoryPool<ArmyView>
        {
            protected override void OnDespawned(ArmyView item)
            {
                item.Army = null;
                base.OnDespawned(item);
            }
        }
    }
}
