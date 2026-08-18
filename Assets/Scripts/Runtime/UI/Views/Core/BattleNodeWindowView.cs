using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class BattleNodeWindowView : MonoBehaviour
    {
        [SerializeField] private Button[] _tabButtons = Array.Empty<Button>();
        [SerializeField] private GameObject[] _tabPanels = Array.Empty<GameObject>();
        [SerializeField] private TrainerTabView _trainerTab;
        [SerializeField] private PachimonTabView[] _pachimonTabs = Array.Empty<PachimonTabView>();
        private readonly List<UnityAction> _tabActions = new();
        private readonly List<PaneTabNavigationView> _pageNavigations = new();
        private readonly List<ScrollEdgeIndicator> _scrollIndicators = new();
        private bool _reverseVisualOrder;

        public PachimonTabView PachimonTabTemplate =>
            _pachimonTabs != null && _pachimonTabs.Length > 0 ? _pachimonTabs[0] : null;
        public int SelectedTabIndex { get; private set; }

        private void OnEnable()
        {
            ApplyFlexibleTabWidths();
            ApplyTabVisualOrder();
            if (_tabActions.Count == 0)
            {
                WireTabListeners();
            }
        }

        private void OnDestroy() => RemoveTabListeners();

        public void Configure(
            Button[] tabButtons,
            GameObject[] tabPanels,
            TrainerTabView trainerTab,
            PachimonTabView[] pachimonTabs)
        {
            RemoveTabListeners();
            _tabButtons = tabButtons;
            _tabPanels = tabPanels;
            _trainerTab = trainerTab;
            _pachimonTabs = pachimonTabs;
            ApplyFlexibleTabWidths();
            WireTabListeners();
            ShowTab(0);
            EnsurePageNavigation();
        }

        public void ConfigureTrainerTab(TrainerTabView trainerTab)
        {
            _trainerTab = trainerTab;
        }

        public void SetVisualOrderReversed(bool isReversed)
        {
            _reverseVisualOrder = isReversed;
            ApplyTabVisualOrder();
            RefreshPageNavigation();
        }

        public void Bind(
            TrainerPreviewContent trainerPreview,
            IReadOnlyList<PachimonPreviewContent> pachimonPreviews)
        {
            if (_tabActions.Count == 0)
            {
                WireTabListeners();
            }

            _trainerTab?.Bind(trainerPreview);
            for (var index = 0; index < _pachimonTabs.Length; index++)
            {
                var preview = index < pachimonPreviews.Count
                    ? pachimonPreviews[index]
                    : PachimonPreviewContent.Hidden;
                _pachimonTabs[index]?.Bind(preview);
                SetPachimonTabLabel(index, preview, index < pachimonPreviews.Count);
            }

            ShowTab(0);
            EnsurePageNavigation();
        }

        private void SetPachimonTabLabel(
            int pachimonIndex,
            PachimonPreviewContent preview,
            bool hasPachimon)
        {
            var buttonIndex = pachimonIndex + 1;
            if (buttonIndex >= _tabButtons.Length || _tabButtons[buttonIndex] == null)
            {
                return;
            }

            var label = _tabButtons[buttonIndex].GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;

            label.text = !hasPachimon
                ? "---"
                : preview.IsRevealed
                    ? preview.DisplayName
                    : "?";
        }

        private void ApplyFlexibleTabWidths()
        {
            HorizontalLayoutGroup tabLayout = null;
            foreach (var button in _tabButtons)
            {
                if (button == null)
                {
                    continue;
                }

                var layout = button.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = button.gameObject.AddComponent<LayoutElement>();
                }

                layout.minWidth = 0f;
                layout.preferredWidth = 0f;
                layout.flexibleWidth = 1f;
                tabLayout ??= button.transform.parent
                    ?.GetComponent<HorizontalLayoutGroup>();
            }

            if (tabLayout != null)
            {
                tabLayout.childControlWidth = true;
                tabLayout.childForceExpandWidth = true;
            }
        }

        private void ApplyTabVisualOrder()
        {
            for (var index = 0; index < _tabButtons.Length; index++)
            {
                var button = _tabButtons[index];
                if (button == null)
                {
                    continue;
                }

                button.transform.SetSiblingIndex(
                    _reverseVisualOrder
                        ? _tabButtons.Length - 1 - index
                        : index);
            }
        }

        public void ShowTab(int selectedIndex)
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, _tabPanels.Length - 1));
            SelectedTabIndex = selectedIndex;
            for (var index = 0; index < _tabPanels.Length; index++)
            {
                _tabPanels[index]?.SetActive(index == selectedIndex);
            }

            for (var index = 0; index < _tabButtons.Length; index++)
            {
                if (_tabButtons[index] != null) _tabButtons[index].interactable = index != selectedIndex;
            }

            RefreshPageNavigation();
        }

        private void EnsurePageNavigation()
        {
            _pageNavigations.Clear();
            _scrollIndicators.Clear();
            for (var index = 0; index < _tabPanels.Length; index++)
            {
                var graphicRect = index == 0
                    ? _trainerTab?.GraphicRect
                    : index - 1 < _pachimonTabs.Length
                        ? _pachimonTabs[index - 1]?.GraphicRect
                        : null;
                var panelRect = _tabPanels[index]?.transform as RectTransform;
                _pageNavigations.Add(
                    PaneTabNavigationView.GetOrCreate(
                        graphicRect,
                        panelRect,
                        gameObject.layer));
                _scrollIndicators.Add(index > 0
                    ? ScrollEdgeIndicator.GetOrCreate(
                        panelRect?.GetComponent<ScrollRect>())
                    : null);
            }

            RefreshPageNavigation();
        }

        private void RefreshPageNavigation()
        {
            for (var index = 0; index < _pageNavigations.Count; index++)
            {
                var capturedIndex = index;
                var tabCount = _tabPanels.Length;
                UnityAction showPrevious = tabCount > 1
                    ? () => ShowTab((capturedIndex - 1 + tabCount) % tabCount)
                    : null;
                UnityAction showNext = tabCount > 1
                    ? () => ShowTab((capturedIndex + 1) % tabCount)
                    : null;
                _pageNavigations[index]?.Bind(
                    _reverseVisualOrder ? showNext : showPrevious,
                    _reverseVisualOrder ? showPrevious : showNext);
            }
        }

        private void WireTabListeners()
        {
            for (var index = 0; index < _tabButtons.Length; index++)
            {
                var capturedIndex = index;
                UnityAction action = () => ShowTab(capturedIndex);
                _tabActions.Add(action);
                _tabButtons[index]?.onClick.AddListener(action);
            }
        }

        private void RemoveTabListeners()
        {
            for (var index = 0; index < _tabActions.Count && index < _tabButtons.Length; index++)
            {
                _tabButtons[index]?.onClick.RemoveListener(_tabActions[index]);
            }

            _tabActions.Clear();
        }
    }

    internal sealed class PaneTabNavigationView : MonoBehaviour
    {
        private const float ViewportEdgePadding = 4f;

        private RectTransform _root;
        private RectTransform _graphicRect;
        private Button _previousButton;
        private Button _nextButton;
        private bool _hasInitialTopInset;
        private float _initialTopInset;

        public static PaneTabNavigationView GetOrCreate(
            RectTransform graphicRect,
            RectTransform containerRect,
            int layer)
        {
            if (graphicRect == null || containerRect == null)
            {
                return null;
            }

            var navigationContainer = GetNavigationContainer(containerRect);
            var existing = containerRect.GetComponentInChildren<PaneTabNavigationView>(true);
            if (existing != null)
            {
                existing.ConfigureLayout(graphicRect, navigationContainer);
                return existing;
            }

            var rootObject = new GameObject(
                "RuntimeTabNavigation",
                typeof(RectTransform),
                typeof(PaneTabNavigationView));
            rootObject.layer = layer;
            var root = rootObject.GetComponent<RectTransform>();
            root.SetParent(navigationContainer, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.SetAsLastSibling();

            var view = rootObject.GetComponent<PaneTabNavigationView>();
            view._root = root;
            view._graphicRect = graphicRect;
            view._previousButton = CreateButton(root, "Previous", "<", false, layer);
            view._nextButton = CreateButton(root, "Next", ">", true, layer);
            view.SyncButtonPositions();
            return view;
        }

        private static RectTransform GetNavigationContainer(RectTransform panelRect)
        {
            var scrollRect = panelRect.GetComponent<ScrollRect>();
            var viewport = scrollRect?.viewport;
            if (viewport == null)
            {
                return panelRect;
            }

            if (viewport.GetComponent<RectMask2D>() == null
                && viewport.GetComponent<Mask>() == null)
            {
                viewport.gameObject.AddComponent<RectMask2D>();
            }

            return viewport;
        }

        private void LateUpdate()
        {
            SyncButtonPositions();
        }

        public void Bind(UnityAction onPrevious, UnityAction onNext)
        {
            BindButton(_previousButton, onPrevious);
            BindButton(_nextButton, onNext);
        }

        private void ConfigureLayout(
            RectTransform graphicRect,
            RectTransform containerRect)
        {
            _root ??= transform as RectTransform;
            _graphicRect = graphicRect;
            _previousButton ??= transform.Find("Previous")?.GetComponent<Button>();
            _nextButton ??= transform.Find("Next")?.GetComponent<Button>();
            if (_root != null && _root.parent != containerRect)
            {
                _root.SetParent(containerRect, false);
                _root.anchorMin = Vector2.zero;
                _root.anchorMax = Vector2.one;
                _root.offsetMin = Vector2.zero;
                _root.offsetMax = Vector2.zero;
            }

            _root?.SetAsLastSibling();
            SyncButtonPositions();
        }

        private void SyncButtonPositions()
        {
            if (_root == null || _graphicRect == null)
            {
                return;
            }

            var graphicBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                _root,
                _graphicRect);
            var buttonHeight = Mathf.Max(
                GetButtonHeight(_previousButton),
                GetButtonHeight(_nextButton));
            var halfButtonHeight = buttonHeight * 0.5f;
            var visibleRect = _root.rect;
            // Inactive tab panels have not completed layout yet. Capturing their
            // bounds here pins navigation to the viewport top when first opened.
            if (!_hasInitialTopInset
                && _root.gameObject.activeInHierarchy
                && _graphicRect.gameObject.activeInHierarchy
                && visibleRect.height > 0f
                && _graphicRect.rect.height > 0f)
            {
                _initialTopInset = visibleRect.yMax - graphicBounds.center.y;
                _hasInitialTopInset = true;
            }

            var minimumY = visibleRect.yMin + halfButtonHeight + ViewportEdgePadding;
            var maximumY = visibleRect.yMax - halfButtonHeight - ViewportEdgePadding;
            var initialY = _hasInitialTopInset
                ? visibleRect.yMax - _initialTopInset
                : graphicBounds.center.y;
            var targetY = minimumY <= maximumY
                ? Mathf.Clamp(initialY, minimumY, maximumY)
                : visibleRect.center.y;
            SetButtonPosition(_previousButton, 4f, targetY);
            SetButtonPosition(_nextButton, -4f, targetY);
        }

        private static float GetButtonHeight(Button button)
        {
            return button?.transform is RectTransform rect
                ? rect.rect.height
                : 0f;
        }

        private static void SetButtonPosition(Button button, float x, float y)
        {
            if (button?.transform is RectTransform rect)
            {
                var position = new Vector2(x, y);
                if (rect.anchoredPosition != position)
                {
                    rect.anchoredPosition = position;
                }
            }
        }

        private static void BindButton(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.interactable = action != null;
            if (action != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static Button CreateButton(
            RectTransform parent,
            string objectName,
            string label,
            bool alignRight,
            int layer)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = layer;
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var anchorX = alignRight ? 1f : 0f;
            rect.anchorMin = new Vector2(anchorX, 0.5f);
            rect.anchorMax = new Vector2(anchorX, 0.5f);
            rect.pivot = new Vector2(anchorX, 0.5f);
            rect.sizeDelta = new Vector2(42f, 58f);
            rect.anchoredPosition = new Vector2(alignRight ? -4f : 4f, 0f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.10f, 0.11f, 0.78f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = layer;
            var text = labelObject.GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(rect, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.text = label;
            text.fontSize = 30f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }
    }
}
