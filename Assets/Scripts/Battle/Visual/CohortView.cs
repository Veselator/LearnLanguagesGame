using LearnLanguages.Battle.Logic;
using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Battle.Visual
{
    /// <summary>
    /// A cohort on the battle lines: a group of soldier figures whose number reflects the remaining strength.
    /// </summary>
    public sealed class CohortView : MonoBehaviour
    {
        [Tooltip("Figure sets per UnitClass: Swordsmen, Archers, Cavalry, Commander.")]
        [SerializeField] private CohortFigureSet[] _variants;
        [SerializeField] private Transform _flipRoot;
        [SerializeField] private TextMeshPro _countLabel;
        [SerializeField] private TextMeshPro _orderLabel;
        [SerializeField] private Transform _organizationBar;
        [SerializeField] private SpriteRenderer _organizationBarRenderer;
        [SerializeField] private GameObject _selection;
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _shakeDuration = 0.35f;

        private CohortFigureSet _figures;
        private Vector3 _target;
        private float _shake;

        public BattleCohort Cohort { get; private set; }

        public void Bind(BattleCohort cohort, Color ownerColor, int sortingOrder)
        {
            Cohort = cohort;
            for (var i = 0; i < _variants.Length; i++) _variants[i].gameObject.SetActive(i == (int)cohort.Type.Class);
            _figures = _variants[(int)cohort.Type.Class];
            _figures.Setup(ownerColor, sortingOrder);
            _organizationBarRenderer.sortingOrder = sortingOrder + 3;
            _countLabel.sortingOrder = sortingOrder + 3;
            _orderLabel.sortingOrder = sortingOrder + 3;

            var scale = _flipRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * (cohort.Side == BattleSideId.Attacker ? 1f : -1f);
            _flipRoot.localScale = scale;
            SetSelected(false);
        }

        public void SetSortingOrder(int sortingOrder)
        {
            _figures.SetSortingOrder(sortingOrder);
            _organizationBarRenderer.sortingOrder = sortingOrder + 3;
            _countLabel.sortingOrder = sortingOrder + 3;
            _orderLabel.sortingOrder = sortingOrder + 3;
        }

        public void PlaceImmediately(Vector3 position)
        {
            _target = position;
            transform.position = position;
        }

        public void MoveTo(Vector3 position) => _target = position;

        public void Refresh(bool showOrder)
        {
            _figures.ShowStrength(Cohort.StrengthRatio);
            _countLabel.text = Cohort.Healthy.ToString();
            var bar = _organizationBar.localScale;
            bar.x = Mathf.Clamp01(Cohort.Organization);
            _organizationBar.localScale = bar;
            _organizationBarRenderer.color = Color.Lerp(Color.red, Color.green, Cohort.Organization);
            _orderLabel.text = showOrder ? OrderSymbol(Cohort.Order.Type) : string.Empty;
        }

        public void SetSelected(bool selected) => _selection.SetActive(selected);

        public void PlayHit() => _shake = _shakeDuration;

        private void Update()
        {
            var position = Vector3.MoveTowards(transform.position, _target, _moveSpeed * Time.deltaTime);
            if (_shake > 0f)
            {
                _shake -= Time.deltaTime;
                _flipRoot.localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * 0.06f, 0f, 0f);
            }
            else _flipRoot.localPosition = Vector3.zero;
            transform.position = position;
        }

        private static string OrderSymbol(BattleOrderType order)
        {
            switch (order)
            {
                case BattleOrderType.Move: return ">";
                case BattleOrderType.Defend: return "#";
                case BattleOrderType.Shout: return "!";
                case BattleOrderType.Shoot: return "*";
                default: return string.Empty;
            }
        }

        public sealed class Pool : MonoMemoryPool<CohortView>
        {
            protected override void OnDespawned(CohortView item)
            {
                item.Cohort = null;
                base.OnDespawned(item);
            }
        }
    }
}
