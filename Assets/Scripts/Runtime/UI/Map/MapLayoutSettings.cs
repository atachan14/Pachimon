using System;
using UnityEngine;

namespace Pachimon.UI
{
    [Serializable]
    public sealed class MapLayoutSettings
    {
        [SerializeField, Min(1)] private int _maxColumnCount = 6;
        [SerializeField, Min(1)] private int _visibleRowCount = 6;
        [SerializeField, Min(1f)] private float _compactMinimumNodeSize = 56f;
        [SerializeField, Min(1f)] private float _portraitMaximumNodeSize = 160f;
        [SerializeField, Range(0.01f, 0.25f)] private float _compactNodeWidthRatio = 0.12f;
        [SerializeField, Range(0.3f, 1f)] private float _referencePortraitAspectRatio = 7f / 12f;
        [SerializeField, Range(0f, 1f)] private float _tallPortraitBoostExponent = 0.5f;
        [SerializeField, Min(1f)] private float _expandedNodeSize = 56f;
        [SerializeField, Min(1f)] private float _compactRowSpacing = 226.8f;
        [SerializeField, Min(0f)] private float _partyEncounterGapRows = 1f;
        [SerializeField, Min(0f)] private float _horizontalPadding = 32f;
        [SerializeField, Min(0f)] private float _verticalPadding = 64f;
        [SerializeField, Min(0f)] private float _nodeEdgeGap = 16f;
        [SerializeField, Range(0f, 0.45f)] private float _horizontalJitterRatio = 0.22f;
        [SerializeField, Range(0f, 0.45f)] private float _verticalJitterRatio = 0.18f;
        [SerializeField, Range(0.2f, 1f)] private float _cityPortSpacingRatio = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _currentNodeViewportRatio = 0.38f;

        public int MaxColumnCount => _maxColumnCount;
        public int VisibleRowCount => _visibleRowCount;
        public float GetNodeSize(
            LayoutMode layoutMode,
            float viewportWidth,
            float screenAspectRatio)
        {
            if (layoutMode == LayoutMode.Expanded || screenAspectRatio >= 1f)
            {
                return _expandedNodeSize > 0f ? _expandedNodeSize : 56f;
            }

            var minimumSize = _compactMinimumNodeSize > 0f
                ? _compactMinimumNodeSize
                : 56f;
            var maximumSize = Mathf.Max(
                minimumSize,
                _portraitMaximumNodeSize > 0f ? _portraitMaximumNodeSize : 160f);
            var widthRatio = _compactNodeWidthRatio > 0f
                ? _compactNodeWidthRatio
                : 0.12f;
            var referenceAspect = _referencePortraitAspectRatio > 0f
                ? _referencePortraitAspectRatio
                : 7f / 12f;
            var safeAspect = Mathf.Max(0.01f, screenAspectRatio);
            var tallPortraitBoost = Mathf.Pow(
                Mathf.Max(1f, referenceAspect / safeAspect),
                Mathf.Clamp01(_tallPortraitBoostExponent));
            return Mathf.Clamp(
                viewportWidth * widthRatio * tallPortraitBoost,
                minimumSize,
                maximumSize);
        }

        public float CompactRowSpacing =>
            _compactRowSpacing > 0f ? _compactRowSpacing : 226.8f;
        public float PartyEncounterGapRows => Mathf.Max(0f, _partyEncounterGapRows);
        public float HorizontalPadding => _horizontalPadding;
        public float VerticalPadding => _verticalPadding;
        public float NodeEdgeGap => _nodeEdgeGap >= 0f ? _nodeEdgeGap : 16f;
        public float HorizontalJitterRatio => _horizontalJitterRatio;
        public float VerticalJitterRatio => _verticalJitterRatio;
        public float CityPortSpacingRatio => _cityPortSpacingRatio > 0f ? _cityPortSpacingRatio : 0.55f;
        public float CurrentNodeViewportRatio => _currentNodeViewportRatio;
    }
}
