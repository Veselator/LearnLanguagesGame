using System;
using System.Collections.Generic;
using LearnLanguages.Settings;
using UnityEngine;

namespace LearnLanguages.Gameplay.AI
{
    /// <summary>
    /// A model of the AI: what it values (priorities of the criteria), how far it looks ahead and how often it
    /// deliberately plays a weaker option. Edited in the "AI models" window of the main menu.
    /// </summary>
    [Serializable]
    public sealed class AIProfile
    {
        public const int MaxSearchDepth = 5;
        public const int MaxOptionsPerNode = 20;
        public const int MaxActions = 60;

        [SerializeField] private string _id = "new_ai";
        [SerializeField] private string _displayName = "New AI";
        [Tooltip("Plies of minimax: own action, opponent's reply, own action...")]
        [SerializeField] private int _searchDepth = 2;
        [Tooltip("Options examined in each node of the tree; the most promising ones are kept.")]
        [SerializeField] private int _maxBranching = 8;
        [SerializeField] private int _maxActionsPerTurn = 30;
        [Tooltip("Priority of each criterion. Normalized at runtime: number / sum.")]
        [SerializeField] private float[] _priorities = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        [Tooltip("Index 0: chance of taking the best option, 1: the second best, and so on.")]
        [SerializeField] private float[] _decisionProbabilities = { 1f };

        public string Id => _id;
        public string DisplayName => _displayName;
        public float DifficultyRating => AIDifficultyRating.Calculate(SearchDepth, _decisionProbabilities);
        /// <summary>Not chosen by hand: follows from the search depth and the decision probabilities.</summary>
        public GameDifficulty Difficulty => AIDifficultyRating.ToDifficulty(DifficultyRating);
        public int SearchDepth => Mathf.Clamp(_searchDepth, 1, MaxSearchDepth);
        public int MaxBranching => Mathf.Clamp(_maxBranching, 2, MaxOptionsPerNode);
        public int MaxActionsPerTurn => Mathf.Clamp(_maxActionsPerTurn, 1, MaxActions);
        public IReadOnlyList<float> Priorities => _priorities;
        public IReadOnlyList<float> DecisionProbabilities => _decisionProbabilities;
        public float[] NormalizedPriorities => WeightVector.Normalize(_priorities);

        public void SetId(string id) => _id = id;
        public void SetDisplayName(string displayName) => _displayName = displayName;
        public void SetSearchDepth(int depth) => _searchDepth = Mathf.Clamp(depth, 1, MaxSearchDepth);
        public void SetMaxBranching(int branching) => _maxBranching = Mathf.Clamp(branching, 2, MaxOptionsPerNode);
        public void SetMaxActionsPerTurn(int actions) => _maxActionsPerTurn = Mathf.Clamp(actions, 1, MaxActions);
        public void SetPriority(AICriterion criterion, float value) => _priorities[(int)criterion] = Mathf.Max(0f, value);

        public void SetPriorities(float[] values)
        {
            if (values.Length != AICriteria.Count) throw new ArgumentException($"Expected {AICriteria.Count} priorities.");
            _priorities = (float[])values.Clone();
        }

        public void SetDecisionProbabilities(float[] values) => _decisionProbabilities = (float[])values.Clone();

        public AIProfile Clone()
        {
            var copy = (AIProfile)MemberwiseClone();
            copy._priorities = (float[])_priorities.Clone();
            copy._decisionProbabilities = (float[])_decisionProbabilities.Clone();
            return copy;
        }

        /// <summary>Repairs data read from a file that could have been edited by hand.</summary>
        public void EnsureValid()
        {
            if (_priorities == null || _priorities.Length != AICriteria.Count) Array.Resize(ref _priorities, AICriteria.Count);
            if (_decisionProbabilities == null || _decisionProbabilities.Length == 0) _decisionProbabilities = new[] { 1f };
        }
    }
}
