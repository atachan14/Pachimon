using System;
using Pachimon.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class ItemDropTargetView : MonoBehaviour, IDropHandler
    {
        private static readonly Color AvailableBorderColor =
            new(0.96f, 0.58f, 0.12f, 1f);

        private Func<ItemInstance, bool> _canUse;
        private Func<ItemInstance, bool> _tryUse;
        private RectTransform _borderRoot;

        public void Configure(
            Func<ItemInstance, bool> canUse,
            Func<ItemInstance, bool> tryUse)
        {
            _canUse = canUse;
            _tryUse = tryUse;
        }

        private void OnEnable()
        {
            ItemDragSession.Started += HandleDragStarted;
            ItemDragSession.Ended += HandleDragEnded;
        }

        private void OnDisable()
        {
            ItemDragSession.Started -= HandleDragStarted;
            ItemDragSession.Ended -= HandleDragEnded;
            if (_borderRoot != null)
            {
                _borderRoot.gameObject.SetActive(false);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            var slot = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<ItemSlotView>()
                : null;
            if (slot?.ItemInstance != null)
            {
                var succeeded = _tryUse?.Invoke(slot.ItemInstance) == true;
                ItemDragSession.ReportUseResult(succeeded);
            }
        }

        private void HandleDragStarted(ItemInstance item)
        {
            SetHighlighted(item != null && _canUse?.Invoke(item) == true);
        }

        private void HandleDragEnded()
        {
            SetHighlighted(false);
        }

        private void SetHighlighted(bool highlighted)
        {
            if (!highlighted && _borderRoot == null)
            {
                return;
            }

            EnsureBorder();
            if (_borderRoot != null)
            {
                _borderRoot.gameObject.SetActive(highlighted);
            }
        }

        private void EnsureBorder()
        {
            if (_borderRoot != null)
            {
                return;
            }

            // Outline duplicates a transparent target's full mesh, so use four
            // dedicated edge images to keep the highlight border-only.
            var legacyOutline = GetComponent<Outline>();
            if (legacyOutline != null)
            {
                legacyOutline.enabled = false;
            }

            var borderObject = new GameObject(
                "ItemDropBorder",
                typeof(RectTransform),
                typeof(CanvasGroup));
            borderObject.layer = gameObject.layer;
            _borderRoot = borderObject.GetComponent<RectTransform>();
            _borderRoot.SetParent(transform, false);
            _borderRoot.anchorMin = Vector2.zero;
            _borderRoot.anchorMax = Vector2.one;
            _borderRoot.offsetMin = Vector2.zero;
            _borderRoot.offsetMax = Vector2.zero;
            var canvasGroup = borderObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            CreateEdge("Top", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -4f), Vector2.zero);
            CreateEdge("Bottom", Vector2.zero, new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 4f));
            CreateEdge("Left", Vector2.zero, new Vector2(0f, 1f),
                Vector2.zero, new Vector2(4f, 0f));
            CreateEdge("Right", new Vector2(1f, 0f), Vector2.one,
                new Vector2(-4f, 0f), Vector2.zero);
            _borderRoot.SetAsLastSibling();
            _borderRoot.gameObject.SetActive(false);
        }

        private void CreateEdge(
            string edgeName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var edgeObject = new GameObject(
                edgeName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            edgeObject.layer = gameObject.layer;
            var rect = edgeObject.GetComponent<RectTransform>();
            rect.SetParent(_borderRoot, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = edgeObject.GetComponent<Image>();
            image.color = AvailableBorderColor;
            image.raycastTarget = false;
        }
    }

    internal static class ItemDragSession
    {
        public static event Action<ItemInstance> Started;
        public static event Action Ended;
        public static event Action<bool> UseResultReported;

        public static void Begin(ItemInstance item) => Started?.Invoke(item);
        public static void End() => Ended?.Invoke();
        public static void ReportUseResult(bool succeeded) =>
            UseResultReported?.Invoke(succeeded);
    }
}
