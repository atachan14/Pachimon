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
        private TMP_Text _messageText;
        private readonly ItemSlotView[] _slots = new ItemSlotView[ItemInventory.Capacity];
        private ItemInventory _inventory;
        private ItemCatalog _catalog;
        private VerticalSlideTransition _slideTransition;
        private Coroutine _messageRoutine;
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

        private void OnEnable()
        {
            ItemDragSession.UseResultReported += ShowUseResult;
        }

        private void OnDisable()
        {
            ItemDragSession.UseResultReported -= ShowUseResult;
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshGridCellSize();
            if (!IsOpen && _rectTransform != null)
            {
                _slideTransition?.Snap(0f);
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
            _slideTransition?.SetSlideDistance(distance);
            if (!IsOpen && _slideTransition?.IsRunning != true)
            {
                _slideTransition?.Snap(0f);
            }
        }

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            LayoutMode = layoutMode;
        }

        public void Open()
        {
            EnsureInitialized();
            if (IsOpen && _slideTransition?.IsRunning != true)
            {
                return;
            }

            IsOpen = true;
            Refresh();
            gameObject.SetActive(true);
            _slideTransition.Play(1f, _transitionDuration);
        }

        public void ReplayOpenTransition()
        {
            EnsureInitialized();
            IsOpen = true;
            gameObject.SetActive(true);
            _slideTransition.Snap(0f);
            _slideTransition.Play(1f, _transitionDuration);
        }

        public void Close()
        {
            EnsureInitialized();
            if (!IsOpen && _slideTransition?.IsRunning != true)
            {
                _slideTransition?.Snap(0f);
                return;
            }

            var wasOpen = IsOpen;
            IsOpen = false;
            if (wasOpen)
            {
                Closed?.Invoke();
            }

            _slideTransition.Play(0f, _transitionDuration);
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
            _slideTransition = new VerticalSlideTransition(
                this,
                _rectTransform,
                _canvasGroup,
                () => IsOpen);
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
            Stretch(gridRect, new Vector2(14f, 44f), new Vector2(-14f, -12f));
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

            _messageText = CreateMessageText();

            _initialized = true;
            RefreshGridCellSize();
            _slideTransition.Snap(0f);
        }

        private TMP_Text CreateMessageText()
        {
            var messageObject = new GameObject(
                "ItemUseMessage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            messageObject.layer = gameObject.layer;
            var rect = messageObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 7f);
            rect.sizeDelta = new Vector2(-28f, 30f);
            var text = messageObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.fontSize = 20f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = GameUiPalette.PrimaryText;
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private void ShowUseResult(bool succeeded)
        {
            if (_messageRoutine != null)
            {
                StopCoroutine(_messageRoutine);
            }

            _messageRoutine = StartCoroutine(ShowUseResultRoutine(succeeded));
        }

        private IEnumerator ShowUseResultRoutine(bool succeeded)
        {
            _messageText.text = succeeded
                ? "アイテムを使用した！"
                : "その対象には使用できない！";
            _messageText.color = succeeded
                ? new Color32(36, 116, 62, 255)
                : new Color32(174, 50, 44, 255);
            yield return new WaitForSecondsRealtime(1.5f);
            _messageText.text = string.Empty;
            _messageRoutine = null;
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

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(slotObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 6f);
            labelRect.offsetMax = new Vector2(-8f, -6f);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            label.fontSize = 20f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 20f;
            label.raycastTarget = false;
            var slotView = slotObject.GetComponent<ItemSlotView>();
            slotView.Initialize(this, null, label);
            return slotView;
        }

        private void RefreshGridCellSize()
        {
            if (_grid == null || _rectTransform == null)
            {
                return;
            }

            var width = Mathf.Max(1f, _rectTransform.rect.width - 28f);
            var height = Mathf.Max(1f, _rectTransform.rect.height - 56f);
            _grid.cellSize = new Vector2(
                Mathf.Max(1f, (width - _grid.spacing.x * 2f) / 3f),
                Mathf.Max(1f, (height - _grid.spacing.y * 2f) / 3f));
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
