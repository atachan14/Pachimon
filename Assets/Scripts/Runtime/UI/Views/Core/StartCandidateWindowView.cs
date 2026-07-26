using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class StartCandidateWindowView : MonoBehaviour
    {
        private const float CandidateTabHeight = 52f;
        private const float WindowPadding = 8f;
        private const float TabContentSpacing = 8f;
        private const float GridSpacing = 6f;
        private const int GridColumns = 3;
        private const int GridRows = 3;
        private const float GridInnerPadding = 4f;
        private const float CandidateGridHeight =
            (CandidateTabHeight * GridRows)
            + (GridSpacing * (GridRows - 1))
            + (GridInnerPadding * 2f);

        private static readonly Color[] CandidateTabColors =
        {
            FromRgb(0xF2, 0x8B, 0x82),
            FromRgb(0xF6, 0xAD, 0x55),
            FromRgb(0xF2, 0xD4, 0x5C),
            FromRgb(0xA8, 0xD6, 0x6D),
            FromRgb(0x63, 0xC5, 0x9B),
            FromRgb(0x63, 0xC7, 0xD6),
            FromRgb(0x78, 0xA7, 0xE8),
            FromRgb(0xAF, 0x8D, 0xE0),
            FromRgb(0xE9, 0x92, 0xC4),
        };

        private static readonly Color SelectedCandidateColor =
            FromRgb(0x8F, 0x99, 0x9E);

        private RectTransform _tabArea;
        private RectTransform _tabContent;
        private GridLayoutGroup _tabLayout;
        private PachimonTabView _detailView;
        private readonly List<Button> _tabButtons = new();
        private readonly List<Image> _tabImages = new();
        private readonly List<Outline> _tabOutlines = new();
        private IReadOnlyList<PachimonPreviewContent> _previews =
            Array.Empty<PachimonPreviewContent>();
        private IReadOnlyList<bool> _candidateSelections = Array.Empty<bool>();
        private Action<int> _onTabSelected;
        private int _selectedIndex;
        private PaneTabNavigationView _pageNavigation;

        public void Initialize(PachimonTabView detailTemplate)
        {
            if (_detailView != null || detailTemplate == null)
            {
                return;
            }

            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            _tabArea = CreateTabArea(root);
            _tabContent = CreateTabContent(_tabArea);
            _tabLayout = _tabContent.GetComponent<GridLayoutGroup>();

            _detailView = Instantiate(detailTemplate, root, false);
            _detailView.name = "CandidateDetail";
            _detailView.gameObject.SetActive(true);
            ApplyLayoutMode(LayoutMode.Expanded);
        }

        private void LateUpdate()
        {
            if (_tabArea != null && _tabArea.gameObject.activeSelf)
            {
                RefreshGridCellSize();
            }
        }

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            var isCompact = layoutMode == LayoutMode.Compact;
            _tabArea?.gameObject.SetActive(isCompact);

            var detailRect = _detailView != null
                ? _detailView.transform as RectTransform
                : null;
            if (detailRect == null)
            {
                return;
            }

            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.offsetMin = new Vector2(WindowPadding, WindowPadding);
            detailRect.offsetMax = new Vector2(
                -WindowPadding,
                isCompact
                    ? -(WindowPadding + CandidateGridHeight + TabContentSpacing)
                    : -WindowPadding);

            if (isCompact)
            {
                RefreshGridCellSize();
            }
        }

        public void Bind(
            IReadOnlyList<PachimonPreviewContent> previews,
            IReadOnlyList<bool> candidateSelections,
            int selectedIndex,
            Action<int> onTabSelected)
        {
            _previews = previews ?? Array.Empty<PachimonPreviewContent>();
            _candidateSelections = candidateSelections ?? Array.Empty<bool>();
            _onTabSelected = onTabSelected;
            EnsureTabs();
            ShowTab(Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, _previews.Count - 1)), false);
        }

        private void EnsureTabs()
        {
            var canReuseTabs = _tabButtons.Count == _previews.Count;
            for (var index = 0; canReuseTabs && index < _tabButtons.Count; index++)
            {
                if (_tabButtons[index] == null)
                {
                    canReuseTabs = false;
                    break;
                }

                var label = _tabButtons[index].GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = _previews[index].DisplayName;
                }
            }

            if (canReuseTabs)
            {
                return;
            }

            foreach (var button in _tabButtons)
            {
                if (button != null) Destroy(button.gameObject);
            }

            _tabButtons.Clear();
            _tabImages.Clear();
            _tabOutlines.Clear();
            for (var index = 0; index < _previews.Count; index++)
            {
                var capturedIndex = index;
                var button = CreateTabButton(
                    _tabContent,
                    _previews[index].DisplayName,
                    () => ShowTab(capturedIndex, true));
                _tabButtons.Add(button);
                _tabImages.Add(button.GetComponent<Image>());
                _tabOutlines.Add(button.GetComponent<Outline>());
            }
        }

        private void ShowTab(int index, bool notify)
        {
            if (index < 0 || index >= _previews.Count)
            {
                return;
            }

            _detailView?.Bind(_previews[index]);
            _selectedIndex = index;
            EnsurePageNavigation();
            for (var buttonIndex = 0; buttonIndex < _tabButtons.Count; buttonIndex++)
            {
                var isSelected = buttonIndex == index;
                _tabButtons[buttonIndex].interactable = true;
                var image = buttonIndex < _tabImages.Count
                    ? _tabImages[buttonIndex]
                    : null;
                if (image != null)
                {
                    image.CrossFadeColor(Color.white, 0f, true, true);
                    var isCandidateSelected = buttonIndex < _candidateSelections.Count
                        && _candidateSelections[buttonIndex];
                    image.color = isCandidateSelected
                        ? SelectedCandidateColor
                        : CandidateTabColors[buttonIndex % CandidateTabColors.Length];
                }

                if (buttonIndex < _tabOutlines.Count && _tabOutlines[buttonIndex] != null)
                {
                    _tabOutlines[buttonIndex].enabled = isSelected;
                }
            }

            if (notify) _onTabSelected?.Invoke(index);
        }

        private void EnsurePageNavigation()
        {
            if (_detailView?.GraphicRect == null)
            {
                return;
            }

            _pageNavigation ??= PaneTabNavigationView.GetOrCreate(
                _detailView.GraphicRect,
                _detailView.transform as RectTransform,
                gameObject.layer);
            _pageNavigation?.Bind(
                _previews.Count > 1
                    ? () => ShowTab(
                        (_selectedIndex - 1 + _previews.Count) % _previews.Count,
                        true)
                    : null,
                _previews.Count > 1
                    ? () => ShowTab((_selectedIndex + 1) % _previews.Count, true)
                    : null);
        }

        private static RectTransform CreateTabArea(RectTransform parent)
        {
            var areaObject = new GameObject(
                "CandidateTabGrid",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            areaObject.layer = parent.gameObject.layer;
            var area = areaObject.GetComponent<RectTransform>();
            area.SetParent(parent, false);
            area.anchorMin = new Vector2(0f, 1f);
            area.anchorMax = Vector2.one;
            area.pivot = new Vector2(0.5f, 1f);
            area.offsetMin = new Vector2(
                WindowPadding,
                -(WindowPadding + CandidateGridHeight));
            area.offsetMax = new Vector2(-WindowPadding, -WindowPadding);
            areaObject.GetComponent<Image>().color = GameUiPalette.Card;
            return area;
        }

        private static RectTransform CreateTabContent(RectTransform parent)
        {
            var contentObject = new GameObject(
                "TabContent",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            contentObject.layer = parent.gameObject.layer;
            var content = contentObject.GetComponent<RectTransform>();
            content.SetParent(parent, false);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 0.5f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var layout = contentObject.GetComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = new Vector2(GridSpacing, GridSpacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = GridColumns;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.cellSize = new Vector2(144f, CandidateTabHeight);
            return content;
        }

        private void RefreshGridCellSize()
        {
            if (_tabArea == null || _tabLayout == null)
            {
                return;
            }

            var usableWidth = _tabArea.rect.width
                - (_tabLayout.padding.left + _tabLayout.padding.right)
                - (GridSpacing * (GridColumns - 1));
            _tabLayout.cellSize = new Vector2(
                Mathf.Max(0f, usableWidth / GridColumns),
                CandidateTabHeight);
        }

        private static Button CreateTabButton(
            RectTransform parent,
            string label,
            UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(
                "CandidateTab",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = GameUiPalette.ButtonNeutral;
            var button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = null;
            image.CrossFadeColor(Color.white, 0f, true, true);
            button.onClick.AddListener(onClick);
            var outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = GameUiPalette.PrimaryText;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(buttonObject.transform, false);
            var text = labelObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 17f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = GameUiPalette.PrimaryText;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 4f);
            textRect.offsetMax = new Vector2(-6f, -4f);
            return button;
        }

        private static Color FromRgb(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, byte.MaxValue);
        }
    }
}
