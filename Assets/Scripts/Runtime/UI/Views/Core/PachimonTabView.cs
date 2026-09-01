using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Pachimon.Battle;
using Pachimon.Items;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public enum PachimonDisplayStat
    {
        Fire, Aqua, Leaf, Electric,
        Poison, Ice, Wind, Dragon,
        Speed, Haste, DamageBonus, ResistBonus,
        GenerationPower, StatusMastery, SustainPower, StatusResistance,
    }

    public readonly struct PachimonStatPreview
    {
        public PachimonStatPreview(
            PachimonDisplayStat stat,
            int value,
            PachimonDisplayStat? boundSubStat = null)
        {
            Stat = stat;
            Value = value;
            BoundSubStat = boundSubStat;
        }

        public PachimonDisplayStat Stat { get; }
        public int Value { get; }
        public PachimonDisplayStat? BoundSubStat { get; }
    }

    public enum PachimonAbilityKind
    {
        Skill,
        Passive,
    }

    public readonly struct PachimonAbilityPreview
    {
        public PachimonAbilityPreview(
            PachimonAbilityKind kind,
            int id,
            string displayName,
            SkillAsset skill = null,
            int upgradeLevel = 0)
        {
            Kind = kind;
            Id = id;
            DisplayName = displayName ?? string.Empty;
            Skill = skill;
            UpgradeLevel = upgradeLevel;
        }

        public PachimonAbilityKind Kind { get; }
        public int Id { get; }
        public string DisplayName { get; }
        public SkillAsset Skill { get; }
        public int UpgradeLevel { get; }
    }

    public readonly struct PachimonStatusPreview
    {
        public PachimonStatusPreview(BattleStatusInstance instance)
        {
            Instance = instance;
        }

        public BattleStatusInstance Instance { get; }
        public string DisplayName => Instance?.DisplayName ?? string.Empty;
    }

    public readonly struct PachimonEquipmentPreview
    {
        public PachimonEquipmentPreview(
            EquipmentSlot slot,
            string displayName,
            GeneratedItemData generatedData)
        {
            Slot = slot;
            DisplayName = displayName ?? string.Empty;
            GeneratedData = generatedData;
        }

        public EquipmentSlot Slot { get; }
        public string DisplayName { get; }
        public GeneratedItemData GeneratedData { get; }
    }

    public readonly struct PachimonEngravingPreview
    {
        public PachimonEngravingPreview(
            string displayName,
            GeneratedItemData generatedData)
        {
            DisplayName = displayName ?? string.Empty;
            GeneratedData = generatedData;
        }

        public string DisplayName { get; }
        public GeneratedItemData GeneratedData { get; }
    }

    public sealed class PachimonPreviewContent
    {
        private readonly Dictionary<PachimonDisplayStat, int> _stats;
        private readonly Dictionary<PachimonDisplayStat, PachimonDisplayStat> _subStatBindings;

        public PachimonPreviewContent(
            Sprite frontSprite,
            string displayName,
            int currentHp,
            int maxHp,
            int currentShield,
            int currentMn,
            int maxMn,
            IEnumerable<PachimonStatPreview> stats,
            IEnumerable<PachimonStatusPreview> statusEffects,
            IEnumerable<PachimonAbilityPreview> skills,
            IEnumerable<PachimonAbilityPreview> passives,
            StatCalculationResult statCalculation = null,
            IEnumerable<PachimonEquipmentPreview> equipment = null,
            IEnumerable<PachimonEngravingPreview> engravings = null)
        {
            IsRevealed = true;
            FrontSprite = frontSprite;
            DisplayName = displayName;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            CurrentShield = currentShield;
            CurrentMn = currentMn;
            MaxMn = maxMn;
            _stats = stats?.ToDictionary(item => item.Stat, item => item.Value) ?? new();
            _subStatBindings = stats?
                .Where(item => item.BoundSubStat.HasValue)
                .ToDictionary(item => item.Stat, item => item.BoundSubStat.Value)
                ?? new Dictionary<PachimonDisplayStat, PachimonDisplayStat>();
            StatusEffects = statusEffects?.ToArray()
                ?? Array.Empty<PachimonStatusPreview>();
            Skills = skills?.ToArray() ?? Array.Empty<PachimonAbilityPreview>();
            Passives = passives?.ToArray() ?? Array.Empty<PachimonAbilityPreview>();
            Equipment = equipment?.ToArray()
                ?? Array.Empty<PachimonEquipmentPreview>();
            Engravings = engravings?.ToArray()
                ?? Array.Empty<PachimonEngravingPreview>();
            StatCalculation = statCalculation;
        }

        private PachimonPreviewContent()
        {
            _stats = new Dictionary<PachimonDisplayStat, int>();
            _subStatBindings = new Dictionary<PachimonDisplayStat, PachimonDisplayStat>();
            StatusEffects = Array.Empty<PachimonStatusPreview>();
            Skills = Array.Empty<PachimonAbilityPreview>();
            Passives = Array.Empty<PachimonAbilityPreview>();
            Equipment = Array.Empty<PachimonEquipmentPreview>();
            Engravings = Array.Empty<PachimonEngravingPreview>();
        }

        public bool IsRevealed { get; }
        public Sprite FrontSprite { get; }
        public string DisplayName { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public int CurrentShield { get; }
        public int CurrentMn { get; }
        public int MaxMn { get; }
        public IReadOnlyList<PachimonStatusPreview> StatusEffects { get; }
        public IReadOnlyList<PachimonAbilityPreview> Skills { get; }
        public IReadOnlyList<PachimonAbilityPreview> Passives { get; }
        public IReadOnlyList<PachimonEquipmentPreview> Equipment { get; }
        public IReadOnlyList<PachimonEngravingPreview> Engravings { get; }
        public StatCalculationResult StatCalculation { get; }
        public static PachimonPreviewContent Hidden { get; } = new();

        public bool TryGetStat(PachimonDisplayStat stat, out int value)
        {
            return _stats.TryGetValue(stat, out value);
        }

        public bool TryGetBoundSubStat(
            PachimonDisplayStat attribute,
            out PachimonDisplayStat subStat)
        {
            return _subStatBindings.TryGetValue(attribute, out subStat);
        }
    }

    public sealed class PachimonTabView : MonoBehaviour
    {
        public const int SkillSlotCount = PachimonInstance.MaxSkillSlots;

        private static readonly Color HealthyHpColor =
            new(0.25f, 0.67f, 0.34f, 1f);
        private static readonly Color WarningHpColor =
            new(0.86f, 0.66f, 0.18f, 1f);
        private static readonly Color CriticalHpColor =
            new(0.78f, 0.22f, 0.20f, 1f);
        private static readonly Color EmptyHpColor =
            new(0.32f, 0.32f, 0.32f, 1f);
        private static readonly Color MnColor =
            new(0.20f, 0.52f, 0.86f, 1f);
        private static readonly Color ShieldColor =
            new(0.58f, 0.61f, 0.64f, 1f);
        private static readonly PachimonDisplayStat[] SubStatDisplayOrder =
        {
            PachimonDisplayStat.DamageBonus,
            PachimonDisplayStat.ResistBonus,
            PachimonDisplayStat.StatusMastery,
            PachimonDisplayStat.StatusResistance,
            PachimonDisplayStat.GenerationPower,
            PachimonDisplayStat.SustainPower,
            PachimonDisplayStat.Speed,
            PachimonDisplayStat.Haste,
        };
        private static readonly IReadOnlyDictionary<PachimonDisplayStat, PachimonDisplayStat>
            DefaultSubStatBindings =
                new Dictionary<PachimonDisplayStat, PachimonDisplayStat>
                {
                    [PachimonDisplayStat.Fire] = PachimonDisplayStat.DamageBonus,
                    [PachimonDisplayStat.Aqua] = PachimonDisplayStat.GenerationPower,
                    [PachimonDisplayStat.Leaf] = PachimonDisplayStat.Haste,
                    [PachimonDisplayStat.Electric] = PachimonDisplayStat.Speed,
                    [PachimonDisplayStat.Poison] = PachimonDisplayStat.StatusMastery,
                    [PachimonDisplayStat.Ice] = PachimonDisplayStat.ResistBonus,
                    [PachimonDisplayStat.Wind] = PachimonDisplayStat.SustainPower,
                    [PachimonDisplayStat.Dragon] = PachimonDisplayStat.StatusResistance,
                };

        [SerializeField] private Image _frontGraphic;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _mnText;
        [SerializeField] private PachimonStatSlotView[] _statSlots = Array.Empty<PachimonStatSlotView>();
        [SerializeField] private Transform _statusContainer;
        [SerializeField] private TextChipView _statusTemplate;
        [SerializeField] private TextChipView[] _skillSlots = Array.Empty<TextChipView>();
        [SerializeField] private Transform _passiveContainer;
        [SerializeField] private TextChipView _passiveTemplate;
        [SerializeField] private Transform _equipmentContainer;
        [SerializeField] private TextChipView _equipmentTemplate;
        [SerializeField] private Transform _engravingContainer;
        [SerializeField] private TextChipView _engravingTemplate;
        private RectTransform _runtimeHpBar;
        private Image _runtimeHpFill;
        private Image _runtimeHpShield;
        private RectTransform _runtimeMnBar;
        private Image _runtimeMnFill;
        private PachimonPreviewContent _boundPreview = PachimonPreviewContent.Hidden;
        public RectTransform GraphicRect => _frontGraphic?.rectTransform;
        public event Action<PachimonAbilityPreview, PachimonPreviewContent>
            AbilityDetailsRequested;
        public event Action<PachimonStatusPreview> StatusDetailsRequested;

        private void LateUpdate()
        {
            SyncRuntimeHpBarTransform();
            SyncRuntimeMnBarTransform();
        }

        public void Configure(
            Image frontGraphic,
            TMP_Text nameText,
            TMP_Text hpText,
            TMP_Text mnText,
            PachimonStatSlotView[] statSlots,
            Transform statusContainer,
            TextChipView statusTemplate,
            TextChipView[] skillSlots,
            Transform passiveContainer,
            TextChipView passiveTemplate,
            Transform equipmentContainer = null,
            TextChipView equipmentTemplate = null,
            Transform engravingContainer = null,
            TextChipView engravingTemplate = null)
        {
            _frontGraphic = frontGraphic;
            _nameText = nameText;
            _hpText = hpText;
            _mnText = mnText;
            _statSlots = statSlots;
            _statusContainer = statusContainer;
            _statusTemplate = statusTemplate;
            _skillSlots = skillSlots;
            _passiveContainer = passiveContainer;
            _passiveTemplate = passiveTemplate;
            _equipmentContainer = equipmentContainer;
            _equipmentTemplate = equipmentTemplate;
            _engravingContainer = engravingContainer;
            _engravingTemplate = engravingTemplate;
        }

        public void Bind(PachimonPreviewContent preview)
        {
            preview ??= PachimonPreviewContent.Hidden;
            _boundPreview = preview;
            var revealed = preview.IsRevealed;
            EnsureInventorySections();
            EnsureAttributeGridLayout(preview);

            if (_frontGraphic != null)
            {
                _frontGraphic.sprite = revealed ? preview.FrontSprite : null;
                _frontGraphic.color = revealed && preview.FrontSprite != null
                    ? Color.white
                    : GameUiPalette.MissingGraphic;
            }

            if (_nameText != null) _nameText.text = revealed ? preview.DisplayName : "?";
            if (_hpText != null)
            {
                EnsureRuntimeHpBar();
                _hpText.text = _mnText != null
                    ? revealed
                        ? $"HP  {preview.CurrentHp} / {preview.MaxHp}"
                        : "HP  ? / ?"
                    : revealed
                        ? $"HP  {preview.CurrentHp} / {preview.MaxHp}\n"
                            + $"MN  {preview.CurrentMn} / {preview.MaxMn}"
                        : "HP  ? / ?\nMN  ? / ?";
                _hpText.color = Color.white;
                _hpText.fontStyle |= FontStyles.Bold;
                SetRuntimeHpFill(
                    revealed ? preview.CurrentHp : 0,
                    revealed ? preview.MaxHp : 0,
                    revealed ? preview.CurrentShield : 0);
            }

            if (_mnText != null)
            {
                EnsureRuntimeMnBar();
                _mnText.text = revealed
                    ? $"MN  {preview.CurrentMn} / {preview.MaxMn}"
                    : "MN  ? / ?";
                _mnText.color = Color.white;
                _mnText.fontStyle |= FontStyles.Bold;
                var mnRatio = revealed && preview.MaxMn > 0
                    ? Mathf.Clamp01((float)preview.CurrentMn / preview.MaxMn)
                    : 0f;
                SetRuntimeMnFill(mnRatio);
            }

            foreach (var slot in _statSlots)
            {
                if (slot == null) continue;
                if (!AttributeRichText.IsAttribute(slot.Stat))
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }
                slot.gameObject.SetActive(true);
                var value = 0;
                var hasValue = revealed && preview.TryGetStat(slot.Stat, out value);
                PachimonDisplayStat? boundSubStat = null;
                int? boundSubStatValue = null;
                if (hasValue
                    && preview.TryGetBoundSubStat(slot.Stat, out var resolvedSubStat))
                {
                    boundSubStat = resolvedSubStat;
                    if (preview.TryGetStat(resolvedSubStat, out var resolvedSubStatValue))
                    {
                        boundSubStatValue = resolvedSubStatValue;
                    }
                }
                slot.Bind(
                    hasValue ? value.ToString() : "?",
                    boundSubStat,
                    boundSubStatValue);
            }

            if (revealed)
            {
                RebuildStatusChips(
                    _statusContainer,
                    _statusTemplate,
                    preview.StatusEffects,
                    RequestStatusDetails);
            }
            else
            {
                RebuildChips(
                    _statusContainer,
                    _statusTemplate,
                    new[] { "?" },
                    "なし");
            }

            for (var index = 0; index < _skillSlots.Length; index++)
            {
                var slotObject = _skillSlots[index]?.gameObject;
                if (slotObject != null)
                {
                    slotObject.SetActive(index < SkillSlotCount);
                }
                if (index >= SkillSlotCount) continue;

                if (!revealed)
                {
                    _skillSlots[index]?.Bind("?");
                }
                else if (index < preview.Skills.Count)
                {
                    var ability = preview.Skills[index];
                    _skillSlots[index]?.Bind(
                        ability.DisplayName,
                        () => RequestAbilityDetails(ability));
                    _skillSlots[index]?.SetAttributeColors(
                        AttributeCardPalette.GetSkillColors(ability.Skill));
                }
                else
                {
                    _skillSlots[index]?.Bind("---");
                }
            }

            if (revealed)
            {
                RebuildAbilityChips(
                    _passiveContainer,
                    _passiveTemplate,
                    preview.Passives,
                    RequestAbilityDetails);
            }
            else
            {
                RebuildChips(
                    _passiveContainer,
                    _passiveTemplate,
                    new[] { "?" },
                    "なし");
            }

            RebuildEquipmentChips(revealed, preview.Equipment);
            RebuildEngravingChips(revealed, preview.Engravings);
        }

        private void RebuildEquipmentChips(
            bool revealed,
            IReadOnlyList<PachimonEquipmentPreview> equipment)
        {
            if (_equipmentContainer == null || _equipmentTemplate == null)
            {
                return;
            }

            DeactivateChipPool(_equipmentContainer, _equipmentTemplate);
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var chip = GetOrCreatePooledChip(
                    _equipmentContainer,
                    _equipmentTemplate,
                    (int)slot);
                chip.gameObject.SetActive(true);
                var equipped = equipment.FirstOrDefault(item => item.Slot == slot);
                if (!revealed)
                {
                    chip.Bind($"{GetEquipmentSlotLabel(slot)}：?");
                }
                else if (string.IsNullOrWhiteSpace(equipped.DisplayName))
                {
                    chip.Bind($"{GetEquipmentSlotLabel(slot)}：なし");
                }
                else
                {
                    chip.Bind($"{GetEquipmentSlotLabel(slot)}：{equipped.DisplayName}\n"
                        + FormatStatChanges(equipped.GeneratedData, false));
                    ApplyGeneratedEffectColor(chip, equipped.GeneratedData);
                }
            }

            _equipmentTemplate.gameObject.SetActive(false);
            _equipmentContainer.GetComponent<ResponsiveGridLayout>()?.RefreshLayout();
        }

        private void RebuildEngravingChips(
            bool revealed,
            IReadOnlyList<PachimonEngravingPreview> engravings)
        {
            if (_engravingContainer == null || _engravingTemplate == null)
            {
                return;
            }

            DeactivateChipPool(_engravingContainer, _engravingTemplate);
            if (!revealed || engravings.Count == 0)
            {
                var chip = GetOrCreatePooledChip(
                    _engravingContainer,
                    _engravingTemplate,
                    0);
                chip.gameObject.SetActive(true);
                chip.Bind(revealed ? "なし" : "?");
            }
            else
            {
                for (var index = 0; index < engravings.Count; index++)
                {
                    var engraving = engravings[index];
                    var chip = GetOrCreatePooledChip(
                        _engravingContainer,
                        _engravingTemplate,
                        index);
                    chip.gameObject.SetActive(true);
                    chip.Bind($"{engraving.DisplayName}\n"
                        + FormatStatChanges(engraving.GeneratedData, false));
                    ApplyGeneratedEffectColor(chip, engraving.GeneratedData);
                }
            }

            _engravingTemplate.gameObject.SetActive(false);
            _engravingContainer.GetComponent<ResponsiveGridLayout>()?.RefreshLayout();
        }

        private static string FormatStatChanges(
            GeneratedItemData generatedData,
            bool subStatIsDerivationRatio)
        {
            if (generatedData?.StatChanges == null
                || generatedData.StatChanges.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("/", generatedData.StatChanges.Select(change =>
                CityShopWindowView.FormatStatChange(
                    change,
                    subStatIsDerivationRatio)));
        }

        private static void ApplyGeneratedEffectColor(
            TextChipView chip,
            GeneratedItemData generatedData)
        {
            var main = generatedData?.StatChanges.FirstOrDefault(
                change => change.Amount > 0);
            if (main == null
                || !PachimonStatTypeUtility.TryGetAttribute(
                    main.StatType,
                    out var attribute))
            {
                return;
            }

            chip.SetAttributeColors(new[]
            {
                RewardElementPalette.GetAttributeColor(attribute),
            });
        }

        private static string GetEquipmentSlotLabel(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Head => "頭",
                EquipmentSlot.Body => "胴",
                EquipmentSlot.Feet => "足",
                _ => throw new ArgumentOutOfRangeException(nameof(slot)),
            };
        }

        private void EnsureInventorySections()
        {
            if (_equipmentContainer != null
                && _equipmentTemplate != null
                && _engravingContainer != null
                && _engravingTemplate != null)
            {
                return;
            }
            if (_passiveContainer == null || _passiveTemplate == null)
            {
                return;
            }

            var passiveSection = _passiveContainer.parent;
            var content = passiveSection?.parent;
            if (content == null)
            {
                return;
            }

            EnsureRuntimeCollectionSection(
                content,
                "EquipmentSection",
                "装備",
                "EquipmentGrid",
                "EquipmentTemplate",
                GameUiPalette.SkillSection,
                GameUiPalette.ButtonNeutral,
                ref _equipmentContainer,
                ref _equipmentTemplate);
            EnsureRuntimeCollectionSection(
                content,
                "EngravingSection",
                "刻印",
                "EngravingGrid",
                "EngravingTemplate",
                GameUiPalette.PassiveSection,
                GameUiPalette.ItemChip,
                ref _engravingContainer,
                ref _engravingTemplate);

            var equipmentSection = _equipmentContainer?.parent;
            var engravingSection = _engravingContainer?.parent;
            if (equipmentSection != null)
            {
                equipmentSection.SetSiblingIndex(
                    passiveSection.GetSiblingIndex() + 1);
            }
            if (engravingSection != null)
            {
                engravingSection.SetSiblingIndex(
                    equipmentSection != null
                        ? equipmentSection.GetSiblingIndex() + 1
                        : passiveSection.GetSiblingIndex() + 1);
            }
        }

        private void EnsureRuntimeCollectionSection(
            Transform content,
            string sectionName,
            string title,
            string gridName,
            string templateName,
            Color sectionColor,
            Color chipColor,
            ref Transform container,
            ref TextChipView template)
        {
            var existingSection = content.Find(sectionName);
            if (existingSection != null)
            {
                container ??= existingSection.Find(gridName);
                template ??= container?.Find(templateName)
                    ?.GetComponent<TextChipView>();
                if (container != null && template != null)
                {
                    return;
                }
            }

            var sectionObject = new GameObject(
                sectionName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            sectionObject.layer = gameObject.layer;
            sectionObject.transform.SetParent(content, false);
            sectionObject.GetComponent<Image>().color = sectionColor;
            var outline = sectionObject.GetComponent<Outline>();
            outline.effectColor = GameUiPalette.Border;
            outline.effectDistance = new Vector2(1f, -1f);
            var vertical = sectionObject.GetComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(10, 10, 9, 10);
            vertical.spacing = 8f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            sectionObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var sourceLabel = _passiveTemplate.GetComponentInChildren<TMP_Text>(true);
            var titleObject = sourceLabel != null
                ? Instantiate(sourceLabel.gameObject, sectionObject.transform, false)
                : new GameObject(
                    "Title",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
            if (sourceLabel == null)
            {
                titleObject.layer = gameObject.layer;
                titleObject.transform.SetParent(sectionObject.transform, false);
            }
            titleObject.name = "Title";
            titleObject.SetActive(true);
            var titleText = titleObject.GetComponent<TMP_Text>();
            titleText.text = title;
            titleText.fontSize = 18f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = GameUiPalette.PrimaryText;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.raycastTarget = false;
            var titleLayout = titleObject.GetComponent<LayoutElement>()
                ?? titleObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 28f;

            var gridObject = new GameObject(
                gridName,
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(LayoutElement),
                typeof(ResponsiveGridLayout));
            gridObject.layer = gameObject.layer;
            gridObject.transform.SetParent(sectionObject.transform, false);
            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(7f, 7f);
            grid.childAlignment = TextAnchor.UpperLeft;
            gridObject.GetComponent<ResponsiveGridLayout>().Configure(3, 80f, 58f);

            var templateObject = Instantiate(
                _passiveTemplate.gameObject,
                gridObject.transform,
                false);
            templateObject.name = templateName;
            templateObject.GetComponent<Image>().color = chipColor;
            template = templateObject.GetComponent<TextChipView>();
            var label = templateObject.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 12f;
                label.text = "なし";
            }
            templateObject.SetActive(false);
            container = gridObject.transform;
        }

        private void EnsureAttributeGridLayout(PachimonPreviewContent preview)
        {
            ResponsiveGridLayout grid = null;
            foreach (var slot in _statSlots)
            {
                if (slot == null || slot.transform.parent == null)
                {
                    continue;
                }

                grid = slot.transform.parent.GetComponent<ResponsiveGridLayout>();
                if (grid != null)
                {
                    grid.Configure(2, 120f, 38f);
                }
                break;
            }

            if (grid == null)
            {
                return;
            }

            var orderedSlots = _statSlots
                .Where(slot => slot != null && AttributeRichText.IsAttribute(slot.Stat))
                .OrderBy(slot => GetSubStatDisplayIndex(preview, slot.Stat))
                .ThenBy(slot => (int)slot.Stat)
                .ToArray();
            for (var orderIndex = 0; orderIndex < orderedSlots.Length; orderIndex++)
            {
                orderedSlots[orderIndex].transform.SetSiblingIndex(orderIndex);
            }

            grid.RefreshLayout();
        }

        private static int GetSubStatDisplayIndex(
            PachimonPreviewContent preview,
            PachimonDisplayStat attribute)
        {
            if (preview == null
                || !preview.TryGetBoundSubStat(attribute, out var subStat))
            {
                DefaultSubStatBindings.TryGetValue(attribute, out subStat);
            }

            var index = Array.IndexOf(SubStatDisplayOrder, subStat);
            return index >= 0 ? index : SubStatDisplayOrder.Length;
        }

        private void RequestAbilityDetails(PachimonAbilityPreview ability)
        {
            AbilityDetailsRequested?.Invoke(ability, _boundPreview);
        }

        private void RequestStatusDetails(PachimonStatusPreview status)
        {
            StatusDetailsRequested?.Invoke(status);
        }

        private void EnsureRuntimeHpBar()
        {
            if (_runtimeHpBar != null || _hpText == null)
            {
                return;
            }

            var parent = _hpText.transform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var barObject = new GameObject(
                "RuntimePaneHpGauge",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            barObject.layer = gameObject.layer;
            _runtimeHpBar = barObject.GetComponent<RectTransform>();
            _runtimeHpBar.SetParent(parent, false);
            _runtimeHpBar.SetSiblingIndex(_hpText.transform.GetSiblingIndex());

            var layout = barObject.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var track = barObject.GetComponent<Image>();
            track.color = new Color(0.12f, 0.15f, 0.16f, 1f);
            track.raycastTarget = false;

            var fillObject = new GameObject(
                "Fill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            fillObject.layer = gameObject.layer;
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.SetParent(_runtimeHpBar, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            _runtimeHpFill = fillObject.GetComponent<Image>();
            _runtimeHpFill.raycastTarget = false;

            var shieldObject = new GameObject(
                "Shield",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            shieldObject.layer = gameObject.layer;
            var shieldRect = shieldObject.GetComponent<RectTransform>();
            shieldRect.SetParent(_runtimeHpBar, false);
            _runtimeHpShield = shieldObject.GetComponent<Image>();
            _runtimeHpShield.color = ShieldColor;
            _runtimeHpShield.raycastTarget = false;
            _runtimeHpShield.enabled = false;

            SyncRuntimeHpBarTransform();
        }

        private void SyncRuntimeHpBarTransform()
        {
            if (_runtimeHpBar == null || _hpText == null)
            {
                return;
            }

            var source = _hpText.rectTransform;
            if (HasSameGeometry(_runtimeHpBar, source))
            {
                return;
            }

            _runtimeHpBar.anchorMin = source.anchorMin;
            _runtimeHpBar.anchorMax = source.anchorMax;
            _runtimeHpBar.pivot = source.pivot;
            _runtimeHpBar.anchoredPosition = source.anchoredPosition;
            _runtimeHpBar.sizeDelta = source.sizeDelta;
        }

        private void EnsureRuntimeMnBar()
        {
            if (_runtimeMnBar != null || _mnText == null)
            {
                return;
            }

            var parent = _mnText.transform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var barObject = new GameObject(
                "RuntimePaneMnGauge",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            barObject.layer = gameObject.layer;
            _runtimeMnBar = barObject.GetComponent<RectTransform>();
            _runtimeMnBar.SetParent(parent, false);
            _runtimeMnBar.SetSiblingIndex(_mnText.transform.GetSiblingIndex());

            var layout = barObject.GetComponent<LayoutElement>();
            layout.ignoreLayout = true;

            var track = barObject.GetComponent<Image>();
            track.color = new Color(0.12f, 0.15f, 0.16f, 1f);
            track.raycastTarget = false;

            var fillObject = new GameObject(
                "Fill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            fillObject.layer = gameObject.layer;
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.SetParent(_runtimeMnBar, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            _runtimeMnFill = fillObject.GetComponent<Image>();
            _runtimeMnFill.raycastTarget = false;

            SyncRuntimeMnBarTransform();
        }

        private void SyncRuntimeMnBarTransform()
        {
            if (_runtimeMnBar == null || _mnText == null)
            {
                return;
            }

            var source = _mnText.rectTransform;
            if (HasSameGeometry(_runtimeMnBar, source))
            {
                return;
            }

            _runtimeMnBar.anchorMin = source.anchorMin;
            _runtimeMnBar.anchorMax = source.anchorMax;
            _runtimeMnBar.pivot = source.pivot;
            _runtimeMnBar.anchoredPosition = source.anchoredPosition;
            _runtimeMnBar.sizeDelta = source.sizeDelta;
        }

        private static bool HasSameGeometry(RectTransform target, RectTransform source)
        {
            return target.anchorMin == source.anchorMin
                && target.anchorMax == source.anchorMax
                && target.pivot == source.pivot
                && target.anchoredPosition == source.anchoredPosition
                && target.sizeDelta == source.sizeDelta;
        }

        private void SetRuntimeHpFill(
            int currentHp,
            int maxHp,
            int currentShield)
        {
            if (_runtimeHpFill == null)
            {
                return;
            }

            var safeMaxHp = Mathf.Max(0, maxHp);
            var safeHp = Mathf.Clamp(currentHp, 0, safeMaxHp);
            var safeShield = Mathf.Max(0, currentShield);
            var gaugeMaximum = safeMaxHp + safeShield;
            var hpRatio = gaugeMaximum > 0
                ? Mathf.Clamp01((float)safeHp / gaugeMaximum)
                : 0f;
            var healthRatio = safeMaxHp > 0
                ? Mathf.Clamp01((float)safeHp / safeMaxHp)
                : 0f;
            _runtimeHpFill.rectTransform.anchorMin = Vector2.zero;
            _runtimeHpFill.rectTransform.anchorMax = new Vector2(hpRatio, 1f);
            _runtimeHpFill.color = healthRatio <= 0f
                ? EmptyHpColor
                : healthRatio <= 0.25f
                    ? CriticalHpColor
                    : healthRatio <= 0.5f
                        ? WarningHpColor
                        : HealthyHpColor;

            if (_runtimeHpShield == null)
            {
                return;
            }

            var shieldEnd = gaugeMaximum > 0
                ? Mathf.Clamp01((float)(safeHp + safeShield) / gaugeMaximum)
                : hpRatio;
            _runtimeHpShield.enabled = safeShield > 0;
            _runtimeHpShield.rectTransform.anchorMin =
                new Vector2(hpRatio, 0f);
            _runtimeHpShield.rectTransform.anchorMax =
                new Vector2(shieldEnd, 1f);
            _runtimeHpShield.rectTransform.offsetMin = Vector2.zero;
            _runtimeHpShield.rectTransform.offsetMax = Vector2.zero;
        }

        private void SetRuntimeMnFill(float ratio)
        {
            if (_runtimeMnFill == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);
            _runtimeMnFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            _runtimeMnFill.color = ratio <= 0f ? EmptyHpColor : MnColor;
        }

        private static void RebuildChips(
            Transform container,
            TextChipView template,
            IReadOnlyList<string> labels,
            string emptyLabel)
        {
            if (container == null || template == null) return;

            DeactivateChipPool(container, template);

            var values = labels.Count > 0 ? labels : new[] { emptyLabel };
            for (var index = 0; index < values.Count; index++)
            {
                var chip = GetOrCreatePooledChip(container, template, index);
                chip.gameObject.SetActive(true);
                chip.Bind(values[index]);
            }

            template.gameObject.SetActive(false);
            container.GetComponent<ResponsiveGridLayout>()?.RefreshLayout();
        }

        private static void RebuildAbilityChips(
            Transform container,
            TextChipView template,
            IReadOnlyList<PachimonAbilityPreview> abilities,
            Action<PachimonAbilityPreview> onClicked)
        {
            if (container == null || template == null) return;

            DeactivateChipPool(container, template);

            if (abilities.Count == 0)
            {
                var emptyChip = GetOrCreatePooledChip(container, template, 0);
                emptyChip.gameObject.SetActive(true);
                emptyChip.Bind("なし");
            }
            else
            {
                for (var index = 0; index < abilities.Count; index++)
                {
                    var ability = abilities[index];
                    var capturedAbility = ability;
                    var chip = GetOrCreatePooledChip(container, template, index);
                    chip.gameObject.SetActive(true);
                    chip.Bind(
                        ability.DisplayName,
                        () => onClicked?.Invoke(capturedAbility));
                }
            }

            template.gameObject.SetActive(false);
            container.GetComponent<ResponsiveGridLayout>()?.RefreshLayout();
        }


        private static void RebuildStatusChips(
            Transform container,
            TextChipView template,
            IReadOnlyList<PachimonStatusPreview> statuses,
            Action<PachimonStatusPreview> onClicked)
        {
            if (container == null || template == null) return;

            DeactivateChipPool(container, template);

            if (statuses.Count == 0)
            {
                var emptyChip = GetOrCreatePooledChip(container, template, 0);
                emptyChip.gameObject.SetActive(true);
                emptyChip.Bind("なし");
            }
            else
            {
                for (var index = 0; index < statuses.Count; index++)
                {
                    var status = statuses[index];
                    var capturedStatus = status;
                    var chip = GetOrCreatePooledChip(container, template, index);
                    chip.gameObject.SetActive(true);
                    chip.Bind(
                        status.DisplayName,
                        () => onClicked?.Invoke(capturedStatus));
                    chip.SetAttributeColors(
                        AttributeCardPalette.GetStatusColors(status.Instance));
                }
            }

            template.gameObject.SetActive(false);
            container.GetComponent<ResponsiveGridLayout>()?.RefreshLayout();
        }

        private static void DeactivateChipPool(
            Transform container,
            TextChipView template)
        {
            foreach (Transform child in container)
            {
                if (child != template.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static TextChipView GetOrCreatePooledChip(
            Transform container,
            TextChipView template,
            int poolIndex)
        {
            var currentIndex = 0;
            foreach (Transform child in container)
            {
                if (child == template.transform)
                {
                    continue;
                }

                var chip = child.GetComponent<TextChipView>();
                if (chip == null)
                {
                    continue;
                }

                if (currentIndex == poolIndex)
                {
                    return chip;
                }

                currentIndex++;
            }

            var created = Instantiate(template, container, false);
            created.name = $"{template.name}_Pooled_{currentIndex + 1:D2}";
            return created;
        }
    }
}
