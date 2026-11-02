using System;
using System.Collections.Generic;
using LearnLanguages.Gameplay.AI;
using LearnLanguages.Localization;
using LearnLanguages.Map.Logic;
using LearnLanguages.UI.Common;
using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.UI.AI
{
    /// <summary>
    /// Debug view of an AI turn. Replays the recorded steps of the minimax one by one: each row of the tree holds the
    /// options of a node on the current path, so the path through a tree of any size stays readable.
    /// </summary>
    public sealed class AIDecisionTreeView : MonoBehaviour, IAIDecisionViewer
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _progress;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private RectTransform _treeArea;
        [SerializeField] private TMP_Text[] _rowLabels;
        [SerializeField] private ActionButtonView _back;
        [SerializeField] private ActionButtonView _forward;
        [SerializeField] private ActionButtonView _toEnd;
        [SerializeField] private ActionButtonView _skip;
        [SerializeField] private float _maxNodeWidth = 170f;
        [SerializeField] private float _nodeGap = 6f;
        [SerializeField] private float _nodeHeight = 62f;
        [SerializeField] private Color _lineColor = new Color(0.7f, 0.74f, 0.8f);

        private readonly List<AITreeNodeView> _nodes = new List<AITreeNodeView>();
        private readonly List<AITreeLineView> _lines = new List<AITreeLineView>();
        private readonly List<AITraceNode> _path = new List<AITraceNode>();

        private AITreeNodeView.Pool _nodePool;
        private AITreeLineView.Pool _linePool;
        private AIDecisionTextFormatter _text;
        private ILocalizationService _localization;
        private AIDecisionPlayback _playback;
        private Action _onClosed;

        [Inject]
        public void Construct(AITreeNodeView.Pool nodePool, AITreeLineView.Pool linePool, ILocalizationService localization,
            IGameMap map)
        {
            _nodePool = nodePool;
            _linePool = linePool;
            _localization = localization;
            _text = new AIDecisionTextFormatter(localization, map);
        }

        private void Awake() => _panel.SetActive(false);

        public void Show(AIDecisionTrace trace, Action onClosed)
        {
            _playback = new AIDecisionPlayback(trace);
            _onClosed = onClosed;
            _panel.SetActive(true);
            Refresh();
        }

        private void Run(Action<AIDecisionPlayback> action)
        {
            action(_playback);
            Refresh();
        }

        private void Close()
        {
            ClearTree();
            _panel.SetActive(false);
            _playback = null;
            var onClosed = _onClosed;
            _onClosed = null;
            onClosed?.Invoke();
        }

        private void Refresh()
        {
            _title.text = _text.Title(_playback.Trace);
            _progress.text = _text.Progress(_playback);
            _description.text = _text.Describe(_playback);

            _back.Bind(_localization.Get("ai.back"), null, _playback.CanStepBack, () => Run(p => p.StepBack()));
            _forward.Bind(_localization.Get("ai.forward"), null, _playback.CanStepForward, () => Run(p => p.StepForward()));
            _toEnd.Bind(_localization.Get("ai.to_end"), null, _playback.CanStepForward, () => Run(p => p.ToEndOfTree()));
            _skip.Bind(_localization.Get("ai.skip"), null, true, Close);

            DrawTree();
        }

        private void DrawTree()
        {
            ClearTree();
            BuildPath();

            var area = _treeArea.rect;
            var rowHeight = _rowLabels.Length > 1 ? area.height / _rowLabels.Length : area.height;
            var root = _path[0];

            var rootPosition = new Vector2(0f, -rowHeight * 0.5f);
            SpawnNode(root, _text.RootLabel, rootPosition, new Vector2(_maxNodeWidth, _nodeHeight));
            SetRowLabel(0, string.Empty);

            var parentPosition = rootPosition;
            for (var row = 1; row < _rowLabels.Length; row++)
            {
                var parent = row - 1 < _path.Count ? _path[row - 1] : null;
                var children = parent != null ? VisibleChildren(parent) : null;
                if (children == null || children.Count == 0)
                {
                    SetRowLabel(row, string.Empty);
                    continue;
                }
                SetRowLabel(row, _text.RowLabel(parent.IsMaximizing));

                var width = Mathf.Min(_maxNodeWidth, (area.width - _nodeGap * children.Count) / children.Count);
                var startX = -(children.Count - 1) * (width + _nodeGap) * 0.5f;
                var y = -rowHeight * (row + 0.5f);
                var nextParentPosition = parentPosition;
                for (var i = 0; i < children.Count; i++)
                {
                    var position = new Vector2(startX + i * (width + _nodeGap), y);
                    SpawnLine(parentPosition + Vector2.down * (_nodeHeight * 0.5f), position + Vector2.up * (_nodeHeight * 0.5f));
                    SpawnNode(children[i], _text.Action(children[i].Action), position, new Vector2(width, _nodeHeight));
                    if (row < _path.Count && children[i] == _path[row]) nextParentPosition = position;
                }
                parentPosition = nextParentPosition;
            }
        }

        private List<AITraceNode> VisibleChildren(AITraceNode parent)
        {
            var result = new List<AITraceNode>();
            foreach (var id in parent.Children)
            {
                var child = _playback.Trace.Nodes[id];
                if (_playback.IsGenerated(child)) result.Add(child);
            }
            return result;
        }

        /// <summary>From the root of the tree to the node the algorithm is in.</summary>
        private void BuildPath()
        {
            _path.Clear();
            var nodes = _playback.Trace.Nodes;
            var node = nodes[_playback.Current.NodeId];
            while (true)
            {
                _path.Insert(0, node);
                if (node.ParentId < 0) break;
                node = nodes[node.ParentId];
            }
        }

        private void SpawnNode(AITraceNode node, string label, Vector2 position, Vector2 size)
        {
            var view = _nodePool.Spawn();
            view.transform.SetParent(_treeArea, false);
            view.transform.SetAsLastSibling();
            var value = _playback.IsFinished(node) ? _text.Value(node.Value) : string.Empty;
            view.Bind(label, value, StateOf(node), position, size);
            _nodes.Add(view);
        }

        private void SpawnLine(Vector2 from, Vector2 to)
        {
            var line = _linePool.Spawn();
            line.transform.SetParent(_treeArea, false);
            line.transform.SetAsFirstSibling();
            line.Bind(from, to, _lineColor);
            _lines.Add(line);
        }

        private AITreeNodeState StateOf(AITraceNode node)
        {
            var step = _playback.Current;
            if (node.Id == step.NodeId && !(step.PickedRank >= 0)) return AITreeNodeState.Current;
            if (step.PickedRank >= 0 && node.Action != null && node.Action == step.Ranking[step.PickedRank].Action)
                return AITreeNodeState.Chosen;
            if (_path.Contains(node)) return AITreeNodeState.OnPath;
            if (_playback.IsPruned(node)) return AITreeNodeState.Pruned;
            return _playback.IsFinished(node) ? AITreeNodeState.Finished : AITreeNodeState.Waiting;
        }

        private void SetRowLabel(int row, string text) => _rowLabels[row].text = text;

        private void ClearTree()
        {
            foreach (var node in _nodes) _nodePool.Despawn(node);
            _nodes.Clear();
            foreach (var line in _lines) _linePool.Despawn(line);
            _lines.Clear();
        }
    }
}
