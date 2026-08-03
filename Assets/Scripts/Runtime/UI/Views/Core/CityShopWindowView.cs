using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Items;
using Pachimon.Map;
using Pachimon.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class CityShopWindowView : MonoBehaviour
    {
        private readonly Dictionary<ItemCategory, bool> _expandedCategories = new();
        private TMP_Text _titleText;
        private RectTransform _contentRoot;
        private bool _initialized;

        public static CityShopWindowView CreateRuntime(Transform parent)
        {
            var rootObject = new GameObject(
                "CityNodeWindow",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(CityShopWindowView));
            rootObject.layer = parent.gameObject.layer;
            var root = rootObject.GetComponent<RectTransform>();
            root.SetParent(parent, false);
            Stretch(root);

            var layout = rootObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var view = rootObject.GetComponent<CityShopWindowView>();
            view.EnsureInitialized();
            return view;
        }

        public void Bind(
            CityNodeContent city,
            ItemCatalog catalog,
            RunState runState,
            bool purchaseEnabled,
            Action<int> onDetails,
            Action<string> onPurchase)
        {
            EnsureInitialized();
            if (_titleText != null)
            {
                _titleText.text = purchaseEnabled
                    ? $"CITY SHOP    所持Gold  {runState?.Gold ?? 0}"
                    : "CITY LINEUP";
            }

            RebuildStock(
                city,
                catalog,
                runState,
                purchaseEnabled,
                onDetails,
                onPurchase);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _titleText = CreateText(
                "Title",
                transform,
                "CITY LINEUP",
                25f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetLayoutHeight(_titleText.gameObject, 50f);

            var scrollObject = new GameObject(
                "StockScroll",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(ScrollRect));
            scrollObject.layer = gameObject.layer;
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.SetParent(transform, false);
            var scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 0f;

            var viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D));
            viewportObject.layer = gameObject.layer;
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(scrollRectTransform, false);
            Stretch(viewport);
            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;

            var contentObject = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.layer = gameObject.layer;
            _contentRoot = contentObject.GetComponent<RectTransform>();
            _contentRoot.SetParent(viewport, false);
            _contentRoot.anchorMin = new Vector2(0f, 1f);
            _contentRoot.anchorMax = Vector2.one;
            _contentRoot.pivot = new Vector2(0.5f, 1f);
            _contentRoot.anchoredPosition = Vector2.zero;
            _contentRoot.sizeDelta = Vector2.zero;
            var contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.content = _contentRoot;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 28f;
            ScrollEdgeIndicator.GetOrCreate(scrollRect);

            _initialized = true;
        }

        private void RebuildStock(
            CityNodeContent city,
            ItemCatalog catalog,
            RunState runState,
            bool purchaseEnabled,
            Action<int> onDetails,
            Action<string> onPurchase)
        {
            foreach (Transform child in _contentRoot)
            {
                Destroy(child.gameObject);
            }

            if (city?.StockEntries == null || catalog == null)
            {
                CreateText(
                    "MissingStock",
                    _contentRoot,
                    "商品データがありません。",
                    19f,
                    FontStyles.Normal,
                    TextAlignmentOptions.Center);
                return;
            }

            var groups = city.StockEntries
                .Select(entry => new StockViewValue(entry, catalog.Get(entry.ItemId)))
                .Where(value => value.Item != null)
                .GroupBy(value => value.Item.Category)
                .OrderBy(group => (int)group.Key);

            foreach (var group in groups)
            {
                var values = group
                    .OrderBy(value => value.Item.DisplayName)
                    .ThenBy(value => value.Entry.Price)
                    .ToArray();
                CreateCategorySection(
                    group.Key,
                    values,
                    runState,
                    purchaseEnabled,
                    onDetails,
                    onPurchase);
            }
        }

        private void CreateCategorySection(
            ItemCategory category,
            IReadOnlyList<StockViewValue> values,
            RunState runState,
            bool purchaseEnabled,
            Action<int> onDetails,
            Action<string> onPurchase)
        {
            var sectionObject = new GameObject(
                $"{category}Section",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            sectionObject.layer = gameObject.layer;
            sectionObject.transform.SetParent(_contentRoot, false);
            var sectionLayout = sectionObject.GetComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 4f;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var header = CreateButton(
                "Header",
                sectionObject.transform,
                GameUiPalette.Card,
                out var headerText);
            SetLayoutHeight(header.gameObject, 48f);

            var rowsObject = new GameObject(
                "Rows",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            rowsObject.layer = gameObject.layer;
            rowsObject.transform.SetParent(sectionObject.transform, false);
            var rowsLayout = rowsObject.GetComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 3f;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;
            rowsObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            foreach (var value in values)
            {
                CreateStockRow(
                    rowsObject.transform,
                    value.Entry,
                    value.Item,
                    runState,
                    purchaseEnabled,
                    onDetails,
                    onPurchase);
            }

            if (!_expandedCategories.TryGetValue(category, out var expanded))
            {
                expanded = true;
                _expandedCategories[category] = true;
            }

            void ApplyExpanded()
            {
                rowsObject.SetActive(expanded);
                headerText.text =
                    $"{(expanded ? "▼" : "▶")}  {GetCategoryLabel(category)}"
                    + $"  ({values.Count})";
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            }

            header.onClick.AddListener(() =>
            {
                expanded = !expanded;
                _expandedCategories[category] = expanded;
                ApplyExpanded();
            });
            ApplyExpanded();
        }

        private void CreateStockRow(
            Transform parent,
            CityStockEntry entry,
            ItemAsset item,
            RunState runState,
            bool purchaseEnabled,
            Action<int> onDetails,
            Action<string> onPurchase)
        {
            var rowObject = new GameObject(
                $"Stock_{entry.StockId}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup));
            rowObject.layer = gameObject.layer;
            rowObject.transform.SetParent(parent, false);
            rowObject.GetComponent<Image>().color = entry.IsPurchased
                ? GameUiPalette.MissingGraphic
                : GameUiPalette.StatCard;
            SetLayoutHeight(rowObject, 50f);

            var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 5, 5);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var detailButton = CreateButton(
                "DetailsButton",
                rowObject.transform,
                GameUiPalette.Transparent,
                out var detailLabel);
            detailLabel.gameObject.SetActive(false);
            var detailLayoutElement = detailButton.gameObject.AddComponent<LayoutElement>();
            detailLayoutElement.flexibleWidth = 1f;
            detailLayoutElement.minWidth = 0f;
            var detailLayout = detailButton.gameObject.AddComponent<HorizontalLayoutGroup>();
            detailLayout.spacing = 8f;
            detailLayout.childControlWidth = true;
            detailLayout.childControlHeight = true;
            detailLayout.childForceExpandWidth = false;
            detailLayout.childForceExpandHeight = true;

            var nameText = CreateText(
                "ItemName",
                detailButton.transform,
                item.DisplayName,
                18f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            var nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;

            var priceText = CreateText(
                "Price",
                detailButton.transform,
                $"{entry.Price} G",
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);
            var priceLayout = priceText.gameObject.AddComponent<LayoutElement>();
            priceLayout.preferredWidth = 92f;
            if (onDetails != null)
            {
                detailButton.onClick.AddListener(() => onDetails(item.ItemId));
            }

            var status = GetStatus(entry, runState, purchaseEnabled);
            var canPurchase = purchaseEnabled
                && !entry.IsPurchased
                && runState != null
                && !runState.ItemInventory.IsFull
                && runState.Gold >= entry.Price;
            if (purchaseEnabled)
            {
                var purchaseButton = CreateButton(
                    "PurchaseButton",
                    rowObject.transform,
                    canPurchase
                        ? GameUiPalette.ButtonAccent
                        : GameUiPalette.MissingGraphic,
                    out var purchaseLabel);
                purchaseLabel.text = status;
                purchaseLabel.fontSize = 15f;
                purchaseLabel.color = canPurchase
                    ? GameUiPalette.OnAccentText
                    : GameUiPalette.SecondaryText;
                var purchaseLayout = purchaseButton.gameObject.AddComponent<LayoutElement>();
                purchaseLayout.preferredWidth = 116f;
                purchaseLayout.flexibleWidth = 0f;
                if (canPurchase && onPurchase != null)
                {
                    purchaseButton.onClick.AddListener(() => onPurchase(entry.StockId));
                }
            }
        }

        private static string GetStatus(
            CityStockEntry entry,
            RunState runState,
            bool purchaseEnabled)
        {
            if (!purchaseEnabled)
            {
                return string.Empty;
            }

            if (entry.IsPurchased)
            {
                return "売り切れ";
            }

            if (runState?.ItemInventory.IsFull ?? true)
            {
                return "満杯";
            }

            if (runState.Gold < entry.Price)
            {
                return "Gold不足";
            }

            return "購入";
        }

        private static string GetCategoryLabel(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Pharmacy => "薬局",
                ItemCategory.Other => "その他",
                ItemCategory.SkillMachine => "技マシーン",
                _ => category.ToString(),
            };
        }

        private static Button CreateButton(
            string objectName,
            Transform parent,
            Color backgroundColor,
            out TMP_Text label)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = backgroundColor;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            label = CreateText(
                "Label",
                buttonObject.transform,
                string.Empty,
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            label.margin = new Vector4(8f, 4f, 8f, 4f);
            return button;
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            Transform parent,
            string value,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = GameUiPalette.PrimaryText;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void SetLayoutHeight(GameObject target, float height)
        {
            var layout = target.GetComponent<LayoutElement>()
                ?? target.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private readonly struct StockViewValue
        {
            public StockViewValue(CityStockEntry entry, ItemAsset item)
            {
                Entry = entry;
                Item = item;
            }

            public CityStockEntry Entry { get; }
            public ItemAsset Item { get; }
        }
    }
}
