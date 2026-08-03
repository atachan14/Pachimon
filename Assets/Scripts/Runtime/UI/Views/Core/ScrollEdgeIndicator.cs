using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class ScrollEdgeIndicator : MonoBehaviour
    {
        private const float IndicatorHeight = 68f;
        private const float EdgeEpsilon = 0.005f;
        private const float MaximumFadeAlpha = 0.80f;

        private ScrollRect _scrollRect;
        private RectTransform _topIndicator;
        private RectTransform _bottomIndicator;

        public static ScrollEdgeIndicator GetOrCreate(ScrollRect scrollRect)
        {
            if (scrollRect == null)
            {
                return null;
            }

            var existing = scrollRect.GetComponentInChildren<ScrollEdgeIndicator>(true);
            if (existing != null)
            {
                existing.Initialize(scrollRect);
                return existing;
            }

            var indicatorObject = new GameObject(
                "ScrollEdgeIndicators",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(ScrollEdgeIndicator));
            indicatorObject.layer = scrollRect.gameObject.layer;

            var indicatorRect = indicatorObject.GetComponent<RectTransform>();
            indicatorRect.SetParent(scrollRect.transform, false);
            Stretch(indicatorRect);
            indicatorRect.SetAsLastSibling();

            var layoutElement = indicatorObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            var indicator = indicatorObject.GetComponent<ScrollEdgeIndicator>();
            indicator.Initialize(scrollRect);
            return indicator;
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            _scrollRect?.onValueChanged.RemoveListener(HandleScrollValueChanged);
        }

        private void Initialize(ScrollRect scrollRect)
        {
            if (_scrollRect != scrollRect)
            {
                _scrollRect?.onValueChanged.RemoveListener(HandleScrollValueChanged);
                _scrollRect = scrollRect;
                _scrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
            }

            _topIndicator ??= CreateIndicator("Top", true);
            _bottomIndicator ??= CreateIndicator("Bottom", false);
            (transform as RectTransform)?.SetAsLastSibling();
            Refresh();
        }

        private RectTransform CreateIndicator(string objectName, bool isTop)
        {
            var indicatorObject = new GameObject(objectName, typeof(RectTransform));
            indicatorObject.layer = gameObject.layer;
            var indicator = indicatorObject.GetComponent<RectTransform>();
            indicator.SetParent(transform, false);
            indicator.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
            indicator.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
            indicator.pivot = new Vector2(0.5f, isTop ? 1f : 0f);
            indicator.anchoredPosition = Vector2.zero;
            indicator.sizeDelta = new Vector2(0f, IndicatorHeight);

            CreateGradient(indicator, isTop);
            CreateArrow(indicator, isTop);
            return indicator;
        }

        private void CreateGradient(RectTransform parent, bool isTop)
        {
            var gradientObject = new GameObject(
                "Fade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(ScrollFadeGraphic));
            gradientObject.layer = gameObject.layer;
            var gradientRect = gradientObject.GetComponent<RectTransform>();
            gradientRect.SetParent(parent, false);
            Stretch(gradientRect);

            var gradient = gradientObject.GetComponent<ScrollFadeGraphic>();
            gradient.Configure(isTop, new Color(
                GameUiPalette.PrimaryText.r,
                GameUiPalette.PrimaryText.g,
                GameUiPalette.PrimaryText.b,
                MaximumFadeAlpha));
        }

        private void CreateArrow(RectTransform parent, bool isTop)
        {
            var arrowObject = new GameObject(
                "Arrow",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            arrowObject.layer = gameObject.layer;
            arrowObject.transform.SetParent(parent, false);

            var arrow = arrowObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                arrow.font = TMP_Settings.defaultFontAsset;
            }

            arrow.text = isTop ? "▲" : "▼";
            arrow.fontSize = 24f;
            arrow.fontStyle = FontStyles.Bold;
            arrow.alignment = TextAlignmentOptions.Center;
            arrow.color = new Color(
                GameUiPalette.PrimaryText.r,
                GameUiPalette.PrimaryText.g,
                GameUiPalette.PrimaryText.b,
                0.78f);
            arrow.raycastTarget = false;

            var arrowRect = arrow.rectTransform;
            arrowRect.anchorMin = Vector2.zero;
            arrowRect.anchorMax = Vector2.one;
            arrowRect.offsetMin = Vector2.zero;
            arrowRect.offsetMax = Vector2.zero;
        }

        private void HandleScrollValueChanged(Vector2 _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_scrollRect == null
                || _topIndicator == null
                || _bottomIndicator == null
                || _scrollRect.content == null
                || _scrollRect.viewport == null)
            {
                SetIndicatorActive(_topIndicator, false);
                SetIndicatorActive(_bottomIndicator, false);
                return;
            }

            var contentBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                _scrollRect.viewport,
                _scrollRect.content);
            var canScroll = contentBounds.size.y
                > _scrollRect.viewport.rect.height + 0.5f;
            var position = _scrollRect.verticalNormalizedPosition;
            SetIndicatorActive(
                _topIndicator,
                canScroll && position < 1f - EdgeEpsilon);
            SetIndicatorActive(
                _bottomIndicator,
                canScroll && position > EdgeEpsilon);
        }

        private static void SetIndicatorActive(RectTransform indicator, bool isActive)
        {
            if (indicator != null && indicator.gameObject.activeSelf != isActive)
            {
                indicator.gameObject.SetActive(isActive);
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    internal sealed class ScrollFadeGraphic : MaskableGraphic
    {
        private bool _isTop;

        public void Configure(bool isTop, Color edgeColor)
        {
            _isTop = isTop;
            color = edgeColor;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var edgeColor = color;
            var transparentColor = edgeColor;
            transparentColor.a = 0f;

            var bottomColor = _isTop ? transparentColor : edgeColor;
            var topColor = _isTop ? edgeColor : transparentColor;
            var vertices = new[]
            {
                CreateVertex(new Vector2(rect.xMin, rect.yMin), bottomColor),
                CreateVertex(new Vector2(rect.xMin, rect.yMax), topColor),
                CreateVertex(new Vector2(rect.xMax, rect.yMax), topColor),
                CreateVertex(new Vector2(rect.xMax, rect.yMin), bottomColor),
            };
            vertexHelper.AddUIVertexQuad(vertices);
        }

        private static UIVertex CreateVertex(Vector2 position, Color vertexColor)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            return vertex;
        }
    }
}
