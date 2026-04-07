using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class GameRootView : MonoBehaviour
    {
        [field: SerializeField] public HeaderView HeaderView { get; private set; }
        [field: SerializeField] public LeftPaneView LeftPaneView { get; private set; }
        [field: SerializeField] public MainPaneView MainPaneView { get; private set; }
        [field: SerializeField] public RightPaneView RightPaneView { get; private set; }
        [field: SerializeField] public MapOverlayView MapOverlayView { get; private set; }
        [field: SerializeField] public LayoutMode LayoutMode { get; private set; }

        private RectTransform _headerRect;
        private RectTransform _contentRect;
        private RectTransform _leftPaneRect;
        private RectTransform _mainPaneRect;
        private RectTransform _rightPaneRect;
        private float _compactBreakpoint;

        public void Initialize(
            HeaderView headerView,
            LeftPaneView leftPaneView,
            MainPaneView mainPaneView,
            RightPaneView rightPaneView,
            MapOverlayView mapOverlayView,
            RectTransform headerRect,
            RectTransform contentRect,
            RectTransform leftPaneRect,
            RectTransform mainPaneRect,
            RectTransform rightPaneRect,
            float compactBreakpoint)
        {
            HeaderView = headerView;
            LeftPaneView = leftPaneView;
            MainPaneView = mainPaneView;
            RightPaneView = rightPaneView;
            MapOverlayView = mapOverlayView;
            _headerRect = headerRect;
            _contentRect = contentRect;
            _leftPaneRect = leftPaneRect;
            _mainPaneRect = mainPaneRect;
            _rightPaneRect = rightPaneRect;
            _compactBreakpoint = compactBreakpoint;

            LogMissingRuntimeReferences();
            ApplyLayoutMode(GetRecommendedLayoutMode());
        }

        private void Update()
        {
            var recommendedMode = GetRecommendedLayoutMode();
            if (recommendedMode != LayoutMode)
            {
                ApplyLayoutMode(recommendedMode);
            }
        }

        public void ToggleMapOverlay()
        {
            if (MapOverlayView == null)
            {
                Debug.LogWarning($"{nameof(GameRootView)} on '{name}' cannot toggle map because {nameof(MapOverlayView)} is missing.", this);
                return;
            }

            if (MapOverlayView.IsOpen)
            {
                MapOverlayView.Close();
            }
            else
            {
                MapOverlayView.Open();
            }
        }

        public LayoutMode GetRecommendedLayoutMode()
        {
            var width = _contentRect != null && _contentRect.rect.width > 0f
                ? _contentRect.rect.width
                : Screen.width;
            return width < _compactBreakpoint ? LayoutMode.Compact : LayoutMode.Expanded;
        }

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            LayoutMode = layoutMode;

            if (_mainPaneRect == null || _leftPaneRect == null || _rightPaneRect == null)
            {
                Debug.LogWarning($"{nameof(GameRootView)} on '{name}' is missing layout rect references.", this);
                return;
            }

            var isCompact = layoutMode == LayoutMode.Compact;
            _leftPaneRect.gameObject.SetActive(!isCompact);
            _rightPaneRect.gameObject.SetActive(!isCompact);

            _mainPaneRect.anchorMin = new Vector2(0f, 0f);
            _mainPaneRect.anchorMax = new Vector2(1f, 1f);
            _mainPaneRect.offsetMin = isCompact ? new Vector2(12f, 0f) : new Vector2(280f, 0f);
            _mainPaneRect.offsetMax = isCompact ? new Vector2(-12f, 0f) : new Vector2(-280f, 0f);

            if (_headerRect != null)
            {
                _headerRect.sizeDelta = new Vector2(0f, isCompact ? 110f : 96f);
            }

            if (HeaderView == null)
            {
                Debug.LogWarning($"{nameof(GameRootView)} on '{name}' is missing {nameof(HeaderView)}.", this);
                return;
            }

            if (HeaderView.BadgeText != null)
            {
                HeaderView.BadgeText.text = isCompact ? "Badges: 3" : "Badges: 3";
            }
        }

        private void LogMissingRuntimeReferences()
        {
            var missing = new List<string>();

            if (HeaderView == null) missing.Add(nameof(HeaderView));
            if (LeftPaneView == null) missing.Add(nameof(LeftPaneView));
            if (MainPaneView == null) missing.Add(nameof(MainPaneView));
            if (RightPaneView == null) missing.Add(nameof(RightPaneView));
            if (MapOverlayView == null) missing.Add(nameof(MapOverlayView));
            if (_headerRect == null) missing.Add(nameof(_headerRect));
            if (_contentRect == null) missing.Add(nameof(_contentRect));
            if (_leftPaneRect == null) missing.Add(nameof(_leftPaneRect));
            if (_mainPaneRect == null) missing.Add(nameof(_mainPaneRect));
            if (_rightPaneRect == null) missing.Add(nameof(_rightPaneRect));

            if (missing.Count == 0)
            {
                return;
            }

            Debug.LogWarning($"{nameof(GameRootView)} on '{name}' is missing references: {string.Join(", ", missing)}", this);
        }
    }
}
