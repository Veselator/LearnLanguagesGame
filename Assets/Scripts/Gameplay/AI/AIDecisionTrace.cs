using System.Collections.Generic;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// Receives every step of <see cref="AIMinimax"/> at the moment the algorithm makes it.
    /// </summary>
    public interface IAIDecisionObserver
    {
        void DecisionStarted();
        void ActionsGenerated(int total, IReadOnlyList<AIAction> kept, bool maximizing);
        void NodeEntered(AIAction action);
        /// <param name="criteria">Values of the criteria, valid during the call only; null for a finished game.</param>
        void LeafEvaluated(float[] criteria, float score, bool terminal, float alpha, float beta);
        void NodeExited(float value, float best, float alpha, float beta);
        void Pruned(int skipped, float alpha, float beta);
        void Ranked(IReadOnlyList<AIRankedAction> ranked);
        void Picked(int rank, float roll);
    }

    public sealed class AINullDecisionObserver : IAIDecisionObserver
    {
        public static readonly AINullDecisionObserver Instance = new AINullDecisionObserver();

        public void DecisionStarted() { }
        public void ActionsGenerated(int total, IReadOnlyList<AIAction> kept, bool maximizing) { }
        public void NodeEntered(AIAction action) { }
        public void LeafEvaluated(float[] criteria, float score, bool terminal, float alpha, float beta) { }
        public void NodeExited(float value, float best, float alpha, float beta) { }
        public void Pruned(int skipped, float alpha, float beta) { }
        public void Ranked(IReadOnlyList<AIRankedAction> ranked) { }
        public void Picked(int rank, float roll) { }
    }

    public enum AITraceStepKind
    {
        DecisionStarted,
        ActionsGenerated,
        NodeEntered,
        LeafEvaluated,
        NodeExited,
        Pruned,
        Ranked,
        Picked
    }

    public sealed class AITraceStep
    {
        public AITraceStepKind Kind;
        public int Decision;
        /// <summary>The node the algorithm is in after this step.</summary>
        public int NodeId;
        /// <summary>The node just left (<see cref="AITraceStepKind.NodeExited"/>) .</summary>
        public int ChildId = -1;
        public float Value;
        public float Best;
        public float Alpha = float.NegativeInfinity;
        public float Beta = float.PositiveInfinity;
        public int Total;
        public int Count;
        public float[] Criteria;
        public bool IsTerminal;
        public IReadOnlyList<AIRankedAction> Ranking;
        public int PickedRank = -1;
        public float Roll;
    }

    public sealed class AITraceNode
    {
        public int Id;
        public int ParentId = -1;
        public int Depth;
        public bool IsMaximizing;
        public AIAction Action;
        public int GeneratedStep;
        public int EnteredStep = -1;
        public int ExitedStep = -1;
        public int PrunedStep = -1;
        public float Value;
        public readonly List<int> Children = new List<int>();
    }

    /// <summary>
    /// The recorded course of one AI turn: a tree per decision and the ordered list of steps.
    /// Since steps carry their own indices, any moment can be displayed without replaying the others.
    /// </summary>
    public sealed class AIDecisionTrace : IAIDecisionObserver
    {
        private readonly List<AITraceStep> _steps = new List<AITraceStep>();
        private readonly List<AITraceNode> _nodes = new List<AITraceNode>();
        private readonly List<int> _decisionEnds = new List<int>();
        private readonly List<int> _decisionRoots = new List<int>();
        private int _current = -1;
        private int _decision = -1;

        public AIDecisionTrace(string playerName, AIProfile profile)
        {
            PlayerName = playerName;
            ProfileName = profile.DisplayName;
            Weights = profile.NormalizedPriorities;
        }

        public string PlayerName { get; }
        public string ProfileName { get; }
        public float[] Weights { get; }
        public IReadOnlyList<AITraceStep> Steps => _steps;
        public IReadOnlyList<AITraceNode> Nodes => _nodes;
        public int DecisionCount => _decisionRoots.Count;

        public int GetRoot(int decision) => _decisionRoots[decision];
        public int GetLastStep(int decision) => _decisionEnds[decision];

        public void DecisionStarted()
        {
            _decision++;
            var root = new AITraceNode { Id = _nodes.Count, IsMaximizing = true, GeneratedStep = _steps.Count, EnteredStep = _steps.Count };
            _nodes.Add(root);
            _decisionRoots.Add(root.Id);
            _decisionEnds.Add(_steps.Count);
            _current = root.Id;
            AddStep(new AITraceStep { Kind = AITraceStepKind.DecisionStarted });
        }

        public void ActionsGenerated(int total, IReadOnlyList<AIAction> kept, bool maximizing)
        {
            var node = _nodes[_current];
            node.IsMaximizing = maximizing;
            foreach (var action in kept)
            {
                var child = new AITraceNode
                {
                    Id = _nodes.Count, ParentId = node.Id, Depth = node.Depth + 1, IsMaximizing = !maximizing,
                    Action = action, GeneratedStep = _steps.Count
                };
                _nodes.Add(child);
                node.Children.Add(child.Id);
            }
            AddStep(new AITraceStep { Kind = AITraceStepKind.ActionsGenerated, Total = total, Count = kept.Count });
        }

        public void NodeEntered(AIAction action)
        {
            foreach (var id in _nodes[_current].Children)
            {
                if (_nodes[id].Action != action) continue;
                _current = id;
                _nodes[id].EnteredStep = _steps.Count;
                break;
            }
            AddStep(new AITraceStep { Kind = AITraceStepKind.NodeEntered });
        }

        public void LeafEvaluated(float[] criteria, float score, bool terminal, float alpha, float beta)
        {
            AddStep(new AITraceStep
            {
                Kind = AITraceStepKind.LeafEvaluated, Value = score, IsTerminal = terminal, Alpha = alpha, Beta = beta,
                Criteria = criteria != null ? (float[])criteria.Clone() : null
            });
        }

        public void NodeExited(float value, float best, float alpha, float beta)
        {
            var child = _nodes[_current];
            child.Value = value;
            child.ExitedStep = _steps.Count;
            _current = child.ParentId;
            AddStep(new AITraceStep
            {
                Kind = AITraceStepKind.NodeExited, ChildId = child.Id, Value = value, Best = best, Alpha = alpha, Beta = beta
            });
        }

        public void Pruned(int skipped, float alpha, float beta)
        {
            _nodes[_current].PrunedStep = _steps.Count;
            AddStep(new AITraceStep { Kind = AITraceStepKind.Pruned, Count = skipped, Alpha = alpha, Beta = beta });
        }

        public void Ranked(IReadOnlyList<AIRankedAction> ranked) =>
            AddStep(new AITraceStep { Kind = AITraceStepKind.Ranked, Ranking = ranked });

        public void Picked(int rank, float roll)
        {
            var ranking = _steps[_steps.Count - 1].Ranking;
            AddStep(new AITraceStep { Kind = AITraceStepKind.Picked, Ranking = ranking, PickedRank = rank, Roll = roll });
        }

        private void AddStep(AITraceStep step)
        {
            step.Decision = _decision;
            step.NodeId = _current;
            _steps.Add(step);
            _decisionEnds[_decision] = _steps.Count - 1;
        }
    }

    /// <summary>
    /// Cursor over a trace: step back, step forward, jump to the end of the current tree.
    /// </summary>
    public sealed class AIDecisionPlayback
    {
        public AIDecisionPlayback(AIDecisionTrace trace)
        {
            Trace = trace;
        }

        public AIDecisionTrace Trace { get; }
        public int Cursor { get; private set; }
        public AITraceStep Current => Trace.Steps.Count > 0 ? Trace.Steps[Cursor] : null;
        public bool CanStepBack => Cursor > 0;
        public bool CanStepForward => Cursor < Trace.Steps.Count - 1;

        public void StepBack()
        {
            if (CanStepBack) Cursor--;
        }

        public void StepForward()
        {
            if (CanStepForward) Cursor++;
        }

        /// <summary>To the last step of the current tree; from there, to the last step of the next one.</summary>
        public void ToEndOfTree()
        {
            if (!CanStepForward) return;
            var decision = Current.Decision;
            var end = Trace.GetLastStep(decision);
            Cursor = Cursor < end ? end : Trace.GetLastStep(decision + 1);
        }

        public bool IsGenerated(AITraceNode node) => node.GeneratedStep <= Cursor;
        public bool IsEntered(AITraceNode node) => node.EnteredStep >= 0 && node.EnteredStep <= Cursor;
        public bool IsFinished(AITraceNode node) => node.ExitedStep >= 0 && node.ExitedStep <= Cursor;

        /// <summary>Generated in a node that was cut off before the search got to it.</summary>
        public bool IsPruned(AITraceNode node)
        {
            if (IsEntered(node) || node.ParentId < 0) return false;
            var parent = Trace.Nodes[node.ParentId];
            return parent.PrunedStep >= 0 && parent.PrunedStep <= Cursor;
        }
    }
}
