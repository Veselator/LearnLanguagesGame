using System;
using System.Collections.Generic;
using LearnLanguages.Battle.Logic;
using UnityEngine;

namespace LearnLanguages.Battle.Visual
{
    public enum BattleClickMode
    {
        Default = 0,
        Shoot = 1
    }

    public interface IBattleSelection
    {
        BattleCohort Selected { get; }
        BattleClickMode Mode { get; }
        event Action Changed;
        void Click(Vector2Int cell);
        void Select(BattleCohort cohort);
        void SetMode(BattleClickMode mode);
        void Clear();
        CellHighlight GetHighlight(Vector2Int cell);
    }

    /// <summary>
    /// Shared selection logic of the battlefield used both by clicks in the scene and by the grid panel.
    /// </summary>
    public sealed class BattleSelection : IBattleSelection
    {
        private readonly IBattleController _controller;
        private readonly Dictionary<Vector2Int, CellHighlight> _highlights = new Dictionary<Vector2Int, CellHighlight>();

        public BattleSelection(IBattleController controller)
        {
            _controller = controller;
        }

        public BattleCohort Selected { get; private set; }
        public BattleClickMode Mode { get; private set; }
        public event Action Changed;

        private BattleModel Model => _controller.Model;

        public void Click(Vector2Int cell)
        {
            if (Model == null || !Model.Grid.IsInside(cell)) return;
            var occupant = Model.GetCohortAt(cell);
            var human = _controller.HumanSide;

            if (Model.Phase == BattlePhase.Deployment)
            {
                if (Selected != null && Model.CanDeploy(Selected, cell) && (occupant == null || occupant.Side == human))
                {
                    Model.Deploy(Selected, cell);
                    _controller.NotifyOrdersChanged();
                    Select(null);
                }
                else Select(occupant != null && occupant.Side == human ? occupant : null);
                return;
            }

            if (!_controller.IsWaitingForHumanOrders)
            {
                Select(occupant);
                return;
            }

            if (occupant != null && occupant.Side == human)
            {
                Select(occupant == Selected ? null : occupant);
                return;
            }

            if (Selected == null || Selected.Side != human)
            {
                Select(occupant);
                return;
            }

            var ordered = occupant != null && (Mode == BattleClickMode.Shoot || Selected.Type.IsRanged) &&
                          Model.TryOrderShoot(Selected, occupant);
            if (!ordered) ordered = Model.TryOrderMove(Selected, cell);
            if (!ordered) return;

            _controller.NotifyOrdersChanged();
            Mode = BattleClickMode.Default;
            Select(null);
        }

        public void Select(BattleCohort cohort)
        {
            Selected = cohort;
            if (cohort == null) Mode = BattleClickMode.Default;
            RebuildHighlights();
            Changed?.Invoke();
        }

        public void SetMode(BattleClickMode mode)
        {
            Mode = mode;
            RebuildHighlights();
            Changed?.Invoke();
        }

        public void Clear()
        {
            Selected = null;
            Mode = BattleClickMode.Default;
            _highlights.Clear();
            Changed?.Invoke();
        }

        public CellHighlight GetHighlight(Vector2Int cell) =>
            _highlights.TryGetValue(cell, out var highlight) ? highlight : CellHighlight.None;

        private void RebuildHighlights()
        {
            _highlights.Clear();
            if (Model == null) return;
            var human = _controller.HumanSide;

            if (Model.Phase == BattlePhase.Deployment)
            {
                for (var row = 0; row < BattleGrid.Rows; row++)
                for (var column = 0; column < BattleGrid.Columns; column++)
                {
                    var cell = new Vector2Int(column, row);
                    if (Model.IsDeploymentCell(human, cell) && Model.Grid.IsPassable(cell)) _highlights[cell] = CellHighlight.Deploy;
                }
                if (Selected != null) _highlights[Selected.Cell] = CellHighlight.Selected;
                return;
            }

            if (Selected == null || !Selected.IsOnField) return;
            if (Selected.Side == human && _controller.IsWaitingForHumanOrders)
            {
                if (Mode == BattleClickMode.Default)
                {
                    foreach (var pair in Model.GetReachableCells(Selected)) _highlights[pair.Key] = CellHighlight.Reachable;
                    foreach (var target in Model.GetAttackTargets(Selected)) _highlights[target.Cell] = CellHighlight.Attack;
                }
                if (Selected.Type.IsRanged)
                    foreach (var target in Model.GetShootTargets(Selected)) _highlights[target.Cell] = CellHighlight.Shoot;
            }

            if (Selected.Order is MoveOrder move)
                foreach (var cell in move.Path) _highlights[cell] = CellHighlight.Path;
            _highlights[Selected.Cell] = CellHighlight.Selected;
        }
    }
}
