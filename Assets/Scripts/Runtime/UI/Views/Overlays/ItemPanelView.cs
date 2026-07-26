using System;
using System.Collections;
using Pachimon.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class ItemPanelView : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _transitionDuration = 0.25f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private GridLayoutGroup _grid;
        private readonly ItemSlotView[] _slots = new ItemSlotView[ItemInventory.Capacity];
        private ItemInventory _inventory;
        private ItemCatalog _catalog;
        private Coroutine _transitionRoutine;
        private float _slideDistance;
        private bool _initialized;

        public bool IsOpen { get; private set; }
        public LayoutMode LayoutMode { get; private set; } = LayoutMode.Expanded;
        public event Action<ItemInstance> DetailsRequested;
        public event Action Closed;

        public static ItemPanelView CreateRuntime(RectTransform parent)
        {
            var panelObject = new GameObject(
                "ItemPanelView",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(ItemPanelView));
            panelObject.layer = parent.gameObject.layer;
            var rect = panelObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
            return panelObject.GetComponent<ItemPanelView>();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshGridCellSize();
            if (!IsOpen && _rectTransform != null)
            {
                ApplyProgress(0f);
            }
        }

        public void Bind(ItemInventory inventory, ItemCatalog catalog)
        {
            EnsureInitialized();
            _inventory = inventory;
            _catalog = catalog;
            Refresh();
        }

        public void SetSlideDistance(float distance)
        {
            _slideDistance = Mathf.Max(0f, distance);
            if (!IsOpen && _transitionRoutine == null)
            {
                ApplyProgress(0f);
            }
        }

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            LayoutMode = layoutMode;
        }

        public void Open()
        {
            EnsureInitialized();
            if (IsOpen && _transitionRoutine == null)
            {
                return;
            }

            IsOpen = true;
            Refresh();
            gameObject.SetActive(true);
            StartTransition(1f);
        }

        public void ReplayOpenTransition()
        {
            EnsureInitialized();
            IsOpen = true;
            gameObject.SetActive(true);
            ApplyProgress(0f);
            StartTransition(1f);
        }

        public void Close()
        {
            EnsureInitialized();
            if (!IsOpen && _transitionRoutine == null)
            {
                ApplyProgress(0f);
                return;
            }

            var wasOpen = IsOpen;
            IsOpen = false;
            if (wasOpen)
            {
                Closed?.Invoke();
            }

            StartTransition(0f);
        }

        public void Refresh()
        {
            EnsureInitialized();
            for (var slotIndex = 0; slotIndex < ItemInventory.Capacity; slotIndex++)
            {
                var itemInstance = _inventory?.GetAt(slotIndex);
                var item = itemInstance != null ? _catalog?.Get(itemInstance.ItemId) : null;
                _slots[slotIndex]?.Bind(itemInstance, item);
            }
        }

        internal void RequestDetails(ItemInstance itemInstance)
        {
            if (itemInstance != null)
            {
                DetailsRequested?.Invoke(itemInstance);
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            var background = GetComponent<Image>();
            background.color = new Color32(239, 244, 241, 250);
            background.raycastTarget = true;

            var gridObject = new GameObject(
                "ItemGrid",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            gridObject.layer = gameObject.layer;
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.SetParent(transform, false);
            Stretch(gridRect, new Vector2(14f, 12f), new Vector2(-14f, -12f));
            _grid = gridObject.GetComponent<GridLayoutGroup>();
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = 3;
            _grid.spacing = new Vector2(10f, 8f);
            _grid.padding = new RectOffset(0, 0, 0, 0);
            _grid.childAlignment = TextAnchor.MiddleCenter;

            for (var index = 0; index < ItemInventory.Capacity; index++)
            {
                _slots[index] = CreateSlot(index);
            }

            _initialized = true;
            RefreshGridCellSize();
            ApplyProgress(0f);
        }

        private ItemSlotView CreateSlot(int slotIndex)
        {
            var slotObject = new GameObject(
                $"Slot_{slotIndex + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(ItemSlotView));
            slotObject.layer = gameObject.layer;
            slotObject.transform.SetParent(_grid.transform, false);
            var background = slotObject.GetComponent<Image>();
            background.color = new Color32(255, 255, 255, 235);

            var iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            iconObject.layer = gameObject.layer;
            iconObject.transform.SetParent(slotObject.transform, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.08f, 0.24f);
            iconRect.anchorMax = new Vector2(0.92f, 0.94f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(slotObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.04f, 0.02f);
            labelRect.anchorMax = new Vector2(0.96f, 0.27f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            var slotView = slotObject.GetComponent<ItemSlotView>();
            slotView.Initialize(this, icon, label);
            return slotView;
        }

        private void RefreshGridCellSize()
        {
            if (_grid == null || _rectTransform == null)
            {
                return;
            }

            var width = Mathf.Max(1f, _rectTransform.rect.width - 28f);
            var height = Mathf.Max(1f, _rectTransform.rect.height - 24f);
            _grid.cellSize = new Vector2(
                Mathf.Max(1f, (width - _grid.spacing.x * 2f) / 3f),
                Mathf.Max(1f, (height - _grid.spacing.y * 2f) / 3f));
        }

        private void StartTransition(float targetProgress)
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
            }

            if (!isActiveAndEnabled || _transitionDuration <= 0f)
            {
                ApplyProgress(targetProgress);
                _transitionRoutine = null;
                return;
            }

            _transitionRoutine = StartCoroutine(AnimateTransition(targetProgress));
        }

        private IEnumerator AnimateTransition(float targetProgress)
        {
            var distance = GetSlideDistance();
            var startProgress = distance <= 0f
                ? targetProgress
                : 1f - Mathf.Clamp01(_rectTransform.anchoredPosition.y / distance);
            var elapsed = 0f;
            while (elapsed < _transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / _transitionDuration);
                var eased = progress * progress * (3f - 2f * progress);
                ApplyProgress(Mathf.Lerp(startProgress, targetProgress, eased));
                yield return null;
            }

            ApplyProgress(targetProgress);
            _transitionRoutine = null;
        }

        private void ApplyProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = new Vector2(
                    0f,
                    Mathf.Lerp(GetSlideDistance(), 0f, progress));
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = progress;
                var interactive = IsOpen && progress >= 0.999f;
                _canvasGroup.interactable = interactive;
                _canvasGroup.blocksRaycasts = interactive;
            }
        }

        private float GetSlideDistance()
        {
            return _slideDistance > 0f
                ? _slideDistance
                : Mathf.Max(1f, _rectTransform?.rect.height ?? 1f);
        }

        private static void Stretch(
            RectTransform rect,
            Vector2? offsetMin = null,
            Vector2? offsetMax = null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;
        }
    }
}
