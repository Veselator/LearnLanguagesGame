using System.Collections.Generic;
using LearnLanguages.Battle.Logic;
using LearnLanguages.Battle.Visual;
using LearnLanguages.Gameplay.Units;
using UnityEngine;

namespace LearnLanguages.UI.Battle
{
    /// <summary>
    /// The command grid at the bottom of the battle screen. Mirrors the battlefield and forwards clicks to the selection.
    /// </summary>
    public sealed class BattleGridPanelView : MonoBehaviour
    {
        [SerializeField] private Transform _container;
        [SerializeField] private BattleTileVisualConfig _tileVisuals;

        private static readonly string[] ClassLetters = { "S", "A", "C", "K" };
        private readonly List<BattleGridCellView> _cells = new List<BattleGridCellView>();

        public void Build(BattleModel model, BattleGridCellView.Pool pool, IBattleSelection selection)
        {
            Clear(pool);
            for (var row = 0; row < BattleGrid.Rows; row++)
            for (var column = 0; column < BattleGrid.Columns; column++)
            {
                var cell = new Vector2Int(column, row);
                var view = pool.Spawn();
                view.transform.SetParent(_container, false);
                view.transform.SetAsLastSibling();
                view.Bind(cell, _tileVisuals.Get(model.Grid.GetTile(cell)).GridColor, selection.Click);
                _cells.Add(view);
            }
        }

        public void Clear(BattleGridCellView.Pool pool)
        {
            foreach (var cell in _cells) pool.Despawn(cell);
            _cells.Clear();
        }

        public void Refresh(BattleModel model, IBattleSelection selection)
        {
            foreach (var view in _cells)
            {
                var cohort = model.GetCohortAt(view.Cell);
                if (cohort != null)
                {
                    var color = model.Setup.Get(cohort.Side).Player.Color;
                    view.SetCohort(true, color, ClassLetters[(int)cohort.Type.Class]);
                }
                else view.SetCohort(false, Color.clear, string.Empty);
                view.SetHighlight(selection.GetHighlight(view.Cell));
            }
        }

        public static string ClassLetter(UnitClass unitClass) => ClassLetters[(int)unitClass];
    }
}
