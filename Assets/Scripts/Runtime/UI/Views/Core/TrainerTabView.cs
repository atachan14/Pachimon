using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public readonly struct TrainerRewardIconContent
    {
        public TrainerRewardIconContent(
            string label,
            string colorHex,
            Sprite sprite = null,
            int amount = 0,
            bool useColoredBackground = false)
        {
            Label = label;
            ColorHex = colorHex;
            Sprite = sprite;
            Amount = amount;
            UseColoredBackground = useColoredBackground;
        }

        public string Label { get; }
        public string ColorHex { get; }
        public Sprite Sprite { get; }
        public int Amount { get; }
        public bool UseColoredBackground { get; }
    }

    public readonly struct TrainerStatPreview
    {
        public TrainerStatPreview(PachimonStatType statType, int value)
        {
            StatType = statType;
            Value = value;
        }

        public PachimonStatType StatType { get; }
        public int Value { get; }
    }

    public readonly struct TrainerBadgePreview
    {
        public TrainerBadgePreview(PachimonAttribute attribute, int count)
        {
            Attribute = attribute;
            Count = count;
        }

        public PachimonAttribute Attribute { get; }
        public int Count { get; }
    }

    public sealed class TrainerPreviewContent
    {
        public TrainerPreviewContent(
            Sprite graphic,
            string displayName,
            int rowIndex,
            IEnumerable<TrainerStatPreview> stats,
            IEnumerable<TrainerBadgePreview> badges,
            IEnumerable<TrainerRewardIconContent> rewardIcons,
            Sprite goldIcon,
            int? gold,
            bool hasReward)
        {
            Graphic = graphic;
            DisplayName = displayName;
            RowIndex = rowIndex;
            Stats = stats?.ToArray() ?? Array.Empty<TrainerStatPreview>();
            Badges = badges?.ToArray() ?? Array.Empty<TrainerBadgePreview>();
            RewardIcons = rewardIcons?.ToArray() ?? Array.Empty<TrainerRewardIconContent>();
            GoldIcon = goldIcon;
            Gold = gold;
            HasReward = hasReward;
        }

        public Sprite Graphic { get; }
        public string DisplayName { get; }
        public int RowIndex { get; }
        public IReadOnlyList<TrainerStatPreview> Stats { get; }
        public IReadOnlyList<TrainerBadgePreview> Badges { get; }
        public IReadOnlyList<TrainerRewardIconContent> RewardIcons { get; }
        public Sprite GoldIcon { get; }
        public int? Gold { get; }
        public bool HasReward { get; }
    }

    public sealed class TrainerTabView : MonoBehaviour
    {
        private static readonly PachimonStatType[] StatDisplayOrder =
        {
            PachimonStatType.MaxHp,
            PachimonStatType.MaxMn,
            PachimonStatType.Fire,
            PachimonStatType.Aqua,
            PachimonStatType.Leaf,
            PachimonStatType.Electric,
            PachimonStatType.Ice,
            PachimonStatType.Wind,
            PachimonStatType.Poison,
            PachimonStatType.Dragon,
        };

        [SerializeField] private Image _graphic;
        [SerializeField] private TMP_Text _displayName;
        [SerializeField] private Transform _rewardIconContainer;
        [SerializeField] private TrainerRewardIconView _rewardIconTemplate;
        [SerializeField] private TMP_Text _emptyRewardText;
        [SerializeField] private TMP_Text _goldText;
        private TMP_Text _rowText;
        private GameObject _statusSection;
        private RectTransform _statusGrid;
        private GameObject _badgeSection;
        private RectTransform _badgeGrid;
        private GameObject _rewardSection;
        private GameObject _goldSection;
        private GameObject _rewardSummarySection;
        private RectTransform _rewardSummaryLines;
        private readonly Dictionary<PachimonStatType, TMP_Text> _statTexts = new();
        private readonly List<TMP_Text> _badgeCards = new();
        private readonly List<RewardElementBinding> _rewardElements = new();
        private GameObject _rewardElementsLine;
        private GameObject _goldSummaryLine;
        private Image _goldSummaryIcon;
        private TMP_Text _goldSummaryValue;
        public RectTransform GraphicRect => _graphic?.rectTransform;

        private readonly struct RewardElementBinding
        {
            public RewardElementBinding(
                GameObject root,
                Image background,
                LayoutElement labelLayout,
                TMP_Text label,
                TMP_Text amount)
            {
                Root = root;
                Background = background;
                LabelLayout = labelLayout;
                Label = label;
                Amount = amount;
            }

            public GameObject Root { get; }
            public Image Background { get; }
            public LayoutElement LabelLayout { get; }
            public TMP_Text Label { get; }
            public TMP_Text Amount { get; }
        }

        public void Configure(
            Image graphic,
            TMP_Text displayName,
            Transform rewardIconContainer,
            TrainerRewardIconView rewardIconTemplate,
            TMP_Text emptyRewardText,
            TMP_Text goldText)
        {
            _graphic = graphic;
            _displayName = displayName;
            _rewardIconContainer = rewardIconContainer;
            _rewardIconTemplate = rewardIconTemplate;
            _emptyRewardText = emptyRewardText;
            _goldText = goldText;
        }

        public void Bind(TrainerPreviewContent content)
        {
            if (content == null) return;

            EnsureRuntimeSections();

            if (_graphic != null)
            {
                _graphic.sprite = content.Graphic;
                _graphic.enabled = content.Graphic != null;
                _graphic.color = Color.white;
                _graphic.preserveAspect = true;
            }

            if (_displayName != null) _displayName.text = content.DisplayName;
            if (_rowText != null) _rowText.text = $"Row: {content.RowIndex}";
            BindStats(content.Stats);
            RebuildBadges(content.Badges);
            RebuildRewardSummary(
                content.RewardIcons,
                content.GoldIcon,
                content.Gold,
                content.HasReward);
        }

        private void EnsureRuntimeSections()
        {
            if (_statusSection != null)
            {
                return;
            }

            var content = transform.Find("Viewport/Content") as RectTransform;
            if (content == null)
            {
                return;
            }

            if (content.TryGetComponent<VerticalLayoutGroup>(out var contentLayout))
            {
                contentLayout.spacing = 8f;
                contentLayout.padding = new RectOffset(
                    contentLayout.padding.left,
                    contentLayout.padding.right,
                    contentLayout.padding.top,
                    8);
            }
            SetPreferredHeight(_displayName?.gameObject, 40f);

            _rewardSection = content.Find("RewardSection")?.gameObject;
            _goldSection = content.Find("GoldSection")?.gameObject;
            _rewardSection?.SetActive(false);
            _goldSection?.SetActive(false);
            _rowText = CreateText("Row", content, 20f, FontStyles.Bold);
            _rowText.alignment = TextAlignmentOptions.Center;
            SetPreferredHeight(_rowText.gameObject, 28f);
            _rowText.transform.SetSiblingIndex(2);

            _statusSection = CreateSection("TrainerStatusSection", content, 150f);
            _statusSection.transform.SetSiblingIndex(3);
            var statusBackground = _statusSection.GetComponent<Image>();
            statusBackground.color = GameUiPalette.Transparent;
            statusBackground.raycastTarget = false;
            _statusGrid = CreateGrid(_statusSection.transform, "TrainerStatusGrid", 4, 38f);
            _statusGrid.offsetMin = Vector2.zero;
            _statusGrid.offsetMax = Vector2.zero;
            _statusGrid.GetComponent<GridLayoutGroup>().spacing = new Vector2(7f, 7f);
            foreach (var statType in StatDisplayOrder)
            {
                var card = CreateStatCard(_statusGrid, statType);
                _statTexts[statType] = card;
                if (statType == PachimonStatType.MaxMn)
                {
                    CreateStatSpacer(_statusGrid, "ResourceSpacer1");
                    CreateStatSpacer(_statusGrid, "ResourceSpacer2");
                }
            }

            _badgeSection = CreateSection("BadgeSection", content, 126f);
            _badgeSection.transform.SetSiblingIndex(4);
            CreateSectionTitle(_badgeSection.transform, "Badge");
            _badgeGrid = CreateGrid(_badgeSection.transform, "BadgeGrid", 4, 38f);
            _badgeSection.SetActive(false);

            _rewardSummarySection = CreateSection(
                "RewardSummarySection",
                content,
                104f);
            _rewardSummarySection.transform.SetSiblingIndex(5);
            _rewardSummarySection.GetComponent<Image>().color = GameUiPalette.Card;
            CreateSectionTitle(_rewardSummarySection.transform, "報酬");
            _rewardSummaryLines = CreateRewardSummaryLines(
                _rewardSummarySection.transform);
            _rewardSummarySection.SetActive(false);
        }

        private void BindStats(IReadOnlyList<TrainerStatPreview> stats)
        {
            var values = stats?.ToDictionary(item => item.StatType, item => item.Value)
                ?? new Dictionary<PachimonStatType, int>();
            foreach (var pair in _statTexts)
            {
                var value = values.TryGetValue(pair.Key, out var current) ? current : 0;
                pair.Value.text = FormatSigned(value);
            }
        }

        private void RebuildBadges(IReadOnlyList<TrainerBadgePreview> badges)
        {
            if (_badgeSection == null || _badgeGrid == null)
            {
                return;
            }

            foreach (var card in _badgeCards)
            {
                card?.transform.parent.gameObject.SetActive(false);
            }

            var hasBadges = badges != null && badges.Count > 0;
            _badgeSection.SetActive(hasBadges);
            if (!hasBadges)
            {
                return;
            }

            for (var index = 0; index < badges.Count; index++)
            {
                var badge = badges[index];
                var color = RewardElementPalette.GetAttributeColor(badge.Attribute);
                var card = GetOrCreateBadgeCard(index);
                card.transform.parent.gameObject.SetActive(true);
                card.transform.parent.GetComponent<Image>().color = color;
                card.color = AttributeCardPalette.GetReadableTextColor(color);
                card.text = $"{GetAttributeLabel(badge.Attribute)}  x{badge.Count}";
            }

            _badgeGrid.GetComponent<ResponsiveGridLayout>()?.RefreshLayout();
        }

        private TMP_Text GetOrCreateBadgeCard(int index)
        {
            while (_badgeCards.Count <= index)
            {
                _badgeCards.Add(CreateCard(
                    _badgeGrid,
                    GameUiPalette.StatCard,
                    string.Empty));
            }

            return _badgeCards[index];
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            FontStyles fontStyle = FontStyles.Normal)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = GameUiPalette.PrimaryText;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void SetPreferredHeight(GameObject target, float height)
        {
            if (target == null)
            {
                return;
            }

            var layout = target.GetComponent<LayoutElement>()
                ?? target.AddComponent<LayoutElement>();
            layout.minHeight = 0f;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        private static GameObject CreateSection(
            string objectName,
            Transform parent,
            float preferredHeight)
        {
            var section = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            section.transform.SetParent(parent, false);
            section.GetComponent<Image>().color = GameUiPalette.StatusSection;
            SetPreferredHeight(section, preferredHeight);
            return section;
        }

        private static void CreateSectionTitle(Transform section, string title)
        {
            var text = CreateText("Title", section, 18f, FontStyles.Bold);
            text.text = title;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(10f, -32f);
            rect.offsetMax = new Vector2(-10f, -4f);
        }

        private static RectTransform CreateGrid(
            Transform section,
            string objectName,
            int columns,
            float cellHeight)
        {
            var gridObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(LayoutElement),
                typeof(ResponsiveGridLayout));
            gridObject.transform.SetParent(section, false);
            var rect = gridObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -36f);
            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(6f, 6f);
            grid.childAlignment = TextAnchor.UpperCenter;
            gridObject.GetComponent<ResponsiveGridLayout>()
                .Configure(columns, 72f, cellHeight);
            return rect;
        }

        private static RectTransform CreateRewardSummaryLines(Transform section)
        {
            var lines = new GameObject(
                "Lines",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            lines.transform.SetParent(section, false);
            var rect = lines.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 8f);
            rect.offsetMax = new Vector2(-12f, -36f);
            var layout = lines.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        private void RebuildRewardSummary(
            IReadOnlyList<TrainerRewardIconContent> rewards,
            Sprite goldIcon,
            int? gold,
            bool hasReward)
        {
            if (_rewardSummarySection == null || _rewardSummaryLines == null)
            {
                return;
            }

            _rewardElementsLine?.SetActive(false);
            _goldSummaryLine?.SetActive(false);

            _rewardSummarySection.SetActive(hasReward);
            if (!hasReward)
            {
                return;
            }

            var lineCount = 0;
            if (rewards != null && rewards.Count > 0)
            {
                BindRewardElementsLine(rewards);
                lineCount++;
            }

            if (gold.HasValue)
            {
                BindGoldSummaryLine(goldIcon, gold.Value);
                lineCount++;
            }

            SetPreferredHeight(
                _rewardSummarySection,
                44f + Mathf.Max(1, lineCount) * 30f);
        }

        private void BindGoldSummaryLine(Sprite goldIcon, int gold)
        {
            EnsureGoldSummaryLine();
            _goldSummaryLine.SetActive(true);
            _goldSummaryLine.transform.SetAsLastSibling();
            _goldSummaryIcon.sprite = goldIcon;
            _goldSummaryIcon.enabled = goldIcon != null;
            _goldSummaryValue.text = $"+{gold}";
        }

        private void EnsureGoldSummaryLine()
        {
            if (_goldSummaryLine != null)
            {
                return;
            }

            _goldSummaryLine = new GameObject(
                "GoldLine",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            _goldSummaryLine.transform.SetParent(_rewardSummaryLines, false);
            var layout = _goldSummaryLine.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            SetPreferredHeight(_goldSummaryLine, 26f);

            var iconObject = new GameObject(
                "GoldIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            iconObject.transform.SetParent(_goldSummaryLine.transform, false);
            _goldSummaryIcon = iconObject.GetComponent<Image>();
            _goldSummaryIcon.preserveAspect = true;
            _goldSummaryIcon.raycastTarget = false;
            var iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.minWidth = 26f;
            iconLayout.preferredWidth = 26f;
            iconLayout.flexibleWidth = 0f;

            _goldSummaryValue = CreateText(
                "Value",
                _goldSummaryLine.transform,
                18f,
                FontStyles.Bold);
            _goldSummaryValue.richText = true;
            _goldSummaryValue.enableAutoSizing = false;
            _goldSummaryValue.alignment = TextAlignmentOptions.MidlineLeft;
            SetPreferredHeight(_goldSummaryValue.gameObject, 26f);
        }

        private void BindRewardElementsLine(
            IReadOnlyList<TrainerRewardIconContent> rewards)
        {
            EnsureRewardElementsLine();
            _rewardElementsLine.SetActive(true);
            _rewardElementsLine.transform.SetAsFirstSibling();
            for (var index = 0; index < rewards.Count; index++)
            {
                var view = GetOrCreateRewardElement(index);
                view.Root.SetActive(true);
                BindRewardElement(view, rewards[index]);
            }

            for (var index = rewards.Count; index < _rewardElements.Count; index++)
            {
                _rewardElements[index].Root.SetActive(false);
            }
        }

        private void EnsureRewardElementsLine()
        {
            if (_rewardElementsLine != null)
            {
                return;
            }

            _rewardElementsLine = new GameObject(
                "RewardElementsLine",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            _rewardElementsLine.transform.SetParent(_rewardSummaryLines, false);
            var lineLayout = _rewardElementsLine.GetComponent<HorizontalLayoutGroup>();
            lineLayout.spacing = 12f;
            lineLayout.childAlignment = TextAnchor.MiddleLeft;
            lineLayout.childControlWidth = true;
            lineLayout.childControlHeight = true;
            lineLayout.childForceExpandWidth = false;
            lineLayout.childForceExpandHeight = true;
            SetPreferredHeight(_rewardElementsLine, 30f);
        }

        private RewardElementBinding GetOrCreateRewardElement(int index)
        {
            while (_rewardElements.Count <= index)
            {
                _rewardElements.Add(CreateRewardElement(
                    _rewardElementsLine.transform,
                    _rewardElements.Count));
            }

            return _rewardElements[index];
        }

        private static RewardElementBinding CreateRewardElement(
            Transform parent,
            int index)
        {
            var root = new GameObject(
                $"RewardElement_{index + 1}",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            root.transform.SetParent(parent, false);
            var layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var labelRoot = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            labelRoot.transform.SetParent(root.transform, false);
            var labelLayout = labelRoot.GetComponent<LayoutElement>();
            labelLayout.flexibleWidth = 0f;

            var label = CreateText(
                "Text",
                labelRoot.transform,
                34f,
                FontStyles.Bold);
            label.richText = true;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(3f, 1f);
            labelRect.offsetMax = new Vector2(-3f, -1f);

            var amount = CreateText(
                "Amount",
                root.transform,
                18f,
                FontStyles.Bold);
            amount.enableAutoSizing = false;
            amount.alignment = TextAlignmentOptions.MidlineLeft;
            var amountLayout = amount.gameObject.AddComponent<LayoutElement>();
            amountLayout.minWidth = 46f;
            amountLayout.preferredWidth = 46f;
            amountLayout.flexibleWidth = 0f;
            return new RewardElementBinding(
                root,
                labelRoot.GetComponent<Image>(),
                labelLayout,
                label,
                amount);
        }

        private static void BindRewardElement(
            RewardElementBinding view,
            TrainerRewardIconContent reward)
        {
            var color = GameUiPalette.StatCard;
            ColorUtility.TryParseHtmlString(reward.ColorHex, out color);
            view.Background.color = reward.UseColoredBackground
                ? color
                : GameUiPalette.Transparent;
            var labelWidth = reward.Label.EndsWith("Badge", StringComparison.Ordinal)
                ? 104f
                : 48f;
            view.LabelLayout.minWidth = labelWidth;
            view.LabelLayout.preferredWidth = labelWidth;
            view.Label.text = reward.Label;
            view.Label.fontSize = reward.UseColoredBackground ? 13f : 34f;
            view.Label.enableAutoSizing = reward.UseColoredBackground;
            view.Label.fontSizeMin = 10f;
            view.Label.fontSizeMax = reward.UseColoredBackground ? 13f : 34f;
            view.Label.color = reward.UseColoredBackground
                ? AttributeCardPalette.GetReadableTextColor(color)
                : Color.white;
            view.Amount.text = $"+{reward.Amount}";
        }

        private static TMP_Text CreateCard(
            Transform parent,
            Color backgroundColor,
            string textValue)
        {
            var card = new GameObject(
                "Card",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = backgroundColor;
            var text = CreateText("Text", card.transform, 16f, FontStyles.Bold);
            text.text = textValue;
            text.color = AttributeCardPalette.GetReadableTextColor(backgroundColor);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);
            return text;
        }

        private static TMP_Text CreateStatCard(
            Transform parent,
            PachimonStatType statType)
        {
            var card = new GameObject(
                statType.ToString(),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = GameUiPalette.StatCard;
            var row = card.GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(4, 8, 4, 4);
            row.spacing = 6f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            var icon = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            icon.transform.SetParent(card.transform, false);
            var iconLayout = icon.GetComponent<LayoutElement>();
            iconLayout.minWidth = 48f;
            iconLayout.preferredWidth = 48f;
            iconLayout.flexibleWidth = 0f;

            var hasAttributeIcon = TryGetDisplayStat(statType, out var displayStat);
            icon.GetComponent<Image>().color = hasAttributeIcon
                ? GameUiPalette.Transparent
                : GetStatColor(statType);
            var iconText = CreateText("Label", icon.transform, 11f, FontStyles.Bold);
            iconText.richText = true;
            iconText.text = hasAttributeIcon
                ? AttributeRichText.GetIcon(displayStat)
                : GetStatLabel(statType);
            iconText.fontSize = hasAttributeIcon
                ? AttributeRichText.StatLabelIconFontSize
                : 11f;
            iconText.enableAutoSizing = false;
            iconText.fontSizeMax = iconText.fontSize;
            iconText.color = hasAttributeIcon
                ? Color.white
                : AttributeCardPalette.GetReadableTextColor(GetStatColor(statType));
            var iconTextRect = iconText.rectTransform;
            iconTextRect.anchorMin = Vector2.zero;
            iconTextRect.anchorMax = Vector2.one;
            iconTextRect.offsetMin = Vector2.zero;
            iconTextRect.offsetMax = Vector2.zero;

            var value = CreateText("Value", card.transform, 17f, FontStyles.Bold);
            value.text = "0";
            value.enableAutoSizing = false;
            value.color = GameUiPalette.PrimaryText;
            value.alignment = TextAlignmentOptions.MidlineRight;
            var valueLayout = value.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;
            return value;
        }

        private static void CreateStatSpacer(Transform parent, string objectName)
        {
            var spacer = new GameObject(objectName, typeof(RectTransform));
            spacer.transform.SetParent(parent, false);
        }

        private static bool TryGetDisplayStat(
            PachimonStatType statType,
            out PachimonDisplayStat displayStat)
        {
            displayStat = statType switch
            {
                PachimonStatType.Fire => PachimonDisplayStat.Fire,
                PachimonStatType.Aqua => PachimonDisplayStat.Aqua,
                PachimonStatType.Leaf => PachimonDisplayStat.Leaf,
                PachimonStatType.Electric => PachimonDisplayStat.Electric,
                PachimonStatType.Poison => PachimonDisplayStat.Poison,
                PachimonStatType.Ice => PachimonDisplayStat.Ice,
                PachimonStatType.Wind => PachimonDisplayStat.Wind,
                PachimonStatType.Dragon => PachimonDisplayStat.Dragon,
                _ => default,
            };
            return statType >= PachimonStatType.Fire
                && statType <= PachimonStatType.Dragon;
        }

        private static Color GetStatColor(PachimonStatType statType)
        {
            if (PachimonStatTypeUtility.TryGetAttribute(statType, out var attribute))
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
                PachimonStatType.Fire => "炎",
                PachimonStatType.Aqua => "水",
                PachimonStatType.Leaf => "草",
                PachimonStatType.Electric => "電",
                PachimonStatType.Poison => "毒",
                PachimonStatType.Ice => "氷",
                PachimonStatType.Wind => "風",
                PachimonStatType.Dragon => "竜",
                PachimonStatType.Speed => "SPD",
                PachimonStatType.Haste => "HST",
                PachimonStatType.DamageBonus => "DB",
                PachimonStatType.ResistBonus => "RB",
                _ => statType.ToString(),
            };
        }

        private static string GetAttributeLabel(PachimonAttribute attribute)
        {
            return GetStatLabel(PachimonStatTypeUtility.FromAttribute(attribute));
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

    }
}
