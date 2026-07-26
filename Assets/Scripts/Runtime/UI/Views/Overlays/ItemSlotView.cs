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
        private ItemAsset _item;
        private Coroutine _longPressRoutine;
        private RectTransform _dragGhost;
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
            _item = item;
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
                    : item?.DisplayName ?? $"Item #{itemInstance.ItemId}";
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
            CreateDragGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragGhost != null)
            {
                _dragGhost.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            StopLongPress();
            if (_dragGhost != null)
            {
                Destroy(_dragGhost.gameObject);
                _dragGhost = null;
            }
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

        private void CreateDragGhost(PointerEventData eventData)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var ghostObject = new GameObject(
                "ItemDragGhost",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            ghostObject.layer = gameObject.layer;
            var ghostRect = ghostObject.GetComponent<RectTransform>();
            ghostRect.SetParent(canvas.transform, false);
            var sourceRect = transform as RectTransform;
            ghostRect.sizeDelta = sourceRect != null
                ? sourceRect.rect.size
                : new Vector2(96f, 96f);
            ghostRect.position = eventData.position;
            var image = ghostObject.GetComponent<Image>();
            image.sprite = _item?.Icon;
            image.preserveAspect = true;
            image.color = _item?.Icon != null
                ? new Color(1f, 1f, 1f, 0.82f)
                : new Color(0.92f, 0.95f, 0.93f, 0.82f);
            image.raycastTarget = false;
            var canvasGroup = ghostObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            ghostRect.SetAsLastSibling();
            _dragGhost = ghostRect;
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
