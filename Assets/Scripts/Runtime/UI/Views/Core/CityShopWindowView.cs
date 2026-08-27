using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Items;
using Pachimon.Map;
using Pachimon.Data;
using Pachimon.Reward;
using Pachimon.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class CityShopWindowView : MonoBehaviour
    {
        private const float DefaultStockRowHeight = 50f;
        private const float SkillMachineStockRowHeight = 210f;

        private static readonly PachimonStatType[] EngravingDisplayOrder =
        {
            PachimonStatType.MaxHp,
            PachimonStatType.MaxMn,
            PachimonStatType.Fire,
            PachimonStatType.Poison,
            PachimonStatType.Aqua,
            PachimonStatType.Ice,
            PachimonStatType.Leaf,
            PachimonStatType.Wind,
            PachimonStatType.Electric,
            PachimonStatType.Dragon,
        };

        private readonly Dictionary<ItemCategory, bool> _expandedCategories = new();
        private readonly Dictionary<string, bool> _expandedSubcategories = new();
        private readonly Dictionary<string, StockRowBinding> _stockRows = new();
        private TMP_Text _titleText;
        private RectTransform _contentRoot;
        private CityNodeContent _boundCity;
        private ItemCatalog _boundCatalog;
        private bool _boundPurchaseEnabled;
        private bool _initialized;

        private sealed class StockRowBinding
        {
            public StockRowBinding(
                GameObject root,
                Image background,
                Button detailButton,
                TMP_Text nameText,
                TMP_Text priceText,
                Button purchaseButton,
                TMP_Text purchaseLabel)
            {
                Root = root;
                Background = background;
                DetailButton = detailButton;
                NameText = nameText;
                PriceText = priceText;
                PurchaseButton = purchaseButton;
                PurchaseLabel = purchaseLabel;
            }

            public GameObject Root { get; }
            public Image Background { get; }
            public Button DetailButton { get; }
            public TMP_Text NameText { get; }
            public TMP_Text PriceText { get; }
            public Button PurchaseButton { get; }
            public TMP_Text PurchaseLabel { get; }
        }

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
            Action<CityStockEntry> onDetails,
            Action<string> onPurchase)
        {
            EnsureInitialized();
            if (_titleText != null)
            {
                _titleText.text = purchaseEnabled
                    ? $"CITY SHOP    所持Gold  {runState?.Gold ?? 0}"
                    : "CITY LINEUP";
            }

            BindStock(
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

        private void BindStock(
            CityNodeContent city,
            ItemCatalog catalog,
            RunState runState,
            bool purchaseEnabled,
            Action<CityStockEntry> onDetails,
            Action<string> onPurchase)
        {
            var values = GetStockValues(city, catalog);
            if (!CanReuseStockStructure(
                    city,
                    catalog,
                    purchaseEnabled,
                    values))
            {
                RebuildStockStructure(
                    city,
                    catalog,
                    purchaseEnabled,
                    values);
            }

            foreach (var value in values)
            {
                if (_stockRows.TryGetValue(value.Entry.StockId, out var binding))
                {
                    BindStockRow(
                        binding,
                        value.Entry,
                        value.Item,
                        runState,
                        purchaseEnabled,
                        onDetails,
                        onPurchase);
                }
            }
        }

        private static StockViewValue[] GetStockValues(
            CityNodeContent city,
            ItemCatalog catalog)
        {
            if (city?.StockEntries == null || catalog == null)
            {
                return Array.Empty<StockViewValue>();
            }

            return city.StockEntries
                .Select(entry => new StockViewValue(entry, catalog.Get(entry.ItemId)))
                .Where(value => value.Item != null)
                .ToArray();
        }

        private bool CanReuseStockStructure(
            CityNodeContent city,
            ItemCatalog catalog,
            bool purchaseEnabled,
            IReadOnlyList<StockViewValue> values)
        {
            return ReferenceEquals(city, _boundCity)
                && ReferenceEquals(catalog, _boundCatalog)
                && purchaseEnabled == _boundPurchaseEnabled
                && values.Count == _stockRows.Count
                && values.All(value =>
                    _stockRows.ContainsKey(value.Entry.StockId));
        }

        private void RebuildStockStructure(
            CityNodeContent city,
            ItemCatalog catalog,
            bool purchaseEnabled,
            IReadOnlyList<StockViewValue> values)
        {
            foreach (Transform child in _contentRoot)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            _stockRows.Clear();
            _boundCity = city;
            _boundCatalog = catalog;
            _boundPurchaseEnabled = purchaseEnabled;

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

            var categories = values
                .Select(value => value.Item.Category)
                .Concat(catalog.Items.OfType<EngravingItemAsset>().Any()
                    ? new[] { ItemCategory.Engraving }
                    : Array.Empty<ItemCategory>())
                .Distinct()
                .OrderBy(category => (int)category);
            foreach (var category in categories)
            {
                var groupedValues = values
                    .Where(value => value.Item.Category == category)
                    .OrderBy(value => value.Item.DisplayName)
                    .ThenBy(value => value.Entry.Price)
                    .ToArray();
                CreateCategorySection(
                    category,
                    groupedValues,
                    catalog,
                    purchaseEnabled);
            }
        }

        private void CreateCategorySection(
            ItemCategory category,
            IReadOnlyList<StockViewValue> values,
            ItemCatalog catalog,
            bool purchaseEnabled)
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

            if (category == ItemCategory.Engraving)
            {
                var engravingByStat = catalog.Items
                    .OfType<EngravingItemAsset>()
                    .GroupBy(item => item.TargetStat)
                    .ToDictionary(group => group.Key, group => group.First());
                foreach (var statType in EngravingDisplayOrder)
                {
                    if (!engravingByStat.TryGetValue(statType, out var engraving))
                    {
                        continue;
                    }

                    var itemValues = values
                        .Where(value => value.Item.ItemId == engraving.ItemId)
                        .OrderBy(value => value.Entry.Price)
                        .ToArray();
                    CreateItemSubcategory(
                        category,
                        rowsObject.transform,
                        engraving,
                        itemValues,
                        purchaseEnabled);
                }
            }
            else if (category == ItemCategory.Equipment)
            {
                foreach (var value in values.OrderBy(value => value.Entry.Price))
                {
                    AddStockRow(rowsObject.transform, value, purchaseEnabled);
                }
            }
            else
            {
                foreach (var itemGroup in values
                             .GroupBy(value => value.Item.ItemId)
                             .OrderBy(group => group.First().Item.DisplayName))
                {
                    var itemValues = itemGroup
                        .OrderBy(value => value.Entry.Price)
                        .ToArray();
                    if (itemValues.Length > 1)
                    {
                        CreateItemSubcategory(
                            category,
                            rowsObject.transform,
                            itemValues[0].Item,
                            itemValues,
                            purchaseEnabled);
                        continue;
                    }

                    AddStockRow(rowsObject.transform, itemValues[0], purchaseEnabled);
                }
            }

            if (!_expandedCategories.TryGetValue(category, out var expanded))
            {
                expanded = false;
                _expandedCategories[category] = false;
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

        private void CreateItemSubcategory(
            ItemCategory category,
            Transform parent,
            ItemAsset item,
            IReadOnlyList<StockViewValue> values,
            bool purchaseEnabled)
        {
            var stateKey = $"{(int)category}:{item.ItemId}";
            var sectionObject = new GameObject(
                $"Item_{item.ItemId}_Section",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            sectionObject.layer = gameObject.layer;
            sectionObject.transform.SetParent(parent, false);
            var sectionLayout = sectionObject.GetComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 3f;
            sectionLayout.padding = new RectOffset(10, 0, 0, 0);
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var header = CreateButton(
                "SubcategoryHeader",
                sectionObject.transform,
                GameUiPalette.StatCard,
                out var headerText);
            SetLayoutHeight(header.gameObject, 42f);

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
                AddStockRow(rowsObject.transform, value, purchaseEnabled);
            }

            if (!_expandedSubcategories.TryGetValue(stateKey, out var expanded))
            {
                expanded = false;
                _expandedSubcategories[stateKey] = false;
            }

            void ApplyExpanded()
            {
                rowsObject.SetActive(expanded);
                headerText.text = $"{(expanded ? "▼" : "▶")}  "
                    + $"{item.DisplayName}  ({values.Count})";
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            }

            header.onClick.AddListener(() =>
            {
                expanded = !expanded;
                _expandedSubcategories[stateKey] = expanded;
                ApplyExpanded();
            });
            ApplyExpanded();
        }

        private void AddStockRow(
            Transform parent,
            StockViewValue value,
            bool purchaseEnabled)
        {
            var binding = CreateStockRow(
                parent,
                value.Entry,
                purchaseEnabled);
            _stockRows.Add(value.Entry.StockId, binding);
        }

        private StockRowBinding CreateStockRow(
            Transform parent,
            CityStockEntry entry,
            bool purchaseEnabled)
        {
            var rowObject = new GameObject(
                $"Stock_{entry.StockId}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup));
            rowObject.layer = gameObject.layer;
            rowObject.transform.SetParent(parent, false);
            var background = rowObject.GetComponent<Image>();
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
                string.Empty,
                18f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            var nameLayout = nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;

            var priceText = CreateText(
                "Price",
                detailButton.transform,
                string.Empty,
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineRight);
            var priceLayout = priceText.gameObject.AddComponent<LayoutElement>();
            priceLayout.preferredWidth = 92f;
            Button purchaseButton = null;
            TMP_Text purchaseLabel = null;
            if (purchaseEnabled)
            {
                purchaseButton = CreateButton(
                    "PurchaseButton",
                    rowObject.transform,
                    GameUiPalette.MissingGraphic,
                    out purchaseLabel);
                purchaseLabel.fontSize = 15f;
                var purchaseLayout = purchaseButton.gameObject.AddComponent<LayoutElement>();
                purchaseLayout.preferredWidth = 116f;
                purchaseLayout.flexibleWidth = 0f;
            }

            return new StockRowBinding(
                rowObject,
                background,
                detailButton,
                nameText,
                priceText,
                purchaseButton,
                purchaseLabel);
        }

        private static void BindStockRow(
            StockRowBinding binding,
            CityStockEntry entry,
            ItemAsset item,
            RunState runState,
            bool purchaseEnabled,
            Action<CityStockEntry> onDetails,
            Action<string> onPurchase)
        {
            binding.Root.name = $"Stock_{entry.StockId}";
            SetLayoutHeight(
                binding.Root,
                item is SkillMachineItemAsset
                    ? SkillMachineStockRowHeight
                    : DefaultStockRowHeight);
            binding.Background.color = entry.IsPurchased
                ? GameUiPalette.MissingGraphic
                : GameUiPalette.StatCard;
            binding.NameText.richText = true;
            binding.NameText.fontSize = item is SkillMachineItemAsset ? 15f : 18f;
            binding.NameText.fontStyle = FontStyles.Normal;
            binding.NameText.alignment = item is SkillMachineItemAsset
                ? TextAlignmentOptions.TopLeft
                : TextAlignmentOptions.MidlineLeft;
            binding.NameText.text = FormatStockDisplayName(
                item,
                entry.GeneratedData);
            binding.PriceText.text = $"{entry.Price} G";
            binding.DetailButton.onClick.RemoveAllListeners();
            if (onDetails != null)
            {
                binding.DetailButton.onClick.AddListener(
                    () => onDetails(entry));
            }

            if (!purchaseEnabled || binding.PurchaseButton == null)
            {
                return;
            }

            var canPurchase = !entry.IsPurchased
                && runState != null
                && item is not EquipmentItemAsset
                && !runState.ItemInventory.IsFull
                && runState.Gold >= entry.Price;
            binding.PurchaseButton.interactable = canPurchase;
            binding.PurchaseButton.targetGraphic.color = canPurchase
                ? GameUiPalette.ButtonAccent
                : GameUiPalette.MissingGraphic;
            binding.PurchaseLabel.text = GetStatus(
                entry,
                item,
                runState,
                purchaseEnabled);
            binding.PurchaseLabel.color = canPurchase
                ? GameUiPalette.OnAccentText
                : GameUiPalette.SecondaryText;
            binding.PurchaseButton.onClick.RemoveAllListeners();
            if (canPurchase && onPurchase != null)
            {
                binding.PurchaseButton.onClick.AddListener(
                    () => onPurchase(entry.StockId));
            }
        }

        internal static string FormatStockDisplayName(
            ItemAsset item,
            GeneratedItemData generatedData)
        {
            if (item is SkillMachineItemAsset machine && machine.Skill != null)
            {
                return FormatSkillMachine(machine);
            }

            if (item is EquipmentItemAsset
                && generatedData?.EquipmentSlot.HasValue == true
                && generatedData.StatChanges.Count >= 2)
            {
                return $"{item.DisplayName}（"
                    + $"{string.Join("/", generatedData.StatChanges.Select(change => FormatStatChange(change, true)))}）";
            }

            if (item is not EngravingItemAsset
                || generatedData?.StatChanges.Count != 2)
            {
                return ItemDisplayNameFormatter.Format(item, generatedData);
            }

            var main = generatedData.StatChanges.FirstOrDefault(
                change => change.Amount > 0);
            var downside = generatedData.StatChanges.FirstOrDefault(
                change => change.Amount < 0);
            if (main == null || downside == null)
            {
                return ItemDisplayNameFormatter.Format(item, generatedData);
            }

            return $"{item.DisplayName}（{FormatStatChange(main)}"
                + $"/{FormatStatChange(downside)}）";
        }

        private static string FormatSkillMachine(SkillMachineItemAsset machine)
        {
            var skill = machine.Skill;
            return $"{machine.DisplayName}\n"
                + $"{SkillDisplayTextFormatter.FormatTiming(skill)}\n"
                + SkillDisplayTextFormatter.FormatBaseDescription(skill);
        }

        internal static string FormatStatChange(
            GeneratedStatChange change,
            bool subStatIsDerivationRatio = false)
        {
            var amount = change.Amount > 0
                ? $"+{change.Amount}"
                : change.Amount.ToString();
            if (PachimonStatTypeUtility.TryGetAttribute(
                    change.StatType,
                    out var attribute))
            {
                var allocationType = (AllocationType)((int)attribute + 1);
                return $"{AttributeRichText.GetIcon(allocationType)}{amount}";
            }

            var background = change.StatType switch
            {
                PachimonStatType.MaxHp or PachimonStatType.MaxMn =>
                    RewardElementPalette.ResourceColor,
                _ when PachimonStatTypeUtility.IsSubStat(change.StatType) =>
                    RewardElementPalette.TimingColor,
                _ => GameUiPalette.StatCard,
            };
            var foreground = AttributeCardPalette.GetReadableTextColor(background);
            var backgroundHex = ColorUtility.ToHtmlStringRGBA(background);
            var foregroundHex = ColorUtility.ToHtmlStringRGBA(foreground);
            var suffix = subStatIsDerivationRatio
                && PachimonSubStatBindings.IsSubStat(change.StatType)
                    ? $"対応率{amount}%"
                    : amount;
            return $"<mark=#{backgroundHex}>"
                + $"<color=#{foregroundHex}>{GetStatLabel(change.StatType)}</color>"
                + $"</mark>{suffix}";
        }

        internal static Color GetStockAccentColor(
            ItemAsset item,
            GeneratedItemData generatedData)
        {
            if (item is SkillMachineItemAsset machine && machine.Skill != null)
            {
                var colors = AttributeCardPalette.GetSkillColors(machine.Skill);
                return colors.Count > 0 ? colors[0] : GameUiPalette.SkillChip;
            }

            if (item is SkillForgetItemAsset)
            {
                return GameUiPalette.SkillChip;
            }

            if (item is EquipmentItemAsset equipment)
            {
                return RewardElementPalette.GetAttributeColor(
                    equipment.MainAttribute);
            }

            if (item is EngravingItemAsset)
            {
                var main = generatedData?.StatChanges.FirstOrDefault(
                    change => change.Amount > 0);
                if (main != null)
                {
                    return GetStatBackgroundColor(main.StatType);
                }
            }

            return GameUiPalette.ItemChip;
        }

        internal static Color GetStockTextColor(
            ItemAsset item,
            GeneratedItemData generatedData)
        {
            return AttributeCardPalette.GetReadableTextColor(
                GetStockAccentColor(item, generatedData));
        }

        private static Color GetStatBackgroundColor(PachimonStatType statType)
        {
            if (PachimonStatTypeUtility.TryGetAttribute(
                    statType,
                    out var attribute))
            {
                return RewardElementPalette.GetAttributeColor(attribute);
            }

            return statType switch
            {
                PachimonStatType.MaxHp or PachimonStatType.MaxMn =>
                    RewardElementPalette.ResourceColor,
                _ when PachimonStatTypeUtility.IsSubStat(statType) =>
                    RewardElementPalette.TimingColor,
                _ => GameUiPalette.StatCard,
            };
        }

        private static string GetStatLabel(PachimonStatType statType)
        {
            return statType switch
            {
                PachimonStatType.MaxHp => "HP",
                PachimonStatType.MaxMn => "MN",
                PachimonStatType.Speed => "SPD",
                PachimonStatType.Haste => "HST",
                PachimonStatType.DamageBonus => "DB",
                PachimonStatType.ResistBonus => "RB",
                _ => EngravingStatName.Get(statType),
            };
        }

        private static string GetStatus(
            CityStockEntry entry,
            ItemAsset item,
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

            if (item is EquipmentItemAsset
                || item is SkillMachineItemAsset
                || item is SkillForgetItemAsset
                || item is EngravingItemAsset)
            {
                return "MainPaneで利用";
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
                ItemCategory.Engraving => "刻印屋",
                ItemCategory.Equipment => "装備品",
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
