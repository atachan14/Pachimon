using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pachimon.UI
{
    public sealed class StartScreen : NodeScreen
    {
#if UNITY_EDITOR
        private const string DefaultProfessorSpritePath =
            "Assets/Art/Characters/Professor/professor.png";
#endif

        [Header("Presentation")]
        [SerializeField] private Sprite _professorSprite;
        [SerializeField, Min(0f)] private float _panDuration = 0.35f;
        [SerializeField, Min(0f)] private float _candidateFadeDuration = 0.6f;
        [SerializeField, Min(0f)] private float _candidateArrangeDuration = 0.45f;
        [SerializeField, Min(1f)] private float _selectedCandidateScale = 1.08f;

        [SerializeField] private RectTransform _candidatePanel;
        [SerializeField] private GridLayoutGroup _candidateGrid;
        private RectTransform _panContent;
        private RectTransform _professorLayer;
        private CanvasGroup _candidateCanvasGroup;
        private CanvasGroup _professorCanvasGroup;
        private Coroutine _panRoutine;
        private Coroutine _confirmationRoutine;
        private bool _showingCandidates;
        private bool _showingConfirmation;
        private readonly HashSet<string> _confirmationSelectedIds = new();
        private readonly Dictionary<string, int> _confirmationSelectedOrder = new();
        private readonly List<StartCandidateCardView> _candidateCards = new();
        private IReadOnlyList<StartCandidateCardContent> _contents =
            Array.Empty<StartCandidateCardContent>();

        private void OnRectTransformDimensionsChange()
        {
            if (_panContent == null)
            {
                return;
            }

            if (_panRoutine != null)
            {
                StopCoroutine(_panRoutine);
                _panRoutine = null;
            }

            RefreshPresentationLayout();
            ApplyPresentationImmediately(_showingCandidates);
            RefreshCandidateLayout();
        }

        private void OnDisable()
        {
            if (_panRoutine != null)
            {
                StopCoroutine(_panRoutine);
                _panRoutine = null;
                ApplyPresentationImmediately(_showingCandidates);
            }

            if (_confirmationRoutine != null)
            {
                StopCoroutine(_confirmationRoutine);
                _confirmationRoutine = null;
                ApplyCandidateConfirmationImmediately();
            }
        }

        public void ShowCandidates(
            IReadOnlyList<StartCandidateCardContent> contents,
            Action<string> onCandidateClicked)
        {
            EnsureCandidatePanel();
            _contents = contents ?? Array.Empty<StartCandidateCardContent>();
            RebuildCandidateCards(onCandidateClicked);
            _candidatePanel.gameObject.SetActive(true);
            ApplyPresentationImmediately(_showingCandidates);
            ResetCandidatePresentation();
            RefreshCandidateLayout();
        }

        public void ShowCandidatePanel()
        {
            EnsureCandidatePanel();
            _candidatePanel.gameObject.SetActive(true);
            SetPresentation(true, true);
        }

        public void HideCandidatePanel()
        {
            EnsureCandidatePanel();
            SetPresentation(false, false);
        }

        public void ShowCandidateSelection()
        {
            if (!_showingConfirmation)
            {
                return;
            }

            ResetCandidatePresentation();
        }

        public void ShowCandidateConfirmation(IReadOnlyList<string> selectedIds)
        {
            if (_showingConfirmation)
            {
                return;
            }

            var selectedOrder = BuildSelectedOrder(selectedIds);
            if (selectedOrder.Count == 0)
            {
                return;
            }

            _showingConfirmation = true;
            _confirmationSelectedIds.Clear();
            _confirmationSelectedIds.UnionWith(selectedOrder.Keys);
            _confirmationSelectedOrder.Clear();
            foreach (var pair in selectedOrder)
            {
                _confirmationSelectedOrder.Add(pair.Key, pair.Value);
            }

            if ((_candidateFadeDuration <= 0f && _candidateArrangeDuration <= 0f)
                || !isActiveAndEnabled)
            {
                ApplyCandidateConfirmationImmediately();
                return;
            }

            _confirmationRoutine = StartCoroutine(
                AnimateCandidateConfirmation(selectedOrder));
        }

        public void SetCandidateSelections(IReadOnlyList<string> selectedIds)
        {
            for (var index = 0; index < _candidateCards.Count && index < _contents.Count; index++)
            {
                var selectionIndex = selectedIds == null
                    ? -1
                    : selectedIds.ToList().IndexOf(_contents[index].InstanceId);
                _candidateCards[index].SetSelectionOrder(selectionIndex + 1);
                if (_showingConfirmation && selectionIndex >= 0)
                {
                    _candidateCards[index].SetConfirmationProgress(1f);
                }
            }
        }

        public void SetFocusedCandidate(string instanceId)
        {
            for (var index = 0; index < _candidateCards.Count && index < _contents.Count; index++)
            {
                _candidateCards[index].SetFocused(
                    !string.IsNullOrEmpty(instanceId)
                    && _contents[index].InstanceId == instanceId);
            }
        }

        private void EnsureCandidatePanel()
        {
            EnsurePresentationMask();

            if (_panContent != null
                && _candidatePanel != null
                && _candidateGrid != null
                && _professorLayer != null)
            {
                return;
            }

            CreatePresentationHierarchy();
            RefreshPresentationLayout();
            ApplyPresentationImmediately(false);
        }

        private void EnsurePresentationMask()
        {
            if (TryGetComponent<RectMask2D>(out _))
            {
                return;
            }

            gameObject.AddComponent<RectMask2D>();
        }

        private void CreatePresentationHierarchy()
        {
            var panObject = new GameObject("PanContent", typeof(RectTransform));
            panObject.layer = gameObject.layer;
            _panContent = panObject.GetComponent<RectTransform>();
            _panContent.SetParent(transform, false);

            var panelObject = new GameObject(
                "CandidatePanel",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(GridLayoutGroup));
            panelObject.layer = gameObject.layer;
            _candidatePanel = panelObject.GetComponent<RectTransform>();
            _candidatePanel.SetParent(_panContent, false);
            _candidateCanvasGroup = panelObject.GetComponent<CanvasGroup>();

            _candidateGrid = panelObject.GetComponent<GridLayoutGroup>();
            _candidateGrid.padding = new RectOffset(14, 14, 14, 14);
            _candidateGrid.spacing = new Vector2(10f, 10f);
            _candidateGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _candidateGrid.constraintCount = 3;
            _candidateGrid.childAlignment = TextAnchor.MiddleCenter;

            var professorObject = new GameObject(
                "ProfessorLayer",
                typeof(RectTransform),
                typeof(CanvasGroup));
            professorObject.layer = gameObject.layer;
            _professorLayer = professorObject.GetComponent<RectTransform>();
            _professorLayer.SetParent(_panContent, false);
            _professorCanvasGroup = professorObject.GetComponent<CanvasGroup>();

            var graphicObject = new GameObject(
                "ProfessorGraphic",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            graphicObject.layer = gameObject.layer;
            graphicObject.transform.SetParent(_professorLayer, false);
            var graphicRect = graphicObject.GetComponent<RectTransform>();
            graphicRect.anchorMin = new Vector2(0.08f, 0.03f);
            graphicRect.anchorMax = new Vector2(0.92f, 0.97f);
            graphicRect.offsetMin = Vector2.zero;
            graphicRect.offsetMax = Vector2.zero;
            var graphic = graphicObject.GetComponent<Image>();
            graphic.sprite = ResolveProfessorSprite();
            graphic.preserveAspect = true;
            graphic.raycastTarget = false;
        }

        private Sprite ResolveProfessorSprite()
        {
#if UNITY_EDITOR
            if (_professorSprite == null)
            {
                _professorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    DefaultProfessorSpritePath);
            }
#endif

            if (_professorSprite == null)
            {
                Debug.LogError(
                    $"{nameof(StartScreen)} on '{name}' is missing its professor sprite.",
                    this);
            }

            return _professorSprite;
        }

        private void SetPresentation(bool showCandidates, bool animate)
        {
            if (_showingCandidates == showCandidates)
            {
                if (_panRoutine == null)
                {
                    ApplyPresentationImmediately(showCandidates);
                }

                return;
            }

            if (_panRoutine != null)
            {
                StopCoroutine(_panRoutine);
                _panRoutine = null;
            }

            _showingCandidates = showCandidates;
            if (!animate || _panDuration <= 0f || !isActiveAndEnabled)
            {
                ApplyPresentationImmediately(showCandidates);
                return;
            }

            _panRoutine = StartCoroutine(AnimatePresentation(showCandidates));
        }

        private IEnumerator AnimatePresentation(bool showCandidates)
        {
            var width = Mathf.Max(1f, ((RectTransform)transform).rect.width);
            var startPosition = _panContent.anchoredPosition;
            var targetPosition = new Vector2(showCandidates ? 0f : -width, 0f);
            var startCandidateAlpha = _candidateCanvasGroup.alpha;
            var startProfessorAlpha = _professorCanvasGroup.alpha;
            var targetCandidateAlpha = showCandidates ? 1f : 0f;
            var targetProfessorAlpha = showCandidates ? 0f : 1f;

            _candidateCanvasGroup.blocksRaycasts = false;
            _candidateCanvasGroup.interactable = false;

            var elapsed = 0f;
            while (elapsed < _panDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / _panDuration);
                var eased = progress * progress * (3f - (2f * progress));
                _panContent.anchoredPosition = Vector2.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    eased);
                _candidateCanvasGroup.alpha = Mathf.Lerp(
                    startCandidateAlpha,
                    targetCandidateAlpha,
                    eased);
                _professorCanvasGroup.alpha = Mathf.Lerp(
                    startProfessorAlpha,
                    targetProfessorAlpha,
                    eased);
                yield return null;
            }

            ApplyPresentationImmediately(showCandidates);
            _panRoutine = null;
        }

        private IEnumerator AnimateCandidateConfirmation(
            IReadOnlyDictionary<string, int> selectedOrder)
        {
            SetCandidateInteraction(false);
            yield return AnimateUnselectedCandidateFade(selectedOrder);

            var startPositions = CaptureSelectedCandidatePositions(selectedOrder);
            ReorderCandidateCards(selectedOrder);
            HideUnselectedCandidates(selectedOrder);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_candidatePanel);
            var targetPositions = CaptureSelectedCandidatePositions(selectedOrder);
            _candidateGrid.enabled = false;

            foreach (var pair in startPositions)
            {
                pair.Key.GetComponent<RectTransform>().anchoredPosition = pair.Value;
            }

            yield return AnimateSelectedCandidateArrangement(startPositions, targetPositions);

            _candidateGrid.enabled = true;
            ApplyCandidateConfirmationImmediately();
            _confirmationRoutine = null;
        }

        private IEnumerator AnimateUnselectedCandidateFade(
            IReadOnlyDictionary<string, int> selectedOrder)
        {
            if (_candidateFadeDuration <= 0f)
            {
                SetUnselectedCandidateAlpha(selectedOrder, 0f);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < _candidateFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / _candidateFadeDuration);
                var eased = progress * progress * (3f - (2f * progress));
                SetUnselectedCandidateAlpha(selectedOrder, 1f - eased);
                yield return null;
            }

            SetUnselectedCandidateAlpha(selectedOrder, 0f);
        }

        private IEnumerator AnimateSelectedCandidateArrangement(
            IReadOnlyDictionary<StartCandidateCardView, Vector2> startPositions,
            IReadOnlyDictionary<StartCandidateCardView, Vector2> targetPositions)
        {
            if (_candidateArrangeDuration <= 0f)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < _candidateArrangeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / _candidateArrangeDuration);
                var eased = progress * progress * (3f - (2f * progress));
                foreach (var pair in startPositions)
                {
                    var card = pair.Key;
                    var rect = card.GetComponent<RectTransform>();
                    rect.anchoredPosition = Vector2.LerpUnclamped(
                        pair.Value,
                        targetPositions[card],
                        eased);
                    card.transform.localScale = Vector3.one * Mathf.Lerp(
                        1f,
                        _selectedCandidateScale,
                        eased);
                    card.SetConfirmationProgress(eased);
                }

                yield return null;
            }
        }

        private void ApplyCandidateConfirmationImmediately()
        {
            if (!_showingConfirmation)
            {
                return;
            }

            _candidateGrid.enabled = true;
            ReorderCandidateCards(_confirmationSelectedOrder);
            for (var index = 0; index < _candidateCards.Count; index++)
            {
                var card = _candidateCards[index];
                var isSelected = _confirmationSelectedIds.Contains(
                    _contents[index].InstanceId);
                card.gameObject.SetActive(isSelected);
                if (!isSelected)
                {
                    continue;
                }

                var canvasGroup = card.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                card.transform.localScale = Vector3.one * _selectedCandidateScale;
                card.SetConfirmationProgress(1f);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_candidatePanel);
        }

        private void ResetCandidatePresentation()
        {
            if (_confirmationRoutine != null)
            {
                StopCoroutine(_confirmationRoutine);
                _confirmationRoutine = null;
            }

            _showingConfirmation = false;
            _confirmationSelectedIds.Clear();
            _confirmationSelectedOrder.Clear();
            _candidateGrid.enabled = true;
            for (var index = 0; index < _candidateCards.Count; index++)
            {
                var card = _candidateCards[index];
                card.gameObject.SetActive(true);
                card.transform.SetSiblingIndex(index);
                card.transform.localScale = Vector3.one;
                var canvasGroup = card.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_candidatePanel);
        }

        private Dictionary<StartCandidateCardView, Vector2> CaptureSelectedCandidatePositions(
            IReadOnlyDictionary<string, int> selectedOrder)
        {
            var positions = new Dictionary<StartCandidateCardView, Vector2>();
            for (var index = 0; index < _candidateCards.Count; index++)
            {
                if (!selectedOrder.ContainsKey(_contents[index].InstanceId))
                {
                    continue;
                }

                var card = _candidateCards[index];
                positions.Add(
                    card,
                    card.GetComponent<RectTransform>().anchoredPosition);
            }

            return positions;
        }

        private void HideUnselectedCandidates(
            IReadOnlyDictionary<string, int> selectedOrder)
        {
            for (var index = 0; index < _candidateCards.Count; index++)
            {
                if (!selectedOrder.ContainsKey(_contents[index].InstanceId))
                {
                    _candidateCards[index].gameObject.SetActive(false);
                }
            }
        }

        private void SetUnselectedCandidateAlpha(
            IReadOnlyDictionary<string, int> selectedOrder,
            float alpha)
        {
            for (var index = 0; index < _candidateCards.Count; index++)
            {
                if (selectedOrder.ContainsKey(_contents[index].InstanceId))
                {
                    continue;
                }

                _candidateCards[index].GetComponent<CanvasGroup>().alpha = alpha;
            }
        }

        private Dictionary<string, int> BuildSelectedOrder(IReadOnlyList<string> selectedIds)
        {
            var selectedOrder = new Dictionary<string, int>();
            if (selectedIds == null)
            {
                return selectedOrder;
            }

            for (var index = 0; index < selectedIds.Count; index++)
            {
                selectedOrder[selectedIds[index]] = index;
            }

            return selectedOrder;
        }

        private void ReorderCandidateCards(IReadOnlyDictionary<string, int> selectedOrder)
        {
            var orderedCards = _candidateCards
                .Select((card, index) => new
                {
                    Card = card,
                    OriginalIndex = index,
                    SelectionIndex = selectedOrder.TryGetValue(
                        _contents[index].InstanceId,
                        out var selectedIndex)
                            ? selectedIndex
                            : int.MaxValue,
                })
                .OrderBy(entry => entry.SelectionIndex)
                .ThenBy(entry => entry.OriginalIndex)
                .ToArray();

            for (var index = 0; index < orderedCards.Length; index++)
            {
                orderedCards[index].Card.transform.SetSiblingIndex(index);
            }
        }

        private void SetCandidateInteraction(bool enabled)
        {
            foreach (var card in _candidateCards)
            {
                var canvasGroup = card.GetComponent<CanvasGroup>();
                canvasGroup.interactable = enabled;
                canvasGroup.blocksRaycasts = enabled;
            }
        }

        private void ApplyPresentationImmediately(bool showCandidates)
        {
            if (_panContent == null
                || _candidateCanvasGroup == null
                || _professorCanvasGroup == null)
            {
                return;
            }

            var width = Mathf.Max(1f, ((RectTransform)transform).rect.width);
            _panContent.anchoredPosition = new Vector2(showCandidates ? 0f : -width, 0f);
            _candidateCanvasGroup.alpha = showCandidates ? 1f : 0f;
            _candidateCanvasGroup.blocksRaycasts = showCandidates;
            _candidateCanvasGroup.interactable = showCandidates;
            _professorCanvasGroup.alpha = showCandidates ? 0f : 1f;
            _professorCanvasGroup.blocksRaycasts = false;
            _professorCanvasGroup.interactable = false;
        }

        private void RefreshPresentationLayout()
        {
            if (_panContent == null || _candidatePanel == null || _professorLayer == null)
            {
                return;
            }

            var width = Mathf.Max(1f, ((RectTransform)transform).rect.width);
            _panContent.anchorMin = new Vector2(0f, 0f);
            _panContent.anchorMax = new Vector2(0f, 1f);
            _panContent.pivot = new Vector2(0f, 0.5f);
            _panContent.sizeDelta = new Vector2(width * 2f, 0f);

            ConfigurePage(_candidatePanel, 0f, width, 12f);
            ConfigurePage(_professorLayer, width, width, 0f);
        }

        private static void ConfigurePage(
            RectTransform page,
            float positionX,
            float width,
            float inset)
        {
            page.anchorMin = new Vector2(0f, 0f);
            page.anchorMax = new Vector2(0f, 1f);
            page.pivot = new Vector2(0f, 0.5f);
            page.anchoredPosition = new Vector2(positionX + inset, 0f);
            page.sizeDelta = new Vector2(width - (inset * 2f), -(inset * 2f));
        }

        private void RebuildCandidateCards(Action<string> onCandidateClicked)
        {
            foreach (var card in _candidateCards)
            {
                if (card != null) Destroy(card.gameObject);
            }

            _candidateCards.Clear();
            foreach (var content in _contents)
            {
                var card = CreateCandidateCard(_candidatePanel);
                card.Bind(content, onCandidateClicked);
                _candidateCards.Add(card);
            }
        }

        private static StartCandidateCardView CreateCandidateCard(RectTransform parent)
        {
            var cardObject = new GameObject(
                "CandidateCard",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(StartCandidateCardView));
            cardObject.layer = parent.gameObject.layer;
            cardObject.transform.SetParent(parent, false);

            var graphicArea = new GameObject("GraphicArea", typeof(RectTransform));
            graphicArea.layer = cardObject.layer;
            graphicArea.transform.SetParent(cardObject.transform, false);
            SetAnchors(
                graphicArea.GetComponent<RectTransform>(),
                new Vector2(0.08f, 0.23f),
                new Vector2(0.92f, 0.98f));

            var graphicObject = new GameObject(
                "GraphicButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(Button),
                typeof(AspectRatioFitter));
            graphicObject.layer = cardObject.layer;
            graphicObject.transform.SetParent(graphicArea.transform, false);
            var graphic = graphicObject.GetComponent<Image>();
            var focusOutline = graphicObject.GetComponent<Outline>();
            focusOutline.effectColor = GameUiPalette.ButtonAccent;
            focusOutline.effectDistance = new Vector2(4f, -4f);
            focusOutline.useGraphicAlpha = false;
            focusOutline.enabled = false;
            var button = graphicObject.GetComponent<Button>();
            button.targetGraphic = graphic;
            var aspect = graphicObject.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1f;

            var nameText = CreateText("NameText", cardObject.transform, 18f, FontStyles.Bold);
            SetAnchors(nameText.rectTransform, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.27f));

            var orderText = CreateText("SelectionOrderText", cardObject.transform, 15f, FontStyles.Bold);
            orderText.color = GameUiPalette.ButtonAccent;
            orderText.overflowMode = TextOverflowModes.Overflow;
            SetAnchors(orderText.rectTransform, new Vector2(0.12f, 0.01f), new Vector2(0.88f, 0.16f));

            var view = cardObject.GetComponent<StartCandidateCardView>();
            view.Configure(graphic, nameText, orderText, button, focusOutline);
            return view;
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            FontStyles fontStyle)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAlignmentOptions.Center;
            text.color = GameUiPalette.PrimaryText;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void RefreshCandidateLayout()
        {
            if (_candidatePanel == null || _candidateGrid == null)
            {
                return;
            }

            var width = Mathf.Max(1f, _candidatePanel.rect.width - _candidateGrid.padding.horizontal);
            var height = Mathf.Max(1f, _candidatePanel.rect.height - _candidateGrid.padding.vertical);
            _candidateGrid.cellSize = new Vector2(
                Mathf.Max(1f, (width - (_candidateGrid.spacing.x * 2f)) / 3f),
                Mathf.Max(1f, (height - (_candidateGrid.spacing.y * 2f)) / 3f));
        }
    }
}
