using System.Globalization;
using System.Text;
using LearnLanguages.Gameplay.AI;
using LearnLanguages.Localization;
using LearnLanguages.Map.Logic;

namespace LearnLanguages.UI.AI
{
    /// <summary>
    /// Words for the decision tree: what an option means and what the algorithm is doing at a step.
    /// </summary>
    public sealed class AIDecisionTextFormatter
    {
        private readonly ILocalizationService _localization;
        private readonly IGameMap _map;

        public AIDecisionTextFormatter(ILocalizationService localization, IGameMap map)
        {
            _localization = localization;
            _map = map;
        }

        public string Title(AIDecisionTrace trace) => _localization.Format("ai.title", trace.PlayerName, trace.ProfileName);

        public string Progress(AIDecisionPlayback playback)
        {
            var step = playback.Current;
            return _localization.Format("ai.progress", step.Decision + 1, playback.Trace.DecisionCount,
                playback.Cursor + 1, playback.Trace.Steps.Count);
        }

        public string Action(AIAction action)
        {
            switch (action)
            {
                case AIMoveAction move:
                    return _localization.Format("ai.action.move", move.ArmyId, Tile(move.FromTile), Tile(move.ToTile));
                case AIBuildAction build:
                    return _localization.Format("ai.action.build", _localization.Get(build.Option.Upgrade.NameKey), Tile(build.Option.TileId));
                case AIRecruitAction recruit:
                    return _localization.Format("ai.action.recruit", _localization.Get(recruit.Option.Unit.NameKey), Tile(recruit.Option.TileId));
                case AIReinforceAction reinforce:
                    return _localization.Format("ai.action.reinforce", reinforce.ArmyId, Tile(reinforce.Tile));
                case AIMergeAction merge:
                    return _localization.Format("ai.action.merge", Tile(merge.Tile));
                default:
                    return _localization.Get("ai.action.pass");
            }
        }

        public string RowLabel(bool isMaximizing) => _localization.Get(isMaximizing ? "ai.row.max" : "ai.row.min");

        public string RootLabel => _localization.Get("ai.row.root");

        public string Value(float value)
        {
            if (float.IsPositiveInfinity(value)) return "+∞";
            if (float.IsNegativeInfinity(value)) return "−∞";
            return value.ToString("+0.000;−0.000;0.000", CultureInfo.InvariantCulture);
        }

        /// <summary>The step in words, with everything the algorithm knows at that moment.</summary>
        public string Describe(AIDecisionPlayback playback)
        {
            var step = playback.Current;
            var trace = playback.Trace;
            var text = new StringBuilder();
            text.Append("<b>").Append(_localization.Get("ai.step." + KeyOf(step))).Append("</b>\n");
            text.Append(Details(step, trace)).Append('\n');

            if (step.Kind == AITraceStepKind.LeafEvaluated || step.Kind == AITraceStepKind.NodeExited || step.Kind == AITraceStepKind.Pruned)
                text.Append(_localization.Format("ai.bounds", Value(step.Alpha), Value(step.Beta))).Append('\n');

            if (step.Criteria != null) AppendCriteria(text, step, trace);
            if (step.Ranking != null) AppendRanking(text, step);
            return text.ToString();
        }

        private string Details(AITraceStep step, AIDecisionTrace trace)
        {
            switch (step.Kind)
            {
                case AITraceStepKind.ActionsGenerated:
                    return _localization.Format("ai.detail.generated", step.Total, step.Count);
                case AITraceStepKind.NodeEntered:
                    return Action(trace.Nodes[step.NodeId].Action);
                case AITraceStepKind.LeafEvaluated:
                    return _localization.Format("ai.detail.score", Value(step.Value));
                case AITraceStepKind.NodeExited:
                    return _localization.Format("ai.detail.returned", Action(trace.Nodes[step.ChildId].Action), Value(step.Value), Value(step.Best));
                case AITraceStepKind.Pruned:
                    return _localization.Format("ai.detail.pruned", step.Count);
                case AITraceStepKind.Picked:
                    return _localization.Format("ai.detail.picked", step.Roll.ToString("0.00", CultureInfo.InvariantCulture),
                        step.PickedRank + 1, Action(step.Ranking[step.PickedRank].Action));
                default:
                    return string.Empty;
            }
        }

        private void AppendCriteria(StringBuilder text, AITraceStep step, AIDecisionTrace trace)
        {
            for (var i = 0; i < AICriteria.Count; i++)
            {
                var name = _localization.Get("ai.criterion." + ((AICriterion)i).ToString().ToLowerInvariant());
                text.Append(name).Append("<pos=56%>").Append(Value(step.Criteria[i]))
                    .Append(" × ").Append(trace.Weights[i].ToString("0.00", CultureInfo.InvariantCulture))
                    .Append("\n");
            }
        }

        private void AppendRanking(StringBuilder text, AITraceStep step)
        {
            for (var i = 0; i < step.Ranking.Count; i++)
            {
                var option = step.Ranking[i];
                var chosen = i == step.PickedRank;
                if (chosen) text.Append("<b>");
                text.Append(i + 1).Append(". ").Append(Action(option.Action)).Append("  ").Append(Value(option.Value))
                    .Append("  ").Append(option.Probability.ToString("P0", CultureInfo.InvariantCulture));
                if (chosen) text.Append("</b>");
                text.Append('\n');
            }
        }

        private static string KeyOf(AITraceStep step)
        {
            switch (step.Kind)
            {
                case AITraceStepKind.DecisionStarted: return "started";
                case AITraceStepKind.ActionsGenerated: return "generated";
                case AITraceStepKind.NodeEntered: return "entered";
                case AITraceStepKind.LeafEvaluated: return step.IsTerminal ? "terminal" : "leaf";
                case AITraceStepKind.NodeExited: return "returned";
                case AITraceStepKind.Pruned: return "pruned";
                case AITraceStepKind.Ranked: return "ranked";
                default: return "picked";
            }
        }

        private string Tile(int id) => _map.Get(id).Data.Name;
    }
}
