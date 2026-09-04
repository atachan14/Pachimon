using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pachimon.Map;
using Pachimon.Run;
using Pachimon.Trainer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class MapOverlayView : MonoBehaviour
    {
        private const string LayoutSettingsResourcePath = "UI/MapLayoutSettings";

        [field: SerializeField] public TMP_Text TitleText { get; private set; }
        [field: SerializeField] public TMP_Text BodyText { get; private set; }

        [SerializeField, Min(0f)] private float _transitionDuration = 0.25f;
        [SerializeField, Min(0f)] private float _partyEncounterApproachDuration = 2f;
        [SerializeField] private ScrollRect _mapScrollRect;
        [SerializeField] private RectTransform _scrollViewport;
        [SerializeField] private RectTransform _mapContent;
        [SerializeField] private RectTransform _edgeLayer;
        [SerializeField] private RectTransform _nodeLayer;
        [SerializeField] private MapNodeView _nodePrefab;
        [SerializeField] private CityMapNodeView _cityPrefab;
        [SerializeField] private MapEdgeView _edgePrefab;
        [SerializeField] private TrainerMapIconSet _trainerMapIconSet;
        [SerializeField] private TrainerMapIconCatalog _trainerMapIconCatalog;
        [SerializeField] private MapLayoutSettings _layoutSettings = new();
        [SerializeField] private MapLayoutSettingsAsset _layoutSettingsAsset;

        private RectTransform _panelRect;
        private RectTransform _viewportRect;
        private CanvasGroup _canvasGroup;
        private VerticalSlideTransition _slideTransition;
        private bool _isInitialized;
        private bool _isOpen;
        private RunMap _runMap;
        private RunState _runState;
        private TrainerStyleCatalog _trainerStyleCatalog;
        private MapLayout _mapLayout;
        private RunMap _builtMap;
        private readonly Dictionary<string, MapNodeView> _nodeViews = new();
        private readonly Dictionary<string, CityMapNodeView> _cityViews = new();
        private readonly Dictionary<string, Vector2> _partyEncounterOffsets = new();
        private readonly HashSet<string> _groupedNodeIds = new();
        private readonly List<EdgeBinding> _edgeViews = new();
        private readonly List<PartyBoundaryGuide> _partyBoundaryGuides = new();
        private readonly HashSet<string> _selectableNodeIds = new();
        private string _selectedNodeId;
        private string _currentPartyEncounterNodeId;
        private ScrollEdgeIndicator _scrollIndicator;
        private LayoutMode _layoutMode = LayoutMode.Compact;
        private Coroutine _partyApproachRoutine;

        private MapLayoutSettings ActiveLayoutSettings
        {
            get
            {
                if (_layoutSettingsAsset == null)
                {
                    _layoutSettingsAsset = Resources.Load<MapLayoutSettingsAsset>(
                        LayoutSettingsResourcePath);
                }

                return _layoutSettingsAsset != null
                    ? _layoutSettingsAsset.Settings
                    : _layoutSettings ??= new MapLayoutSettings();
            }
        }

        public bool IsOpen => _isOpen;
        public RectTransform EdgeLayer => _edgeLayer;
        public RectTransform NodeLayer => _nodeLayer;
        public MapLayout CurrentLayout => _mapLayout;

        public event Action<string> NodeSelected;
        public event Action<string> PartyCandidatesSelected;
        public event Action Opening;
        public event Action Closed;

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            if (_layoutMode == layoutMode)
            {
                return;
            }

            _layoutMode = layoutMode;
            RefreshMapLayout();
            ApplyMapLayout();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            _slideTransition?.Cancel();
            if (_partyApproachRoutine != null)
            {
                StopCoroutine(_partyApproachRoutine);
                _partyApproachRoutine = null;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!_isInitialized)
            {
                return;
            }

            RefreshMapLayout();
            BuildMapIfNeeded();
            ApplyMapLayout();
            _slideTransition?.SetSlideDistance(_viewportRect.rect.height);

            if (!_isOpen && _slideTransition?.IsRunning != true)
            {
                _slideTransition?.Snap(0f);
            }
        }

        public void Initialize(TMP_Text titleText, TMP_Text bodyText)
        {
            TitleText = titleText;
            BodyText = bodyText;
        }

        public void Open()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!EnsureInitialized())
            {
                return;
            }

            Opening?.Invoke();
            Canvas.ForceUpdateCanvases();
            RefreshMapLayout();
            BuildMapIfNeeded();
            ApplyMapLayout();
            RefreshNodeState();
            FocusCurrentNode();
            _isOpen = true;
            _slideTransition.Play(1f, _transitionDuration);
        }

        public void ReplayOpenTransition()
        {
            if (!EnsureInitialized())
            {
                return;
            }

            _slideTransition.Snap(0f);
            Open();
        }

        public void Close()
        {
            _isOpen = false;
            Closed?.Invoke();

            if (!gameObject.activeInHierarchy || !EnsureInitialized())
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            _slideTransition.Play(0f, _transitionDuration);
        }

        public void Render(
            RunMap runMap,
            RunState runState,
            IEnumerable<string> selectableNodeIds = null,
            TrainerStyleCatalog trainerStyleCatalog = null)
        {
            if (!ReferenceEquals(_runMap, runMap))
            {
                _partyEncounterOffsets.Clear();
            }

            _runMap = runMap;
            _runState = runState;
            _trainerStyleCatalog = trainerStyleCatalog;
            SetSelectableNodes(selectableNodeIds);
            RefreshMapLayout();
            BuildMapIfNeeded();
            ApplyMapLayout();
            RefreshNodeState();

            if (TitleText != null)
            {
                TitleText.text = "MAP";
            }

            if (BodyText == null)
            {
                return;
            }

            if (runMap == null)
            {
                BodyText.text = "Map is not generated.";
                return;
            }

            var builder = new StringBuilder();
            builder.Append("Nodes: ").Append(runMap.Nodes.Count)
                .Append(" / Current: ").AppendLine(runState?.CurrentNodeId ?? "-");

            foreach (var row in runMap.Rows)
            {
                builder.Append("Row ").Append(row.RowIndex).Append(": ");

                for (var index = 0; index < row.NodeIds.Count; index++)
                {
                    var node = runMap.GetNode(row.NodeIds[index]);
                    if (node == null)
                    {
                        continue;
                    }

                    if (index > 0)
                    {
                        builder.Append(" | ");
                    }

                    builder.Append(node.NodeId).Append('[').Append(node.NodeType).Append("] -> ")
                        .Append(string.Join(",", node.NextNodeIds));
                }

                builder.AppendLine();
            }

            BodyText.text = builder.ToString();
        }

        public void ConfigureMapScrollView(
            ScrollRect mapScrollRect,
            RectTransform scrollViewport,
            RectTransform mapContent,
            RectTransform edgeLayer,
            RectTransform nodeLayer)
        {
            _mapScrollRect = mapScrollRect;
            _scrollViewport = scrollViewport;
            _mapContent = mapContent;
            _edgeLayer = edgeLayer;
            _nodeLayer = nodeLayer;
            _scrollIndicator = ScrollEdgeIndicator.GetOrCreate(_mapScrollRect);
            RefreshMapLayout();
        }

        public void ConfigureMapPrefabs(
            MapNodeView nodePrefab,
            MapEdgeView edgePrefab,
            CityMapNodeView cityPrefab,
            TrainerMapIconSet trainerMapIconSet = null,
            TrainerMapIconCatalog trainerMapIconCatalog = null)
        {
            _nodePrefab = nodePrefab;
            _edgePrefab = edgePrefab;
            _cityPrefab = cityPrefab;
            _trainerMapIconSet = trainerMapIconSet;
            _trainerMapIconCatalog = trainerMapIconCatalog;
            ClearMap();
            BuildMapIfNeeded();
            ApplyMapLayout();
            RefreshNodeState();
        }

        public void SetSelectedNode(string nodeId)
        {
            _selectedNodeId = nodeId != null && _runMap?.GetNode(nodeId) != null
                ? nodeId
                : null;
            RefreshNodeState();
        }

        public void SetCurrentPartyEncounterMarker(string encounterNodeId)
        {
            _currentPartyEncounterNodeId = encounterNodeId;
            RefreshNodeState();
        }

        public bool PlayPartyEncounterApproach(
            string encounterNodeId,
            string sourceNodeId,
            string targetNodeId,
            Action onCompleted)
        {
            if (!_isOpen
                || !gameObject.activeInHierarchy
                || _mapLayout == null
                || !_nodeViews.TryGetValue(encounterNodeId, out var encounterView)
                || encounterView == null
                || !_mapLayout.TryGetNodePosition(encounterNodeId, out var encounterPosition)
                || !_mapLayout.TryGetNodePosition(sourceNodeId, out var sourcePosition)
                || !_mapLayout.TryGetNodePosition(targetNodeId, out var targetPosition))
            {
                return false;
            }

            if (_partyApproachRoutine != null)
            {
                StopCoroutine(_partyApproachRoutine);
            }

            var destination = Vector2.Lerp(sourcePosition, targetPosition, 0.5f);
            var targetOffset = destination - encounterPosition;
            _partyApproachRoutine = StartCoroutine(AnimatePartyEncounterApproach(
                encounterNodeId,
                encounterView,
                targetOffset,
                onCompleted));
            return true;
        }

        private bool EnsureInitialized()
        {
            if (_isInitialized)
            {
                return true;
            }

            _panelRect = transform as RectTransform;
            _viewportRect = transform.parent as RectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_panelRect == null || _viewportRect == null || _canvasGroup == null)
            {
                Debug.LogError(
                    $"{nameof(MapOverlayView)} on '{name}' requires a parent RectTransform and CanvasGroup.",
                    this);
                enabled = false;
                return false;
            }

            _panelRect.anchorMin = Vector2.zero;
            _panelRect.anchorMax = Vector2.one;
            _panelRect.offsetMin = Vector2.zero;
            _panelRect.offsetMax = Vector2.zero;
            _panelRect.pivot = new Vector2(0.5f, 0.5f);
            _panelRect.localPosition = new Vector3(
                _panelRect.localPosition.x,
                _panelRect.localPosition.y,
                0f);

            _canvasGroup.alpha = 1f;
            _slideTransition = new VerticalSlideTransition(
                this,
                _panelRect,
                _canvasGroup,
                () => _isOpen,
                applyAlpha: false);
            _slideTransition.SetSlideDistance(_viewportRect.rect.height);
            _scrollIndicator = ScrollEdgeIndicator.GetOrCreate(_mapScrollRect);
            _isInitialized = true;

            Canvas.ForceUpdateCanvases();
            _slideTransition.SetSlideDistance(_viewportRect.rect.height);
            _slideTransition.Snap(0f);
            return true;
        }

        private void RefreshMapLayout()
        {
            if (_runMap == null || _scrollViewport == null || _mapContent == null)
            {
                return;
            }

            var viewportSize = _scrollViewport.rect.size;
            if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            {
                return;
            }

            _mapLayout = MapLayoutCalculator.Calculate(
                _runMap,
                _runState?.RunSeed ?? 0,
                viewportSize,
                _layoutMode,
                Screen.width / Mathf.Max(1f, Screen.height),
                ActiveLayoutSettings);
            _mapContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _mapLayout.ContentSize.x);
            _mapContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _mapLayout.ContentSize.y);
        }

        private void BuildMapIfNeeded()
        {
            if (_runMap == null
                || _mapLayout == null
                || _nodeLayer == null
                || _edgeLayer == null
                || _nodePrefab == null
                || _edgePrefab == null)
            {
                return;
            }

            if (ReferenceEquals(_builtMap, _runMap))
            {
                return;
            }

            ClearMap();

            var displayEdges = _runMap.Nodes.Values
                .SelectMany(sourceNode => sourceNode.NextNodeIds
                    .Where(targetNodeId => _runMap.GetNode(targetNodeId) != null)
                    .Select(targetNodeId => new
                    {
                        SourceNodeId = sourceNode.NodeId,
                        TargetNodeId = targetNodeId,
                        SourceDisplayId = GetDisplayNodeId(sourceNode.NodeId),
                        TargetDisplayId = GetDisplayNodeId(targetNodeId),
                    }))
                .GroupBy(edge => (edge.SourceDisplayId, edge.TargetDisplayId))
                .Select(group => group
                    .OrderBy(edge => GetEdgeDisplayDistanceSquared(
                        edge.SourceNodeId,
                        edge.TargetNodeId))
                    .ThenBy(edge => edge.SourceNodeId)
                    .ThenBy(edge => edge.TargetNodeId)
                    .First())
                .ToArray();

            foreach (var edge in displayEdges)
            {
                var edgeView = Instantiate(_edgePrefab, _edgeLayer, false);
                edgeView.name = $"Edge_{edge.SourceNodeId}_{edge.TargetNodeId}";
                _edgeViews.Add(new EdgeBinding(
                    edge.SourceNodeId,
                    edge.TargetNodeId,
                    edgeView));
            }

            if (_cityPrefab != null)
            {
                foreach (var group in _runMap.NodeGroups.Values
                             .Where(group => group.NodeType == NodeType.City))
                {
                    var cityView = Instantiate(_cityPrefab, _nodeLayer, false);
                    cityView.name = $"City_{group.GroupId}";
                    _cityViews.Add(group.GroupId, cityView);

                    foreach (var nodeId in group.NodeIds)
                    {
                        _groupedNodeIds.Add(nodeId);
                    }
                }
            }

            foreach (var node in _runMap.Nodes.Values.Where(node => !_groupedNodeIds.Contains(node.NodeId)))
            {
                var nodeView = Instantiate(_nodePrefab, _nodeLayer, false);
                nodeView.name = $"Node_{node.NodeId}";
                _nodeViews.Add(node.NodeId, nodeView);
            }

            BuildPartyBoundaryGuides();

            _builtMap = _runMap;
            Debug.Log(
                $"Map view built: {_nodeViews.Count} nodes + {_cityViews.Count} cities "
                + $"/ {_edgeViews.Count} edges.",
                this);
        }

        private string GetDisplayNodeId(string nodeId)
        {
            var group = _runMap.GetNodeGroupForNode(nodeId);
            return group == null ? $"node:{nodeId}" : $"group:{group.GroupId}";
        }

        private float GetEdgeDisplayDistanceSquared(string sourceNodeId, string targetNodeId)
        {
            if (_mapLayout.TryGetNodePosition(sourceNodeId, out var source)
                && _mapLayout.TryGetNodePosition(targetNodeId, out var target))
            {
                return (target - source).sqrMagnitude;
            }

            return float.MaxValue;
        }

        private void ClearMap()
        {
            foreach (var nodeView in _nodeViews.Values)
            {
                if (nodeView != null)
                {
                    Destroy(nodeView.gameObject);
                }
            }

            foreach (var edgeBinding in _edgeViews)
            {
                if (edgeBinding.View != null)
                {
                    Destroy(edgeBinding.View.gameObject);
                }
            }

            foreach (var cityView in _cityViews.Values)
            {
                if (cityView != null)
                {
                    Destroy(cityView.gameObject);
                }
            }

            foreach (var guide in _partyBoundaryGuides)
            {
                guide.Destroy();
            }

            _nodeViews.Clear();
            _cityViews.Clear();
            _groupedNodeIds.Clear();
            _edgeViews.Clear();
            _partyBoundaryGuides.Clear();
            _builtMap = null;
        }

        private void ApplyMapLayout()
        {
            if (_mapLayout == null)
            {
                return;
            }

            foreach (var pair in _nodeViews)
            {
                if (!_mapLayout.TryGetNodePosition(pair.Key, out var position))
                {
                    continue;
                }

                var nodeRect = (RectTransform)pair.Value.transform;
                pair.Value.SetTrainerApproachOffset(
                    _partyEncounterOffsets.TryGetValue(pair.Key, out var encounterOffset)
                        ? encounterOffset
                        : Vector2.zero);
                pair.Value.ApplyNodeSize(_mapLayout.NodeSize);
                nodeRect.anchorMin = Vector2.zero;
                nodeRect.anchorMax = Vector2.zero;
                nodeRect.anchoredPosition = position;
                nodeRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    _mapLayout.NodeSize);
                nodeRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    _mapLayout.NodeSize);
            }

            foreach (var pair in _cityViews)
            {
                var group = _runMap.GetNodeGroup(pair.Key);
                if (group == null || group.NodeIds.Count != 2
                    || !_mapLayout.TryGetNodePosition(group.NodeIds[0], out var leftPosition)
                    || !_mapLayout.TryGetNodePosition(group.NodeIds[1], out var rightPosition))
                {
                    continue;
                }

                var cityRect = (RectTransform)pair.Value.transform;
                cityRect.anchorMin = Vector2.zero;
                cityRect.anchorMax = Vector2.zero;
                cityRect.anchoredPosition = (leftPosition + rightPosition) * 0.5f;
                cityRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    Mathf.Clamp(
                        _mapLayout.ColumnSpacing * 1.15f,
                        _mapLayout.NodeSize * 1.5f,
                        _mapLayout.NodeSize * 2f));
                cityRect.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    _mapLayout.NodeSize);
            }

            foreach (var edgeBinding in _edgeViews)
            {
                if (_mapLayout.TryGetNodePosition(edgeBinding.SourceNodeId, out var from)
                    && _mapLayout.TryGetNodePosition(edgeBinding.TargetNodeId, out var to))
                {
                    edgeBinding.View.Bind(from, to, false, false);
                }
            }

            ApplyPartyBoundaryGuideLayout();
        }

        private void RefreshNodeState()
        {
            if (_runMap == null || _runState == null)
            {
                return;
            }

            foreach (var pair in _nodeViews)
            {
                var node = _runMap.GetNode(pair.Key);
                if (node == null)
                {
                    continue;
                }

                var professorOffset = 0f;
                if (node.NodeType == NodeType.PartyEncounter
                    && _mapLayout.TryGetNodePosition(node.NodeId, out var encounterPosition))
                {
                    professorOffset = ActiveLayoutSettings.HorizontalPadding
                        + (_mapLayout.NodeSize * 0.5f)
                        - encounterPosition.x;
                }

                var isCurrent = node.NodeId == _runState.CurrentNodeId
                    || node.NodeId == _currentPartyEncounterNodeId;
                pair.Value.Bind(
                    node,
                    isCurrent,
                    _runState.ResolvedNodeIds.Contains(node.NodeId),
                    _selectableNodeIds.Contains(node.NodeId),
                    node.NodeId == _selectedNodeId,
                    GetTrainerIconSet(node),
                    GetTrainerColors(node),
                    NotifyNodeSelected,
                    NotifyPartyCandidatesSelected,
                    professorOffset);
            }

            foreach (var pair in _cityViews)
            {
                var group = _runMap.GetNodeGroup(pair.Key);
                if (group == null)
                {
                    continue;
                }

                var isCurrent = group.NodeIds.Contains(_runState.CurrentNodeId);
                var isResolved = group.NodeIds.Any(_runState.ResolvedNodeIds.Contains);
                var selectableTargetNodeId = group.NodeIds.FirstOrDefault(_selectableNodeIds.Contains);
                var inspectionTargetNodeId = selectableTargetNodeId ?? group.NodeIds.FirstOrDefault();

                pair.Value.Bind(
                    inspectionTargetNodeId,
                    isCurrent,
                    isResolved,
                    selectableTargetNodeId != null,
                    group.NodeIds.Contains(_selectedNodeId),
                    NotifyNodeSelected);
            }

            var currentGroup = _runMap.GetNodeGroupForNode(_runState.CurrentNodeId);
            var currentSourceNodeIds = currentGroup == null
                ? new HashSet<string> { _runState.CurrentNodeId }
                : new HashSet<string>(currentGroup.NodeIds);

            foreach (var edgeBinding in _edgeViews)
            {
                if (!_mapLayout.TryGetNodePosition(edgeBinding.SourceNodeId, out var from)
                    || !_mapLayout.TryGetNodePosition(edgeBinding.TargetNodeId, out var to))
                {
                    continue;
                }

                var isSelectable = _selectableNodeIds.Contains(edgeBinding.TargetNodeId)
                    && currentSourceNodeIds.Contains(edgeBinding.SourceNodeId);
                var isResolved = _runState.ResolvedNodeIds.Contains(edgeBinding.SourceNodeId)
                    && (_runState.ResolvedNodeIds.Contains(edgeBinding.TargetNodeId)
                        || edgeBinding.TargetNodeId == _runState.CurrentNodeId);
                edgeBinding.View.Bind(from, to, isResolved, isSelectable);
            }
        }

        private TrainerColorScheme? GetTrainerColors(MapNode node)
        {
            switch (node.Content)
            {
                case BattleNodeContent battle when battle.NodeReward != null:
                    return TrainerColorSchemeResolver.FromBattleReward(battle.NodeReward);
                case GymNodeContent gym when gym.NodeReward?.BadgeAttribute is { } attribute:
                    return TrainerColorSchemeResolver.FromAttribute(attribute);
                case EliteNodeContent elite:
                    var style = _trainerStyleCatalog?.Get(elite.TrainerProfile.StyleId);
                    return style != null
                        && TrainerColorSchemeResolver.TryFromTheme(style.Theme, out var colors)
                            ? colors
                            : null;
                case PartyEncounterNodeContent encounter:
                    var encounterStyle = _trainerStyleCatalog?.Get(
                        encounter.TrainerProfile.StyleId);
                    return encounterStyle != null
                        && TrainerColorSchemeResolver.TryFromTheme(
                            encounterStyle.Theme,
                            out var encounterColors)
                            ? encounterColors
                            : null;
                default:
                    return null;
            }
        }

        private TrainerMapIconSet GetTrainerIconSet(MapNode node)
        {
            var role = node.Content switch
            {
                GymNodeContent => TrainerRole.GymLeader,
                EliteNodeContent => TrainerRole.Elite,
                BattleNodeContent => TrainerRole.Normal,
                PartyEncounterNodeContent => TrainerRole.Normal,
                _ => TrainerRole.Normal,
            };
            return _trainerMapIconCatalog?.Get(role) ?? _trainerMapIconSet;
        }

        private void SetSelectableNodes(IEnumerable<string> selectableNodeIds)
        {
            _selectableNodeIds.Clear();
            if (selectableNodeIds != null)
            {
                foreach (var nodeId in selectableNodeIds)
                {
                    _selectableNodeIds.Add(nodeId);
                }
            }

            if (_selectedNodeId != null && !_selectableNodeIds.Contains(_selectedNodeId))
            {
                _selectedNodeId = null;
            }
        }

        private void NotifyNodeSelected(string nodeId)
        {
            NodeSelected?.Invoke(nodeId);
        }

        private void NotifyPartyCandidatesSelected(string nodeId)
        {
            PartyCandidatesSelected?.Invoke(nodeId);
        }

        private void FocusCurrentNode()
        {
            if (_mapScrollRect == null
                || _mapLayout == null
                || _runState == null
                || !_mapLayout.TryGetNodePosition(_runState.CurrentNodeId, out var nodePosition))
            {
                return;
            }

            var scrollableHeight = Mathf.Max(0f, _mapLayout.ContentSize.y - _scrollViewport.rect.height);
            if (scrollableHeight <= 0f)
            {
                _mapScrollRect.verticalNormalizedPosition = 0f;
                return;
            }

            var desiredViewportY = _scrollViewport.rect.height
                * ActiveLayoutSettings.CurrentNodeViewportRatio;
            var offsetFromBottom = Mathf.Clamp(nodePosition.y - desiredViewportY, 0f, scrollableHeight);
            _mapScrollRect.StopMovement();
            _mapScrollRect.verticalNormalizedPosition = offsetFromBottom / scrollableHeight;
        }

        private void BuildPartyBoundaryGuides()
        {
            if (_edgeLayer == null || _runMap == null)
            {
                return;
            }

            foreach (var encounter in _runMap.Nodes.Values
                         .Where(node => node.NodeType == NodeType.PartyEncounter))
            {
                var segments = new List<Image>();
                for (var index = 0; index < 18; index++)
                {
                    var segmentObject = new GameObject(
                        $"Boundary_{encounter.NodeId}_{index:00}",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    segmentObject.layer = gameObject.layer;
                    var rect = segmentObject.GetComponent<RectTransform>();
                    rect.SetParent(_edgeLayer, false);
                    rect.anchorMin = rect.anchorMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    var image = segmentObject.GetComponent<Image>();
                    image.color = new Color(0.20f, 0.24f, 0.23f, 0.55f);
                    image.raycastTarget = false;
                    segments.Add(image);
                }

                _partyBoundaryGuides.Add(new PartyBoundaryGuide(
                    encounter.NodeId,
                    segments));
            }
        }

        private void ApplyPartyBoundaryGuideLayout()
        {
            if (_mapLayout == null)
            {
                return;
            }

            var left = ActiveLayoutSettings.HorizontalPadding + (_mapLayout.NodeSize * 0.5f);
            var right = _mapLayout.ContentSize.x - left;
            foreach (var guide in _partyBoundaryGuides)
            {
                if (!_mapLayout.TryGetNodePosition(guide.EncounterNodeId, out var encounterPosition))
                {
                    continue;
                }

                var slotWidth = Mathf.Max(1f, (right - left) / guide.Segments.Count);
                for (var index = 0; index < guide.Segments.Count; index++)
                {
                    var rect = (RectTransform)guide.Segments[index].transform;
                    rect.anchoredPosition = new Vector2(
                        left + ((index + 0.5f) * slotWidth),
                        encounterPosition.y);
                    rect.sizeDelta = new Vector2(slotWidth * 0.55f, 3f);
                }
            }
        }

        private IEnumerator AnimatePartyEncounterApproach(
            string encounterNodeId,
            MapNodeView encounterView,
            Vector2 targetOffset,
            Action onCompleted)
        {
            var duration = Mathf.Max(0.01f, _partyEncounterApproachDuration);
            var elapsed = 0f;
            encounterView.SetTrainerApproachOffset(Vector2.zero);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                progress = 1f - Mathf.Pow(1f - progress, 3f);
                encounterView.SetTrainerApproachOffset(
                    Vector2.LerpUnclamped(Vector2.zero, targetOffset, progress));
                yield return null;
            }

            encounterView.SetTrainerApproachOffset(targetOffset);
            _partyEncounterOffsets[encounterNodeId] = targetOffset;
            _partyApproachRoutine = null;
            onCompleted?.Invoke();
        }

        private sealed class EdgeBinding
        {
            public EdgeBinding(string sourceNodeId, string targetNodeId, MapEdgeView view)
            {
                SourceNodeId = sourceNodeId;
                TargetNodeId = targetNodeId;
                View = view;
            }

            public string SourceNodeId { get; }
            public string TargetNodeId { get; }
            public MapEdgeView View { get; }
        }

        private sealed class PartyBoundaryGuide
        {
            public PartyBoundaryGuide(string encounterNodeId, List<Image> segments)
            {
                EncounterNodeId = encounterNodeId;
                Segments = segments;
            }

            public string EncounterNodeId { get; }
            public List<Image> Segments { get; }

            public void Destroy()
            {
                foreach (var segment in Segments)
                {
                    if (segment != null)
                    {
                        UnityEngine.Object.Destroy(segment.gameObject);
                    }
                }
            }
        }
    }
}
