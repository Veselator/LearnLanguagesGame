using System.Collections.Generic;
using LearnLanguages.Core.Randomness;

namespace LearnLanguages.Gameplay.AI
{
    public readonly struct AIRankedAction
    {
        public AIRankedAction(AIAction action, float value, float probability)
        {
            Action = action;
            Value = value;
            Probability = probability;
        }

        public AIAction Action { get; }
        public float Value { get; }
        public float Probability { get; }
    }

    /// <summary>
    /// Chooses one action with minimax and alpha-beta pruning. The AI maximizes the score, its opponents are assumed
    /// to minimize it. Every option of the root is searched with a full window, so all of them get an exact value:
    /// that ranking is what the decision probabilities of the model are applied to.
    /// </summary>
    public sealed class AIMinimax
    {
        private readonly AIEvaluator _evaluator;
        private readonly AIActionGenerator _generator;
        private readonly IRandomService _random;
        private readonly IAIDecisionObserver _observer;
        private readonly IReadOnlyList<float> _probabilities;
        private readonly int _depth;
        private readonly int _maxBranching;
        private readonly float[] _criteria = new float[AICriteria.Count];

        public AIMinimax(AIProfile profile, AIEvaluator evaluator, AIActionGenerator generator, IRandomService random,
            IAIDecisionObserver observer)
        {
            _evaluator = evaluator;
            _generator = generator;
            _random = random;
            _observer = observer;
            _probabilities = profile.DecisionProbabilities;
            _depth = profile.SearchDepth;
            _maxBranching = profile.MaxBranching;
        }

        public AIAction Decide(AISimulation root)
        {
            _observer.DecisionStarted();
            var actions = GetOrderedActions(root, true);

            var values = new float[actions.Count];
            for (var i = 0; i < actions.Count; i++)
            {
                var next = root.Clone();
                actions[i].Apply(next);
                _observer.NodeEntered(actions[i]);
                values[i] = Search(next, _depth - 1, false, float.NegativeInfinity, float.PositiveInfinity);
                _observer.NodeExited(values[i], Best(values, i), float.NegativeInfinity, float.PositiveInfinity);
            }

            var ranked = Rank(actions, values);
            _observer.Ranked(ranked);

            var probabilities = new float[ranked.Count];
            for (var i = 0; i < probabilities.Length; i++) probabilities[i] = ranked[i].Probability;
            var roll = _random.Value;
            var rank = AIDecisionPicker.Pick(probabilities, roll);
            _observer.Picked(rank, roll);
            return ranked[rank].Action;
        }

        private float Search(AISimulation state, int depth, bool maximizing, float alpha, float beta)
        {
            if (state.IsTerminal)
            {
                var terminal = AIEvaluator.TerminalValue(state, depth);
                _observer.LeafEvaluated(null, terminal, true, alpha, beta);
                return terminal;
            }
            if (depth <= 0)
            {
                var score = _evaluator.Score(state, _criteria);
                _observer.LeafEvaluated(_criteria, score, false, alpha, beta);
                return score;
            }

            var actions = GetOrderedActions(state, maximizing);
            var best = maximizing ? float.NegativeInfinity : float.PositiveInfinity;
            for (var i = 0; i < actions.Count; i++)
            {
                var next = state.Clone();
                actions[i].Apply(next);
                _observer.NodeEntered(actions[i]);
                var value = Search(next, depth - 1, !maximizing, alpha, beta);

                if (maximizing)
                {
                    if (value > best) best = value;
                    if (best > alpha) alpha = best;
                }
                else
                {
                    if (value < best) best = value;
                    if (best < beta) beta = best;
                }
                _observer.NodeExited(value, best, alpha, beta);

                if (beta <= alpha)
                {
                    _observer.Pruned(actions.Count - i - 1, alpha, beta);
                    break;
                }
            }
            return best;
        }

        /// <summary>
        /// All legal options, most promising first by the score right after them; only the best ones stay in the tree.
        /// Good ordering is also what makes alpha-beta prune early.
        /// </summary>
        private List<AIAction> GetOrderedActions(AISimulation state, bool maximizing)
        {
            var all = maximizing ? _generator.GenerateOwn(state) : _generator.GenerateOpponents(state);
            var total = all.Count;
            var scored = new List<KeyValuePair<float, AIAction>>(total);
            foreach (var action in all)
            {
                var next = state.Clone();
                action.Apply(next);
                var score = next.IsTerminal ? AIEvaluator.TerminalValue(next, 0) : _evaluator.Score(next);
                scored.Add(new KeyValuePair<float, AIAction>(score, action));
            }
            scored.Sort((a, b) => maximizing ? b.Key.CompareTo(a.Key) : a.Key.CompareTo(b.Key));

            var kept = KeepPromising(scored, all[0]);
            _observer.ActionsGenerated(total, kept, maximizing);
            return kept;
        }

        /// <summary>
        /// The best options, with at most half of the places for economy so it cannot crowd out army moves.
        /// Doing nothing always stays an option, otherwise the search could force a harmful action.
        /// </summary>
        private List<AIAction> KeepPromising(List<KeyValuePair<float, AIAction>> scored, AIAction pass)
        {
            var kept = new List<AIAction>(_maxBranching) { pass };
            var economyPlaces = _maxBranching / 2;
            var skipped = new List<AIAction>();
            foreach (var pair in scored)
            {
                var action = pair.Value;
                if (action == pass) continue;
                if (kept.Count >= _maxBranching) break;
                if (!(action is AIMoveAction) && economyPlaces-- <= 0)
                {
                    skipped.Add(action);
                    continue;
                }
                kept.Add(action);
            }
            for (var i = 0; i < skipped.Count && kept.Count < _maxBranching; i++) kept.Add(skipped[i]);
            return kept;
        }

        private static float Best(float[] values, int last)
        {
            var best = values[0];
            for (var i = 1; i <= last; i++)
                if (values[i] > best) best = values[i];
            return best;
        }

        private List<AIRankedAction> Rank(List<AIAction> actions, float[] values)
        {
            var order = new List<int>(actions.Count);
            for (var i = 0; i < actions.Count; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                var compared = values[b].CompareTo(values[a]);
                if (compared != 0) return compared;
                // Waiting is never better than acting now when the search sees no difference.
                var aPasses = actions[a] is AIPassAction;
                var bPasses = actions[b] is AIPassAction;
                return aPasses != bPasses ? aPasses.CompareTo(bPasses) : a.CompareTo(b);
            });

            var probabilities = AIDecisionPicker.GetRankProbabilities(_probabilities, actions.Count);
            var ranked = new List<AIRankedAction>(actions.Count);
            for (var i = 0; i < order.Count; i++)
                ranked.Add(new AIRankedAction(actions[order[i]], values[order[i]], probabilities[i]));
            return ranked;
        }
    }
}
