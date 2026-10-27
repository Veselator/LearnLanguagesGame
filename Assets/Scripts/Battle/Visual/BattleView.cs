using System.Collections.Generic;
using LearnLanguages.Battle.Logic;
using LearnLanguages.Cameras;
using LearnLanguages.Core.Events;
using LearnLanguages.UI.Common;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Battle.Visual
{
    /// <summary>
    /// Flat 2D view of the battlefield: six horizontal lines stacked from the back (top) to the front (bottom),
    /// cohorts standing on them. Mirrors the battle model on events.
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        [SerializeField] private Transform _tilesRoot;
        [SerializeField] private Transform _cohortsRoot;
        [SerializeField] private float _cellWidth = 1.6f;
        [Tooltip("Vertical distance between lines: the full tile height, so lines never overlap.")]
        [SerializeField] private float _rowStep = 0.8f;
        [SerializeField] private Vector3 _unitOffset = new Vector3(0f, 0.15f, -0.005f);
        [SerializeField] private Vector3 _textOffset = new Vector3(0f, 1f, -0.01f);
        [SerializeField] private Color _lossColor = new Color(1f, 0.3f, 0.25f);
        [SerializeField] private Color _shoutColor = new Color(1f, 0.9f, 0.3f);
        [Tooltip("Where path markers lie relative to the cell centre (on the ground in front of the units).")]
        [SerializeField] private Vector3 _markerOffset = new Vector3(0f, -0.08f, -0.003f);
        [Tooltip("Darkening of every second line so the rows stay readable.")]
        [Range(0f, 0.3f)] [SerializeField] private float _rowShade = 0.1f;
        [Tooltip("Decorative cells beyond both edges so the field does not end inside the view.")]
        [SerializeField] private int _edgePadding = 6;

        private readonly List<BattleTileView> _tiles = new List<BattleTileView>();
        private readonly Dictionary<BattleCohort, CohortView> _cohorts = new Dictionary<BattleCohort, CohortView>();
        private readonly List<PathMarkerView> _markers = new List<PathMarkerView>();

        private IEventBus _eventBus;
        private BattleTileView.Pool _tilePool;
        private CohortView.Pool _cohortPool;
        private FloatingTextView.Pool _textPool;
        private BattleTileVisualConfig _tileVisuals;
        private PathMarkerView.Pool _markerPool;
        private CameraController _cameraController;
        private RtsCameraMethod _mapCamera;
        private IScreenFader _fader;
        private IBattleSelection _selection;
        private BattleModel _model;

        [Inject]
        public void Construct(IEventBus eventBus, BattleTileView.Pool tilePool, CohortView.Pool cohortPool,
            FloatingTextView.Pool textPool, PathMarkerView.Pool markerPool, BattleTileVisualConfig tileVisuals,
            CameraController cameraController, RtsCameraMethod mapCamera, IScreenFader fader, IBattleSelection selection)
        {
            _eventBus = eventBus;
            _tilePool = tilePool;
            _cohortPool = cohortPool;
            _textPool = textPool;
            _markerPool = markerPool;
            _tileVisuals = tileVisuals;
            _cameraController = cameraController;
            _mapCamera = mapCamera;
            _fader = fader;
            _selection = selection;

            _eventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            _eventBus.Subscribe<BattleStateChangedEvent>(OnStateChanged);
            _eventBus.Subscribe<BattleTickResolvedEvent>(OnTickResolved);
            _eventBus.Subscribe<BattleClosedEvent>(OnBattleClosed);
            _selection.Changed += RefreshHighlights;
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            _eventBus.Unsubscribe<BattleStateChangedEvent>(OnStateChanged);
            _eventBus.Unsubscribe<BattleTickResolvedEvent>(OnTickResolved);
            _eventBus.Unsubscribe<BattleClosedEvent>(OnBattleClosed);
            _selection.Changed -= RefreshHighlights;
        }

        public bool TryGetCell(Collider hitCollider, out Vector2Int cell)
        {
            var tile = hitCollider.GetComponentInParent<BattleTileView>();
            cell = tile != null ? tile.Cell : BattleCohort.NoCell;
            return tile != null;
        }

        public float CellWidth => _cellWidth;
        public float FieldWidth => BattleGrid.Columns * _cellWidth;
        public float RowsCenterY => (BattleGrid.Rows - 1) * _rowStep * 0.5f;

        public Vector3 CellToWorld(Vector2Int cell) =>
            transform.position + new Vector3((cell.x + 0.5f) * _cellWidth, (BattleGrid.Rows - 1 - cell.y) * _rowStep, 0f);

        private static int SortingOrderOf(int row) => row * 20;

        // The camera flight into the battle is handled by BattleTransitionView; the field must exist right away.
        private void OnBattleStarted(BattleStartedEvent e) => Build(e.Model);

        private void OnBattleClosed(BattleClosedEvent e)
        {
            _fader.Transition(() =>
            {
                Clear();
                _selection.Clear();
                _cameraController.SetMethod(_mapCamera);
            });
        }

        private void Build(BattleModel model)
        {
            Clear();
            _model = model;

            for (var row = 0; row < BattleGrid.Rows; row++)
            {
                var tint = Color.Lerp(Color.white, Color.black, row % 2 == 1 ? _rowShade : 0f);
                tint.a = 1f;
                for (var column = -_edgePadding; column < BattleGrid.Columns + _edgePadding; column++)
                {
                    var cell = new Vector2Int(column, row);
                    // Padding cells repeat the ground of the nearest real cell and cannot be selected.
                    var source = new Vector2Int(Mathf.Clamp(column, 0, BattleGrid.Columns - 1), row);
                    var visual = _tileVisuals.Get(model.Grid.IsPassable(source) ? model.Grid.GetTile(source) : Data.BattleTileType.Grass);
                    var tile = _tilePool.Spawn();
                    tile.transform.SetParent(_tilesRoot, false);
                    tile.transform.position = CellToWorld(cell);
                    tile.Bind(model.Grid.IsInside(cell) ? cell : BattleCohort.NoCell,
                        model.Grid.IsInside(cell) ? _tileVisuals.Get(model.Grid.GetTile(cell)) : visual, SortingOrderOf(row), tint);
                    _tiles.Add(tile);
                }
            }

            foreach (var cohort in model.Cohorts)
            {
                var view = _cohortPool.Spawn();
                view.transform.SetParent(_cohortsRoot, false);
                view.Bind(cohort, model.Setup.Get(cohort.Side).Player.Color, SortingOrderOf(Mathf.Max(0, cohort.Cell.y)) + 5);
                _cohorts[cohort] = view;
            }
            Sync(true);
            RefreshHighlights();
        }

        private void Clear()
        {
            foreach (var tile in _tiles) _tilePool.Despawn(tile);
            _tiles.Clear();
            foreach (var view in _cohorts.Values) _cohortPool.Despawn(view);
            _cohorts.Clear();
            ClearMarkers();
            _model = null;
        }

        private void OnStateChanged(BattleStateChangedEvent e)
        {
            if (e.Model != _model) return;
            Sync(false);
            RefreshHighlights();
        }

        private void OnTickResolved(BattleTickResolvedEvent e)
        {
            if (e.Model != _model) return;
            foreach (var report in e.Reports)
            {
                switch (report.Kind)
                {
                    case CombatReportKind.Melee:
                    case CombatReportKind.Ranged:
                        if (report.Losses <= 0 || !_cohorts.TryGetValue(report.Target, out var target)) break;
                        target.PlayHit();
                        ShowText(target.transform.position, "-" + report.Losses, _lossColor);
                        break;
                    case CombatReportKind.Shout:
                        if (_cohorts.TryGetValue(report.Source, out var shouter))
                            ShowText(shouter.transform.position, report.Losses > 0 ? "!!!" : "!", _shoutColor);
                        break;
                }
            }
            Sync(false);
        }

        private void ShowText(Vector3 position, string text, Color color)
        {
            var floating = _textPool.Spawn();
            floating.Show(_textPool, position + _textOffset, text, color);
        }

        private void Sync(bool immediate)
        {
            if (_model == null) return;
            var showOrders = _model.Phase == BattlePhase.Command;
            foreach (var pair in _cohorts)
            {
                var cohort = pair.Key;
                var view = pair.Value;
                var visible = cohort.IsOnField;
                var wasVisible = view.gameObject.activeSelf;
                view.gameObject.SetActive(visible);
                if (!visible) continue;

                var position = CellToWorld(cohort.Cell) + _unitOffset;
                view.SetSortingOrder(SortingOrderOf(cohort.Cell.y) + 5);
                if (immediate || !wasVisible) view.PlaceImmediately(position);
                else view.MoveTo(position);
                view.Refresh(showOrders && IsHumanSide(cohort));
            }
            RefreshMarkers();
        }

        private void ClearMarkers()
        {
            foreach (var marker in _markers) _markerPool.Despawn(marker);
            _markers.Clear();
        }

        /// <summary>Dots along the planned paths of the player's cohorts, a cross at the goal, a ring on a shooting target.</summary>
        private void RefreshMarkers()
        {
            ClearMarkers();
            if (_model == null || (_model.Phase != BattlePhase.Command && _model.Phase != BattlePhase.Action)) return;

            foreach (var cohort in _model.Cohorts)
            {
                if (!cohort.IsOnField || !IsHumanSide(cohort)) continue;
                var color = _model.Setup.Get(cohort.Side).Player.Color;
                switch (cohort.Order)
                {
                    case MoveOrder move:
                        for (var i = move.NextIndex; i < move.Path.Count; i++)
                        {
                            var cell = move.Path[i];
                            var isLast = i == move.Path.Count - 1;
                            var occupant = _model.GetCohortAt(cell);
                            var kind = !isLast ? PathMarkerKind.Step
                                : occupant != null && occupant.Side != cohort.Side ? PathMarkerKind.Attack
                                : PathMarkerKind.Destination;
                            AddMarker(cell, kind, color);
                        }
                        break;
                    case ShootOrder shoot when shoot.Target != null && shoot.Target.IsOnField:
                        AddMarker(shoot.Target.Cell, PathMarkerKind.ShootTarget, color);
                        break;
                }
            }
        }

        private void AddMarker(Vector2Int cell, PathMarkerKind kind, Color color)
        {
            var marker = _markerPool.Spawn();
            marker.Show(CellToWorld(cell) + _markerOffset, kind, color, SortingOrderOf(cell.y) + 4);
            _markers.Add(marker);
        }

        private bool IsHumanSide(BattleCohort cohort) => _model.Setup.Get(cohort.Side).IsHuman;

        private void RefreshHighlights()
        {
            foreach (var tile in _tiles) tile.SetHighlight(_selection.GetHighlight(tile.Cell));
            foreach (var pair in _cohorts) pair.Value.SetSelected(pair.Key == _selection.Selected);
            RefreshMarkers();
        }
    }
}
