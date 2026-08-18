using System.Collections;
using Pachimon.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class ItemSlotView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private const float LongPressSeconds = 0.45f;

        private ItemPanelView _owner;
        private Image _icon;
        private TMP_Text _label;
        private ItemInstance _itemInstance;
        private Coroutine _longPressRoutine;
        private RectTransform _dragLine;
        private RectTransform _dragArrowHead;
        private Outline _outline;
        private Canvas _dragCanvas;
        private Vector2 _dragOrigin;
        private bool _longPressTriggered;
        private bool _isDragging;

        public ItemInstance ItemInstance => _itemInstance;

        public void Initialize(
            ItemPanelView owner,
            Image icon,
            TMP_Text label)
        {
            _owner = owner;
            _icon = icon;
            _label = label;
        }

        public void Bind(ItemInstance itemInstance, ItemAsset item)
        {
            _itemInstance = itemInstance;
            if (_icon != null)
            {
                _icon.sprite = item?.Icon;
                _icon.color = item?.Icon != null
                    ? Color.white
                    : GameUiPalette.Transparent;
            }

            if (_label != null)
            {
                _label.text = itemInstance == null
                    ? "---"
                    : ItemDisplayNameFormatter.Format(
                        item,
                        itemInstance.GeneratedData);
                _label.color = itemInstance == null
                    ? GameUiPalette.SecondaryText
                    : GameUiPalette.PrimaryText;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _longPressTriggered = false;
            _isDragging = false;
            StopLongPress();
            if (_itemInstance != null && _owner.LayoutMode == LayoutMode.Compact)
            {
                _longPressRoutine = StartCoroutine(WaitForLongPress());
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopLongPress();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_itemInstance == null || _isDragging || _longPressTriggered)
            {
                return;
            }

            if (_owner.LayoutMode == LayoutMode.Expanded)
            {
                _owner.RequestDetails(_itemInstance);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            StopLongPress();
            if (_itemInstance == null)
            {
                return;
            }

            _isDragging = true;
            SetHighlighted(true);
            CreateDragArrow(eventData);
            ItemDragSession.Begin(_itemInstance);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateDragArrow(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            StopLongPress();
            DestroyDragArrow();
            SetHighlighted(false);
            ItemDragSession.End();
        }

        private IEnumerator WaitForLongPress()
        {
            var elapsed = 0f;
            while (elapsed < LongPressSeconds)
            {
                if (_isDragging)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _longPressRoutine = null;
            _longPressTriggered = true;
            _owner.RequestDetails(_itemInstance);
        }

        private void CreateDragArrow(PointerEventData eventData)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            _dragCanvas = canvas;
            _dragOrigin = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera,
                (transform as RectTransform)?.TransformPoint(
                    (transform as RectTransform)?.rect.center ?? Vector2.zero)
                ?? transform.position);

            var lineObject = new GameObject(
                "ItemDragArrowLine",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            lineObject.layer = gameObject.layer;
            _dragLine = lineObject.GetComponent<RectTransform>();
            _dragLine.SetParent(canvas.transform, false);
            _dragLine.pivot = new Vector2(0f, 0.5f);
            var image = lineObject.GetComponent<Image>();
            image.color = new Color(0.96f, 0.58f, 0.12f, 0.94f);
            image.raycastTarget = false;
            var canvasGroup = lineObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            _dragLine.SetAsLastSibling();

            var headObject = new GameObject(
                "ItemDragArrowHead",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            headObject.layer = gameObject.layer;
            _dragArrowHead = headObject.GetComponent<RectTransform>();
            _dragArrowHead.SetParent(canvas.transform, false);
            _dragArrowHead.sizeDelta = new Vector2(30f, 30f);
            var head = headObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                head.font = TMP_Settings.defaultFontAsset;
            }

            head.text = "▶";
            head.fontSize = 24f;
            head.color = image.color;
            head.alignment = TextAlignmentOptions.Center;
            head.raycastTarget = false;
            _dragArrowHead.SetAsLastSibling();
            UpdateDragArrow(eventData.position);
        }

        private void UpdateDragArrow(Vector2 screenPosition)
        {
            if (_dragLine == null || _dragArrowHead == null || _dragCanvas == null)
            {
                return;
            }

            var canvasRect = _dragCanvas.transform as RectTransform;
            var camera = _dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _dragCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    _dragOrigin,
                    camera,
                    out var start)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    camera,
                    out var end))
            {
                return;
            }

            var delta = end - start;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            _dragLine.anchoredPosition = start;
            _dragLine.sizeDelta = new Vector2(delta.magnitude, 5f);
            _dragLine.localRotation = Quaternion.Euler(0f, 0f, angle);
            _dragArrowHead.anchoredPosition = end;
            _dragArrowHead.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void DestroyDragArrow()
        {
            if (_dragLine != null)
            {
                Destroy(_dragLine.gameObject);
                _dragLine = null;
            }

            if (_dragArrowHead != null)
            {
                Destroy(_dragArrowHead.gameObject);
                _dragArrowHead = null;
            }

            _dragCanvas = null;
        }

        private void SetHighlighted(bool highlighted)
        {
            if (_outline == null)
            {
                _outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
                _outline.effectColor = new Color(0.96f, 0.58f, 0.12f, 1f);
                _outline.effectDistance = new Vector2(4f, -4f);
                _outline.useGraphicAlpha = false;
            }

            _outline.enabled = highlighted;
        }

        private void StopLongPress()
        {
            if (_longPressRoutine == null)
            {
                return;
            }

            StopCoroutine(_longPressRoutine);
            _longPressRoutine = null;
        }
    }
}
