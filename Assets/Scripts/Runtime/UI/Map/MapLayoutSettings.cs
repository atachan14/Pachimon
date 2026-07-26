using System;
using UnityEngine;

namespace Pachimon.UI
{
    [Serializable]
    public sealed class MapLayoutSettings
    {
        [SerializeField, Min(1)] private int _maxColumnCount = 6;
        [SerializeField, Min(1)] private int _visibleRowCount = 6;
        [SerializeField, Min(0f)] private float _horizontalPadding = 32f;
        [SerializeField, Min(0f)] private float _verticalPadding = 64f;
        [SerializeField, Range(0f, 0.45f)] private float _horizontalJitterRatio = 0.22f;
        [SerializeField, Range(0f, 0.45f)] private float _verticalJitterRatio = 0.18f;
        [SerializeField, Range(0.2f, 1f)] private float _cityPortSpacingRatio = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _currentNodeViewportRatio = 0.38f;

        public int MaxColumnCount => _maxColumnCount;
        public int VisibleRowCount => _visibleRowCount;
        public float HorizontalPadding => _horizontalPadding;
        public float VerticalPadding => _verticalPadding;
        public float HorizontalJitterRatio => _horizontalJitterRatio;
        public float VerticalJitterRatio => _verticalJitterRatio;
        public float CityPortSpacingRatio => _cityPortSpacingRatio > 0f ? _cityPortSpacingRatio : 0.55f;
        public float CurrentNodeViewportRatio => _currentNodeViewportRatio;
    }
}
