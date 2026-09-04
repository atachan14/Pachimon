using UnityEngine;

namespace Pachimon.UI
{
    [CreateAssetMenu(
        fileName = "ResponsiveUiMetrics",
        menuName = "Pachimon/UI/Responsive UI Metrics")]
    public sealed class ResponsiveUiMetrics : ScriptableObject
    {
        [Header("Compact Root")]
        [SerializeField, Range(0.1f, 1f)]
        private float _compactMaxWidthToHeight = 2f / 3f;

        [Header("Compact Scaling")]
        [SerializeField, Min(1f)] private float _narrowPaneWidth = 700f;
        [SerializeField, Min(1f)] private float _widePaneWidth = 1080f;
        [SerializeField, Min(1f)] private float _minimumTypographyScale = 1.5f;
        [SerializeField, Min(1f)] private float _maximumTypographyScale = 2.25f;
        [SerializeField, Min(1f)] private float _pachimonContentScale = 1.2f;

        [Header("Pachimon And Trainer Graphics")]
        [SerializeField, Min(1f)] private float _expandedGraphicSize = 280f;
        [SerializeField, Min(1f)] private float _expandedGraphicAreaHeight = 300f;
        [SerializeField, Range(0.1f, 1f)] private float _compactGraphicWidthRatio = 0.52f;
        [SerializeField, Range(0.1f, 1f)] private float _compactGraphicHeightRatio = 0.38f;
        [SerializeField, Min(1f)] private float _compactGraphicMinimumSize = 280f;
        [SerializeField, Min(1f)] private float _compactGraphicMaximumSize = 560f;
        [SerializeField, Min(0f)] private float _graphicAreaExtraHeight = 20f;

        [Header("Pachimon Content")]
        [SerializeField, Min(1f)] private float _resourceGaugeHeight = 34f;
        [SerializeField, Min(1f)] private float _sectionTitleHeight = 28f;
        [SerializeField, Min(0f)] private float _compactNameHpSpacing = 10f;

        public float CompactMaxWidthToHeight => _compactMaxWidthToHeight;

        public ResponsiveUiLayout Resolve(
            LayoutMode layoutMode,
            float paneWidth,
            float paneHeight)
        {
            if (layoutMode == LayoutMode.Expanded)
            {
                return new ResponsiveUiLayout(
                    layoutMode,
                    1f,
                    1f,
                    _expandedGraphicSize,
                    _expandedGraphicAreaHeight,
                    _resourceGaugeHeight,
                    _sectionTitleHeight,
                    0f);
            }

            var widthRange = Mathf.Max(1f, _widePaneWidth - _narrowPaneWidth);
            var widthProgress = Mathf.Clamp01(
                (paneWidth - _narrowPaneWidth) / widthRange);
            var typographyScale = Mathf.Lerp(
                _minimumTypographyScale,
                _maximumTypographyScale,
                widthProgress);
            var graphicSize = Mathf.Clamp(
                Mathf.Min(
                    paneWidth * _compactGraphicWidthRatio,
                    paneHeight * _compactGraphicHeightRatio),
                _compactGraphicMinimumSize,
                _compactGraphicMaximumSize);
            var contentScale = typographyScale * _pachimonContentScale;

            return new ResponsiveUiLayout(
                layoutMode,
                typographyScale,
                contentScale,
                graphicSize,
                graphicSize + _graphicAreaExtraHeight,
                _resourceGaugeHeight * contentScale,
                _sectionTitleHeight * contentScale,
                _compactNameHpSpacing);
        }

        public static ResponsiveUiMetrics CreateRuntimeDefaults()
        {
            var metrics = CreateInstance<ResponsiveUiMetrics>();
            metrics.hideFlags = HideFlags.HideAndDontSave;
            return metrics;
        }
    }

    public readonly struct ResponsiveUiLayout
    {
        public ResponsiveUiLayout(
            LayoutMode layoutMode,
            float typographyScale,
            float contentScale,
            float graphicSize,
            float graphicAreaHeight,
            float resourceGaugeHeight,
            float sectionTitleHeight,
            float nameHpSpacing)
        {
            LayoutMode = layoutMode;
            TypographyScale = typographyScale;
            ContentScale = contentScale;
            GraphicSize = graphicSize;
            GraphicAreaHeight = graphicAreaHeight;
            ResourceGaugeHeight = resourceGaugeHeight;
            SectionTitleHeight = sectionTitleHeight;
            NameHpSpacing = nameHpSpacing;
        }

        public LayoutMode LayoutMode { get; }
        public float TypographyScale { get; }
        public float ContentScale { get; }
        public float GraphicSize { get; }
        public float GraphicAreaHeight { get; }
        public float ResourceGaugeHeight { get; }
        public float SectionTitleHeight { get; }
        public float NameHpSpacing { get; }
    }
}
