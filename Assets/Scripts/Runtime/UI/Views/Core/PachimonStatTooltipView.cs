using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    internal sealed class PachimonStatTooltipView : MonoBehaviour
    {
        private const float Width = 360f;
        private static readonly Vector2 CursorOffset = new(16f, -22f);

        private RectTransform _rect;
        private RectTransform _canvasRect;
        private Canvas _canvas;
        private TMP_Text _text;
        private Object _owner;

        public static PachimonStatTooltipView GetOrCreate(Component source)
        {
            var canvas = source?.GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.GetComponentInChildren<PachimonStatTooltipView>(true);
            if (existing != null)
            {
                return existing;
            }

            var popup = new GameObject(
                "PachimonStatTooltip",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(PachimonStatTooltipView));
            popup.layer = canvas.gameObject.layer;
            popup.transform.SetParent(canvas.transform, false);
            return popup.GetComponent<PachimonStatTooltipView>();
        }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            _canvasRect = _canvas?.transform as RectTransform;
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0f, 1f);
            _rect.sizeDelta = new Vector2(Width, 72f);

            var background = GetComponent<Image>();
            background.color = new Color32(255, 252, 237, 252);
            background.raycastTarget = false;
            var outline = GetComponent<Outline>();
            outline.effectColor = new Color32(55, 67, 72, 230);
            outline.effectDistance = new Vector2(2f, -2f);

            var textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(_rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 10f);
            textRect.offsetMax = new Vector2(-14f, -10f);
            _text = textObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                _text.font = TMP_Settings.defaultFontAsset;
            }
            _text.fontSize = 22f;
            _text.alignment = TextAlignmentOptions.MidlineLeft;
            _text.color = GameUiPalette.PrimaryText;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.richText = true;
            _text.raycastTarget = false;

            gameObject.SetActive(false);
        }

        public void Show(Object owner, string description, Vector2 screenPosition)
        {
            if (owner == null || string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            _owner = owner;
            _text.text = description;
            var preferredHeight = _text.GetPreferredValues(
                description,
                Width - 28f,
                0f).y + 20f;
            _rect.sizeDelta = new Vector2(Width, Mathf.Max(58f, preferredHeight));
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Move(owner, screenPosition);
        }

        public void Move(Object owner, Vector2 screenPosition)
        {
            if (!gameObject.activeSelf || owner != _owner || _canvasRect == null)
            {
                return;
            }

            var camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect,
                    screenPosition,
                    camera,
                    out var localPoint))
            {
                return;
            }

            var scale = Mathf.Max(0.01f, _canvas.scaleFactor);
            localPoint += CursorOffset / scale;
            var canvasBounds = _canvasRect.rect;
            var width = _rect.rect.width;
            var height = _rect.rect.height;
            if (localPoint.x + width > canvasBounds.xMax - 8f)
            {
                localPoint.x = canvasBounds.xMax - width - 8f;
            }
            if (localPoint.x < canvasBounds.xMin + 8f)
            {
                localPoint.x = canvasBounds.xMin + 8f;
            }
            if (localPoint.y - height < canvasBounds.yMin + 8f)
            {
                localPoint.y += height + 44f / scale;
            }
            localPoint.y = Mathf.Min(localPoint.y, canvasBounds.yMax - 8f);
            _rect.anchoredPosition = localPoint;
        }

        public void Hide(Object owner)
        {
            if (owner != _owner)
            {
                return;
            }

            _owner = null;
            gameObject.SetActive(false);
        }
    }
}
