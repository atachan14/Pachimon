using System;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class ResponsiveUiGeometry
    {
        private readonly RectTransform _bodyRect;
        private readonly RectTransform _mainPaneRect;
        private readonly RectTransform _leftPaneRect;
        private readonly RectTransform _rightPaneRect;
        private readonly RectTransform _overlayLayer;
        private readonly RectTransform _leftDrawerViewport;
        private readonly RectTransform _rightDrawerViewport;
        private readonly RectTransform _mapViewport;
        private readonly RectTransform _itemPanelViewport;
        private readonly RectTransform _settingsOverlayViewport;
        private readonly RectTransform _contentDetailViewport;
        private readonly MainPaneView _mainPaneView;
        private readonly ItemPanelView _itemPanelView;
        private readonly SettingsOverlayView _settingsOverlayView;
        private readonly ContentDetailOverlayView _contentDetailOverlayView;
        private readonly Vector3[] _mainCorners = new Vector3[4];
        private readonly Vector3[] _logCorners = new Vector3[4];
        private readonly Vector3[] _workingCorners = new Vector3[4];
        private bool _hasSignature;
        private GeometrySignature _signature;

        public ResponsiveUiGeometry(
            RectTransform bodyRect,
            RectTransform mainPaneRect,
            RectTransform leftPaneRect,
            RectTransform rightPaneRect,
            RectTransform overlayLayer,
            RectTransform leftDrawerViewport,
            RectTransform rightDrawerViewport,
            RectTransform mapViewport,
            RectTransform itemPanelViewport,
            RectTransform settingsOverlayViewport,
            RectTransform contentDetailViewport,
            MainPaneView mainPaneView,
            ItemPanelView itemPanelView,
            SettingsOverlayView settingsOverlayView,
            ContentDetailOverlayView contentDetailOverlayView)
        {
            _bodyRect = bodyRect;
            _mainPaneRect = mainPaneRect;
            _leftPaneRect = leftPaneRect;
            _rightPaneRect = rightPaneRect;
            _overlayLayer = overlayLayer;
            _leftDrawerViewport = leftDrawerViewport;
            _rightDrawerViewport = rightDrawerViewport;
            _mapViewport = mapViewport;
            _itemPanelViewport = itemPanelViewport;
            _settingsOverlayViewport = settingsOverlayViewport;
            _contentDetailViewport = contentDetailViewport;
            _mainPaneView = mainPaneView;
            _itemPanelView = itemPanelView;
            _settingsOverlayView = settingsOverlayView;
            _contentDetailOverlayView = contentDetailOverlayView;
        }

        public void Invalidate()
        {
            _hasSignature = false;
        }

        public void RefreshIfChanged(LayoutMode layoutMode)
        {
            if (_bodyRect == null || _mainPaneRect == null || _overlayLayer == null)
            {
                return;
            }

            var signature = CaptureSignature(layoutMode);
            if (_hasSignature && _signature.ApproximatelyEquals(signature))
            {
                return;
            }

            _signature = signature;
            _hasSignature = true;
            if (layoutMode == LayoutMode.Compact)
            {
                RefreshCompactDrawers();
            }

            RefreshMap(layoutMode);
            RefreshItemPanel();
            RefreshSettings(layoutMode);
            RefreshContentDetail(layoutMode);
        }

        public void RefreshCompactDrawers()
        {
            if (_bodyRect == null
                || _leftDrawerViewport == null
                || _rightDrawerViewport == null)
            {
                return;
            }

            var width = Mathf.Max(1f, _bodyRect.rect.width);
            ConfigureDrawerPane(_leftPaneRect, width, true);
            ConfigureDrawerPane(_rightPaneRect, width, false);
        }

        private GeometrySignature CaptureSignature(LayoutMode layoutMode)
        {
            _mainPaneRect.GetWorldCorners(_mainCorners);
            var logRect = _mainPaneView?.LogWindowView?.transform as RectTransform;
            if (logRect != null)
            {
                logRect.GetWorldCorners(_logCorners);
            }
            else
            {
                Array.Clear(_logCorners, 0, _logCorners.Length);
            }

            return new GeometrySignature(
                layoutMode,
                _bodyRect.rect.size,
                _overlayLayer.rect.size,
                _mainCorners[0],
                _mainCorners[2],
                _logCorners[0],
                _logCorners[2]);
        }

        private void RefreshMap(LayoutMode layoutMode)
        {
            if (_mapViewport == null || _overlayLayer == null)
            {
                return;
            }

            if (layoutMode == LayoutMode.Compact)
            {
                SetStretch(_mapViewport);
                return;
            }

            ApplyWorldBounds(_mapViewport, _mainPaneRect);
        }

        private void RefreshItemPanel()
        {
            if (_itemPanelViewport == null
                || _overlayLayer == null
                || _mainPaneView?.LogWindowView == null)
            {
                return;
            }

            var logRect = _mainPaneView.LogWindowView.transform as RectTransform;
            if (logRect == null)
            {
                return;
            }

            var size = ApplyWorldBounds(_itemPanelViewport, logRect);
            _itemPanelView?.SetSlideDistance(
                Mathf.Max(_bodyRect?.rect.height ?? 0f, size.y));
        }

        private void RefreshSettings(LayoutMode layoutMode)
        {
            if (_settingsOverlayViewport == null
                || _overlayLayer == null
                || _mainPaneRect == null)
            {
                return;
            }

            var sourceRect = layoutMode == LayoutMode.Compact
                ? _overlayLayer
                : _mainPaneRect;
            var size = ApplyWorldBounds(
                _settingsOverlayViewport,
                sourceRect,
                0.82f,
                0.62f);
            _settingsOverlayView?.SetSlideDistance(
                Mathf.Max(_bodyRect?.rect.height ?? 0f, size.y));
        }

        private void RefreshContentDetail(LayoutMode layoutMode)
        {
            if (_contentDetailViewport == null
                || _overlayLayer == null
                || _mainPaneRect == null)
            {
                return;
            }

            if (layoutMode == LayoutMode.Compact)
            {
                SetStretch(_contentDetailViewport);
            }
            else
            {
                ApplyWorldBounds(_contentDetailViewport, _mainPaneRect);
            }

            _contentDetailOverlayView?.SetSlideDistance(
                Mathf.Max(
                    _bodyRect?.rect.height ?? 0f,
                    _contentDetailViewport.rect.height));
        }

        private Vector2 ApplyWorldBounds(
            RectTransform target,
            RectTransform source,
            float widthScale = 1f,
            float heightScale = 1f)
        {
            source.GetWorldCorners(_workingCorners);
            var bottomLeft = _overlayLayer.InverseTransformPoint(_workingCorners[0]);
            var topRight = _overlayLayer.InverseTransformPoint(_workingCorners[2]);
            var center = (bottomLeft + topRight) * 0.5f;
            var size = topRight - bottomLeft;

            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = center;
            target.sizeDelta = new Vector2(
                Mathf.Max(1f, size.x * widthScale),
                Mathf.Max(1f, size.y * heightScale));
            return size;
        }

        private static void ConfigureDrawerPane(
            RectTransform pane,
            float width,
            bool alignLeft)
        {
            if (pane == null)
            {
                return;
            }

            pane.anchorMin = new Vector2(alignLeft ? 0f : 1f, 0f);
            pane.anchorMax = new Vector2(alignLeft ? 0f : 1f, 1f);
            pane.pivot = new Vector2(alignLeft ? 0f : 1f, 0.5f);
            pane.anchoredPosition = Vector2.zero;
            pane.sizeDelta = new Vector2(width, 0f);
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private readonly struct GeometrySignature
        {
            private const float Epsilon = 0.01f;

            public GeometrySignature(
                LayoutMode layoutMode,
                Vector2 bodySize,
                Vector2 overlaySize,
                Vector3 mainBottomLeft,
                Vector3 mainTopRight,
                Vector3 logBottomLeft,
                Vector3 logTopRight)
            {
                LayoutMode = layoutMode;
                BodySize = bodySize;
                OverlaySize = overlaySize;
                MainBottomLeft = mainBottomLeft;
                MainTopRight = mainTopRight;
                LogBottomLeft = logBottomLeft;
                LogTopRight = logTopRight;
            }

            private LayoutMode LayoutMode { get; }
            private Vector2 BodySize { get; }
            private Vector2 OverlaySize { get; }
            private Vector3 MainBottomLeft { get; }
            private Vector3 MainTopRight { get; }
            private Vector3 LogBottomLeft { get; }
            private Vector3 LogTopRight { get; }

            public bool ApproximatelyEquals(GeometrySignature other)
            {
                return LayoutMode == other.LayoutMode
                    && Approximately(BodySize, other.BodySize)
                    && Approximately(OverlaySize, other.OverlaySize)
                    && Approximately(MainBottomLeft, other.MainBottomLeft)
                    && Approximately(MainTopRight, other.MainTopRight)
                    && Approximately(LogBottomLeft, other.LogBottomLeft)
                    && Approximately(LogTopRight, other.LogTopRight);
            }

            private static bool Approximately(Vector2 first, Vector2 second) =>
                (first - second).sqrMagnitude <= Epsilon * Epsilon;

            private static bool Approximately(Vector3 first, Vector3 second) =>
                (first - second).sqrMagnitude <= Epsilon * Epsilon;
        }
    }
}
