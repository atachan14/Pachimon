using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Pachimon.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public enum PachimonDisplayStat
    {
        Fire, Aqua, Leaf, Electric,
        Poison, Ice, Wind, Dragon,
        Speed, Haste, DamageBonus, ResistBonus,
    }

    public readonly struct PachimonStatPreview
    {
        public PachimonStatPreview(PachimonDisplayStat stat, int value)
        {
            Stat = stat;
            Value = value;
        }

        public PachimonDisplayStat Stat { get; }
        public int Value { get; }
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
            string displayName)
        {
            Kind = kind;
            Id = id;
            DisplayName = displayName ?? string.Empty;
        }

        public PachimonAbilityKind Kind { get; }
        public int Id { get; }
        public string DisplayName { get; }
    }

    public sealed class PachimonPreviewContent
    {
        private readonly Dictionary<PachimonDisplayStat, int> _stats;

        public PachimonPreviewContent(
            Sprite frontSprite,
            string displayName,
            int currentHp,
            int maxHp,
            int currentMn,
            int maxMn,
            IEnumerable<PachimonStatPreview> stats,
            IEnumerable<string> statusEffects,
            IEnumerable<PachimonAbilityPreview> skills,
            IEnumerable<PachimonAbilityPreview> passives,
            StatCalculationResult statCalculation = null)
        {
            IsRevealed = true;
            FrontSprite = frontSprite;
            DisplayName = displayName;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            CurrentMn = currentMn;
            MaxMn = maxMn;
            _stats = stats?.ToDictionary(item => item.Stat, item => item.Value) ?? new();
            StatusEffects = statusEffects?.ToArray() ?? Array.Empty<string>();
            Skills = skills?.ToArray() ?? Array.Empty<PachimonAbilityPreview>();
            Passives = passives?.ToArray() ?? Array.Empty<PachimonAbilityPreview>();
            StatCalculation = statCalculation;
        }

        private PachimonPreviewContent()
        {
            _stats = new Dictionary<PachimonDisplayStat, int>();
            StatusEffects = Array.Empty<string>();
            Skills = Array.Empty<PachimonAbilityPreview>();
            Passives = Array.Empty<PachimonAbilityPreview>();
        }

        public bool IsRevealed { get; }
        public Sprite FrontSprite { get; }
        public string DisplayName { get; }
        public int CurrentHp { get; }
        public int MaxHp { get; }
        public int CurrentMn { get; }
        public int MaxMn { get; }
        public IReadOnlyList<string> StatusEffects { get; }
        public IReadOnlyList<PachimonAbilityPreview> Skills { get; }
        public IReadOnlyList<PachimonAbilityPreview> Passives { get; }
        public StatCalculationResult StatCalculation { get; }
        public static PachimonPreviewContent Hidden { get; } = new();

        public bool TryGetStat(PachimonDisplayStat stat, out int value)
        {
            return _stats.TryGetValue(stat, out value);
        }
    }

    public sealed class PachimonTabView : MonoBehaviour
    {
        public const int SkillSlotCount = 9;

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
        private RectTransform _runtimeHpBar;
        private Image _runtimeHpFill;
        private RectTransform _runtimeMnBar;
        private Image _runtimeMnFill;
        private PachimonPreviewContent _boundPreview = PachimonPreviewContent.Hidden;
        public RectTransform GraphicRect => _frontGraphic?.rectTransform;
        public event Action<PachimonAbilityPreview, PachimonPreviewContent>
            AbilityDetailsRequested;

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
            TextChipView passiveTemplate)
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
        }

        public void Bind(PachimonPreviewContent preview)
        {
            preview ??= PachimonPreviewContent.Hidden;
            _boundPreview = preview;
            var revealed = preview.IsRevealed;

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
                var hpRatio = revealed && preview.MaxHp > 0
                    ? Mathf.Clamp01((float)preview.CurrentHp / preview.MaxHp)
                    : 0f;
                SetRuntimeHpFill(hpRatio);
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
                slot.Bind(revealed && preview.TryGetStat(slot.Stat, out var value)
                    ? value.ToString()
                    : "?");
            }

            RebuildChips(
                _statusContainer,
                _statusTemplate,
                revealed ? preview.StatusEffects : new[] { "?" },
                "なし");

            for (var index = 0; index < _skillSlots.Length; index++)
            {
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
        }

        private void RequestAbilityDetails(PachimonAbilityPreview ability)
        {
            AbilityDetailsRequested?.Invoke(ability, _boundPreview);
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

            SyncRuntimeHpBarTransform();
        }

        private void SyncRuntimeHpBarTransform()
        {
            if (_runtimeHpBar == null || _hpText == null)
            {
                return;
            }

            var source = _hpText.rectTransform;
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
            _runtimeMnBar.anchorMin = source.anchorMin;
            _runtimeMnBar.anchorMax = source.anchorMax;
            _runtimeMnBar.pivot = source.pivot;
            _runtimeMnBar.anchoredPosition = source.anchoredPosition;
            _runtimeMnBar.sizeDelta = source.sizeDelta;
        }

        private void SetRuntimeHpFill(float ratio)
        {
            if (_runtimeHpFill == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);
            _runtimeHpFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            _runtimeHpFill.color = ratio <= 0f
                ? EmptyHpColor
                : ratio <= 0.25f
                    ? CriticalHpColor
                    : ratio <= 0.5f
                        ? WarningHpColor
                        : HealthyHpColor;
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

            for (var index = container.childCount - 1; index >= 0; index--)
            {
                var child = container.GetChild(index);
                if (child != template.transform) Destroy(child.gameObject);
            }

            var values = labels.Count > 0 ? labels : new[] { emptyLabel };
            foreach (var label in values)
            {
                var chip = Instantiate(template, container, false);
                chip.gameObject.SetActive(true);
                chip.Bind(label);
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

            for (var index = container.childCount - 1; index >= 0; index--)
            {
                var child = container.GetChild(index);
                if (child != template.transform) Destroy(child.gameObject);
            }

            if (abilities.Count == 0)
            {
                var emptyChip = Instantiate(template, container, false);
                emptyChip.gameObject.SetActive(true);
                emptyChip.Bind("なし");
            }
            else
            {
                foreach (var ability in abilities)
                {
                    var capturedAbility = ability;
                    var chip = Instantiate(template, container, false);
                    chip.gameObject.SetActive(true);
                    chip.Bind(
                        ability.DisplayName,
                        () => onClicked?.Invoke(capturedAbility));
                }
            }

            template.gameObject.SetActive(false);
            container.GetComponent<ResponsiveGridLayout>()?.RefreshLayout();
        }
    }
}
