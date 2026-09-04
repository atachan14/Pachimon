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
    public sealed class CityShopConfiguration
    {
        public static CityShopConfiguration City { get; } = new();
        public static CityShopConfiguration LeagueGate { get; } = new(
            "LEAGUE GATE",
            "四天王に挑む！",
            showInstructor: false,
            showEquipment: false);

        public CityShopConfiguration(
            string title = "CITY",
            string proceedLabel = "進む",
            bool showPharmacy = true,
            bool showInstructor = true,
            bool showEngraving = true,
            bool showEquipment = true)
        {
            Title = title;
            ProceedLabel = proceedLabel;
            ShowPharmacy = showPharmacy;
            ShowInstructor = showInstructor;
            ShowEngraving = showEngraving;
            ShowEquipment = showEquipment;
        }

        public string Title { get; }
        public string ProceedLabel { get; }
        public bool ShowPharmacy { get; }
        public bool ShowInstructor { get; }
        public bool ShowEngraving { get; }
        public bool ShowEquipment { get; }
    }

    public sealed class CitySkillOption
    {
        public CitySkillOption(
            int slotId,
            PachimonAbilityPreview ability,
            PachimonPreviewContent owner)
        {
            SlotId = slotId;
            Ability = ability;
            Owner = owner;
        }

        public int SlotId { get; }
        public PachimonAbilityPreview Ability { get; }
        public PachimonPreviewContent Owner { get; }
        public string DisplayName => Ability.DisplayName;
    }

    public sealed class CityPachimonOption
    {
        public CityPachimonOption(
            string instanceId,
            string displayName,
            Sprite frontSprite,
            IEnumerable<CitySkillOption> skills,
            IEnumerable<EquipmentSlot> occupiedSlots,
            int engravingCount)
        {
            InstanceId = instanceId;
            DisplayName = displayName;
            FrontSprite = frontSprite;
            Skills = (skills ?? Array.Empty<CitySkillOption>()).ToArray();
            OccupiedSlots = new HashSet<EquipmentSlot>(
                occupiedSlots ?? Array.Empty<EquipmentSlot>());
            EngravingCount = engravingCount;
        }

        public string InstanceId { get; }
        public string DisplayName { get; }
        public Sprite FrontSprite { get; }
        public IReadOnlyList<CitySkillOption> Skills { get; }
        public int SkillCount => Skills.Count;
        public IReadOnlyCollection<EquipmentSlot> OccupiedSlots { get; }
        public int EngravingCount { get; }
    }

    public sealed partial class CityScreen
    {
        private enum ShopMode { Menu, Pharmacy, Instructor, Engraving, Equipment }

        private RectTransform _cityRoot;
        private CityNodeContent _city;
        private ItemCatalog _catalog;
        private RunState _runState;
        private IReadOnlyList<CityPachimonOption> _party = Array.Empty<CityPachimonOption>();
        private ShopMode _shopMode;
        private string _statusMessage;
        private Action<CityStockEntry> _showDetails;
        private Action<IReadOnlyList<CityStockEntry>> _buyItems;
        private Action<IReadOnlyList<CityStockEntry>, string> _applyEngravings;
        private Action<CityStockEntry, string> _teachSkill;
        private Action<CityStockEntry, string, int> _forgetSkill;
        private Action<CitySkillOption> _showSkillDetails;
        private Action<CityStockEntry, string> _equip;
        private Action _proceed;
        private CityShopConfiguration _configuration = CityShopConfiguration.City;
        private LayoutMode _layoutMode = LayoutMode.Expanded;
        private float _layoutScale = 1f;

        public void ApplyResponsiveLayout(ResponsiveUiLayout layout)
        {
            var nextScale = layout.LayoutMode == LayoutMode.Compact
                ? Mathf.Clamp(layout.TypographyScale, 1f, 1.6f)
                : 1f;
            if (_layoutMode == layout.LayoutMode
                && Mathf.Approximately(_layoutScale, nextScale))
            {
                return;
            }

            _layoutMode = layout.LayoutMode;
            _layoutScale = nextScale;
            if (_cityRoot != null)
            {
                RenderCity();
            }
        }

        public void Bind(
            CityNodeContent city,
            ItemCatalog catalog,
            RunState runState,
            IReadOnlyList<CityPachimonOption> party,
            string statusMessage,
            Action<CityStockEntry> showDetails,
            Action<IReadOnlyList<CityStockEntry>> buyItems,
            Action<IReadOnlyList<CityStockEntry>, string> applyEngravings,
            Action<CityStockEntry, string> teachSkill,
            Action<CityStockEntry, string, int> forgetSkill,
            Action<CitySkillOption> showSkillDetails,
            Action<CityStockEntry, string> equip,
            Action proceed,
            CityShopConfiguration configuration = null)
        {
            var nextConfiguration = configuration ?? CityShopConfiguration.City;
            var stockChanged = _city == null
                || !ReferenceEquals(_city.StockEntries, city?.StockEntries)
                || !ReferenceEquals(_configuration, nextConfiguration);
            _city = city;
            _catalog = catalog;
            _runState = runState;
            _party = party ?? Array.Empty<CityPachimonOption>();
            _statusMessage = statusMessage;
            _showDetails = showDetails;
            _buyItems = buyItems;
            _applyEngravings = applyEngravings;
            _teachSkill = teachSkill;
            _forgetSkill = forgetSkill;
            _showSkillDetails = showSkillDetails;
            _equip = equip;
            _proceed = proceed;
            _configuration = nextConfiguration;
            if (stockChanged)
                _shopMode = ShopMode.Menu;
            EnsureCityRoot();
            RenderCity();
        }

        private void EnsureCityRoot()
        {
            if (_cityRoot != null) return;
            _cityRoot = CreateObject("CityRuntimeRoot", transform);
            Stretch(_cityRoot);
        }

        private void RenderCity()
        {
            foreach (Transform child in _cityRoot) Destroy(child.gameObject);
            if (_shopMode == ShopMode.Menu) RenderMenu();
            else RenderShop();
        }

        private void RenderMenu()
        {
            var title = CreateText(
                "Title",
                _cityRoot,
                _configuration.Title,
                34f,
                FontStyles.Bold);
            Anchor(title.rectTransform, new Vector2(0.08f, 0.83f), new Vector2(0.92f, 0.96f));

            var gridRoot = CreateObject("ShopGrid", _cityRoot);
            Anchor(gridRoot, new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.8f));
            var grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(260f, Scale(92f));
            grid.spacing = new Vector2(24f, Scale(20f));
            grid.childAlignment = TextAnchor.MiddleCenter;
            var responsiveGrid = gridRoot.gameObject.AddComponent<ResponsiveGridLayout>();
            responsiveGrid.Configure(2, 160f, 92f);
            responsiveGrid.SetDisplayScale(_layoutScale);
            if (_configuration.ShowPharmacy)
                AddShopButton(gridRoot, "薬局", ShopMode.Pharmacy, GameUiPalette.ItemChip);
            if (_configuration.ShowInstructor)
                AddShopButton(gridRoot, "技インストラクター", ShopMode.Instructor, GameUiPalette.SkillChip);
            if (_configuration.ShowEngraving)
                AddShopButton(gridRoot, "刻印屋", ShopMode.Engraving, GameUiPalette.PassiveChip);
            if (_configuration.ShowEquipment)
                AddShopButton(gridRoot, "装備屋", ShopMode.Equipment, GameUiPalette.ButtonNeutral);
            CreateButton(
                "Proceed",
                gridRoot,
                _configuration.ProceedLabel,
                () => _proceed?.Invoke(),
                GameUiPalette.ButtonAccent);

            var status = CreateText("Status", _cityRoot, _statusMessage ?? string.Empty, 19f, FontStyles.Normal);
            Anchor(status.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.18f));
        }

        private void AddShopButton(
            Transform parent,
            string label,
            ShopMode mode,
            Color color)
        {
            CreateButton(label, parent, label, () =>
            {
                _shopMode = mode;
                _statusMessage = null;
                RenderCity();
            }, color);
        }

        private void RenderShop()
        {
            var title = CreateText("ShopTitle", _cityRoot, ShopName(_shopMode), 30f, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Left;
            Anchor(title.rectTransform, new Vector2(0.04f, 0.89f), new Vector2(0.7f, 0.98f));
            var gold = CreateText("Gold", _cityRoot, $"所持Gold  {_runState.Gold}", 20f, FontStyles.Bold);
            gold.alignment = TextAlignmentOptions.Right;
            Anchor(gold.rectTransform, new Vector2(0.68f, 0.89f), new Vector2(0.96f, 0.98f));

            var scroll = CreateScroll(_cityRoot, out var content);
            Anchor(scroll.GetComponent<RectTransform>(), new Vector2(0.04f, 0.19f), new Vector2(0.96f, 0.88f));
            var selected = new HashSet<CityStockEntry>();
            var entries = CurrentEntries().ToArray();
            if (_shopMode == ShopMode.Pharmacy) RenderPharmacy(content, entries, selected);
            else if (_shopMode == ShopMode.Engraving) RenderSelectable(content, entries, selected);
            else RenderTargeted(content, entries);

            var back = CreateButton("Back", _cityRoot, "← 戻る", () =>
            {
                _shopMode = ShopMode.Menu;
                _statusMessage = null;
                RenderCity();
            }, GameUiPalette.ButtonNeutral);
            Anchor(back.GetComponent<RectTransform>(), new Vector2(0.2f, 0.04f), new Vector2(0.46f, 0.14f));

            if (_shopMode is ShopMode.Pharmacy or ShopMode.Engraving)
            {
                var purchase = CreateButton("Purchase", _cityRoot, string.Empty, null, GameUiPalette.ButtonAccent);
                Anchor(purchase.GetComponent<RectTransform>(), new Vector2(0.54f, 0.04f), new Vector2(0.8f, 0.14f));
                var label = purchase.GetComponentInChildren<TMP_Text>();
                void RefreshPurchase()
                {
                    var total = selected.Sum(entry => entry.Price);
                    var canUse = selected.Count > 0 && total <= _runState.Gold;
                    label.text = _shopMode == ShopMode.Pharmacy
                        ? $"購入  合計{total} Gold"
                        : $"パチモンを選択  合計{total} Gold";
                    if (selected.Count == 0) { label.text = "商品を選択"; canUse = false; }
                    else if (total > _runState.Gold) { label.text = "残高不足"; canUse = false; }
                    else if (_shopMode == ShopMode.Pharmacy
                             && selected.Count > ItemInventory.Capacity - _runState.ItemInventory.Count)
                    { label.text = "バッグがいっぱいだ！"; canUse = false; }
                    purchase.interactable = canUse;
                    purchase.targetGraphic.color = canUse ? GameUiPalette.ButtonAccent : GameUiPalette.MissingGraphic;
                    label.color = canUse
                        ? GameUiPalette.OnAccentText
                        : GameUiPalette.SecondaryText;
                    purchase.onClick.RemoveAllListeners();
                    if (!canUse) return;
                    purchase.onClick.AddListener(() =>
                    {
                        var chosen = selected.ToArray();
                        if (_shopMode == ShopMode.Pharmacy) _buyItems?.Invoke(chosen);
                        else OpenSelector(
                            "刻印するパチモンを選択",
                            total,
                            option => option.EngravingCount + chosen.Length
                                > PachimonInstance.MaxEngravings
                                ? $"刻印は最大{PachimonInstance.MaxEngravings}個"
                                : null,
                            id => _applyEngravings?.Invoke(chosen, id));
                    });
                }
                foreach (var toggle in content.GetComponentsInChildren<Toggle>(true))
                    toggle.onValueChanged.AddListener(_ => RefreshPurchase());
                RefreshPurchase();
            }

            var status = CreateText("Status", _cityRoot, _statusMessage ?? string.Empty, 16f, FontStyles.Normal);
            Anchor(status.rectTransform, new Vector2(0.04f, 0.145f), new Vector2(0.96f, 0.19f));
        }

        private void RenderPharmacy(
            RectTransform content,
            IEnumerable<CityStockEntry> entries,
            ISet<CityStockEntry> selected)
        {
            var columns = CreateObject("Columns", content);
            var horizontal = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = Scale(14f);
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = false;
            columns.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var recoveryItems = new HashSet<int>
            {
                ItemIds.Potion,
                ItemIds.MnPotion,
                ItemIds.ReviveShard,
                ItemIds.SuperPotion,
                ItemIds.SuperMnPotion,
                ItemIds.SuperRecovery,
                ItemIds.MaxRevive,
            };
            RenderPharmacyColumn(
                columns,
                "RecoveryItems",
                "回復薬",
                entries.Where(entry => recoveryItems.Contains(entry.ItemId)),
                selected);
            RenderPharmacyColumn(
                columns,
                "SpecialRemedies",
                "特効薬",
                Array.Empty<CityStockEntry>(),
                selected);
        }

        private void RenderPharmacyColumn(
            Transform parent,
            string objectName,
            string headingText,
            IEnumerable<CityStockEntry> entries,
            ISet<CityStockEntry> selected)
        {
            var column = CreateVertical(objectName, parent);
            var heading = CreateText(
                "Heading",
                column,
                headingText,
                20f,
                FontStyles.Bold);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = Scale(38f);
            foreach (var entry in entries.OrderBy(entry => entry.Price))
            {
                AddSelectableRow(column, entry, selected);
            }
        }

        private void RenderSelectable(RectTransform content, IEnumerable<CityStockEntry> entries, ISet<CityStockEntry> selected)
        {
            foreach (var entry in entries.OrderBy(entry => entry.Price)) AddSelectableRow(content, entry, selected);
        }

        private void AddSelectableRow(Transform parent, CityStockEntry entry, ISet<CityStockEntry> selected)
        {
            var row = CreateStockRow(parent, entry);
            var toggleRoot = CreateObject("Check", row);
            var toggle = toggleRoot.gameObject.AddComponent<Toggle>();
            var background = toggleRoot.gameObject.AddComponent<Image>();
            background.color = GameUiPalette.StatusChip;
            toggle.targetGraphic = background;
            var outline = toggleRoot.gameObject.AddComponent<Outline>();
            outline.effectColor = GameUiPalette.ButtonNeutral;
            outline.effectDistance = new Vector2(2f, -2f);
            var mark = CreateObject("Mark", toggleRoot);
            Stretch(mark, 7f);
            var markImage = mark.gameObject.AddComponent<Image>();
            markImage.color = GameUiPalette.ButtonAccent;
            toggle.graphic = markImage;
            toggle.interactable = !entry.IsPurchased;
            toggle.onValueChanged.AddListener(value =>
            {
                if (value) selected.Add(entry); else selected.Remove(entry);
            });
            var size = toggleRoot.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 34f;
            size.preferredHeight = Scale(34f);
        }

        private void RenderTargeted(RectTransform content, IEnumerable<CityStockEntry> entries)
        {
            foreach (var entry in entries.OrderBy(entry => entry.Price))
            {
                var row = CreateStockRow(content, entry);
                var action = CreateButton(
                    "SelectTarget",
                    row,
                    entry.IsPurchased ? "売り切れ" : "パチモンを選択",
                    null,
                    entry.IsPurchased ? GameUiPalette.MissingGraphic : GameUiPalette.ButtonAccent);
                action.gameObject.AddComponent<LayoutElement>().preferredWidth =
                    _layoutMode == LayoutMode.Compact ? 150f : 175f;
                action.interactable = !entry.IsPurchased && _runState.Gold >= entry.Price;
                if (!entry.IsPurchased && _runState.Gold < entry.Price)
                {
                    action.GetComponentInChildren<TMP_Text>().text = "残高不足";
                    action.targetGraphic.color = GameUiPalette.MissingGraphic;
                    action.GetComponentInChildren<TMP_Text>().color = GameUiPalette.SecondaryText;
                }
                if (action.interactable) action.onClick.AddListener(() => OpenEntrySelector(entry));
            }
        }

        private void OpenEntrySelector(CityStockEntry entry)
        {
            var item = _catalog.Get(entry.ItemId);
            if (item is SkillForgetItemAsset)
            {
                PachimonSelectionOverlayView.CreateRuntime(_cityRoot).PresentSkillForget(
                    "技を忘れるパチモンを選択",
                    entry.Price,
                    _party,
                    _showSkillDetails,
                    (instanceId, slotId) => _forgetSkill?.Invoke(
                        entry,
                        instanceId,
                        slotId));
                return;
            }

            string Reason(CityPachimonOption option)
            {
                if (item is SkillMachineItemAsset machine)
                {
                    var alreadyKnown = option.Skills.Any(
                        skill => skill.Ability.Id == machine.SkillId);
                    if (!alreadyKnown
                        && option.SkillCount >= PachimonInstance.MaxSkillSlots)
                        return "これ以上おぼえられない";
                }
                if (item is EquipmentItemAsset equipment && option.OccupiedSlots.Contains(equipment.Slot))
                    return "この部位は装備済み";
                return null;
            }
            OpenSelector(
                item is EquipmentItemAsset ? "装備するパチモンを選択" : "教えるパチモンを選択",
                entry.Price,
                Reason,
                id =>
                {
                    if (item is EquipmentItemAsset) _equip?.Invoke(entry, id);
                    else _teachSkill?.Invoke(entry, id);
                });
        }

        private void OpenSelector(
            string title,
            int price,
            Func<CityPachimonOption, string> unavailableReason,
            Action<string> confirm)
        {
            PachimonSelectionOverlayView.CreateRuntime(_cityRoot).Present(
                title,
                $"購入  {price} Gold",
                _party,
                unavailableReason,
                confirm);
        }

        private IEnumerable<CityStockEntry> CurrentEntries()
        {
            var category = _shopMode switch
            {
                ShopMode.Pharmacy => ItemCategory.Pharmacy,
                ShopMode.Instructor => ItemCategory.SkillMachine,
                ShopMode.Engraving => ItemCategory.Engraving,
                ShopMode.Equipment => ItemCategory.Equipment,
                _ => (ItemCategory)(-1),
            };
            return _city.StockEntries.Where(entry => _catalog.Get(entry.ItemId)?.Category == category);
        }

        private RectTransform CreateStockRow(Transform parent, CityStockEntry entry)
        {
            var row = CreateObject("StockRow", parent);
            var background = row.gameObject.AddComponent<Image>();
            var item = _catalog.Get(entry.ItemId);
            var accent = CityShopWindowView.GetStockAccentColor(
                item,
                entry.GeneratedData);
            var textColor = entry.IsPurchased
                ? GameUiPalette.SecondaryText
                : CityShopWindowView.GetStockTextColor(item, entry.GeneratedData);
            background.color = entry.IsPurchased
                ? GameUiPalette.MissingGraphic
                : accent;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, ScaleToInt(5f), ScaleToInt(5f));
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight =
                Scale(item is SkillMachineItemAsset ? 180f : 54f);
            var details = CreateButton(
                "Details",
                row,
                CityShopWindowView.FormatStockDisplayName(
                    item,
                    entry.GeneratedData),
                () => _showDetails?.Invoke(entry),
                Color.clear);
            details.GetComponentInChildren<TMP_Text>().color = textColor;
            if (item is SkillMachineItemAsset)
            {
                var detailText = details.GetComponentInChildren<TMP_Text>();
                detailText.fontSize = 15f;
                detailText.fontStyle = FontStyles.Normal;
                detailText.alignment = TextAlignmentOptions.TopLeft;
            }
            details.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var price = CreateText(
                "Price",
                row,
                entry.IsPurchased ? "売り切れ" : $"{entry.Price} G",
                17f,
                FontStyles.Bold);
            price.gameObject.AddComponent<LayoutElement>().preferredWidth = 100f;
            price.color = textColor;
            return row;
        }

        private static string ShopName(ShopMode mode) => mode switch
        {
            ShopMode.Pharmacy => "薬局",
            ShopMode.Instructor => "技インストラクター",
            ShopMode.Engraving => "刻印屋",
            ShopMode.Equipment => "装備屋",
            _ => "CITY",
        };

        private RectTransform CreateVertical(string name, Transform parent)
        {
            var root = CreateObject(name, parent);
            var layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = Scale(5f);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return root;
        }

        private ScrollRect CreateScroll(Transform parent, out RectTransform content)
        {
            var root = CreateObject("Scroll", parent);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = CreateObject("Viewport", root);
            Stretch(viewport);
            var image = viewport.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = CreateObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = Scale(6f);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private static RectTransform CreateObject(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            return root.GetComponent<RectTransform>();
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Action action,
            Color color)
        {
            var root = CreateObject(name, parent);
            var image = root.gameObject.AddComponent<Image>();
            image.color = color;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (action != null) button.onClick.AddListener(() => action());
            var text = CreateText("Label", root, label, 18f, FontStyles.Bold);
            text.color = AttributeCardPalette.GetReadableTextColor(color);
            Stretch(text.rectTransform, 5f);
            return button;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            float size,
            FontStyles style)
        {
            var root = CreateObject(name, parent);
            var text = root.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = GameUiPalette.PrimaryText;
            return text;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private float Scale(float value) => value * _layoutScale;

        private int ScaleToInt(float value) => Mathf.RoundToInt(Scale(value));
    }
}
