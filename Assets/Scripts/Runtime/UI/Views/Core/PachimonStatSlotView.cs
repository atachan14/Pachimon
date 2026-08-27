using System;
using TMPro;
using Pachimon.Reward;
using Pachimon.Run;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class PachimonStatSlotView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerMoveHandler,
        IPointerExitHandler
    {
        [SerializeField] private PachimonDisplayStat _stat;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private GameObject _subStatBadge;
        [SerializeField] private TMP_Text _subStatText;
        [SerializeField] private Image _subStatIcon;
        private bool _labelVisualApplied;
        private int? _boundValue;
        private PachimonDisplayStat? _boundSubStat;
        private int? _boundSubStatValue;
        private PachimonStatTooltipView _tooltip;

        public PachimonDisplayStat Stat => _stat;

        public void Configure(
            PachimonDisplayStat stat,
            TMP_Text valueText,
            GameObject subStatBadge = null,
            TMP_Text subStatText = null,
            Image subStatIcon = null)
        {
            _stat = stat;
            _valueText = valueText;
            _subStatBadge = subStatBadge;
            _subStatText = subStatText;
            _subStatIcon = subStatIcon;
        }

        public void Bind(
            string value,
            PachimonDisplayStat? boundSubStat = null,
            int? boundSubStatValue = null)
        {
            ApplyLabelVisual();
            if (boundSubStat.HasValue)
            {
                EnsureSubStatBadge();
            }
            _boundValue = int.TryParse(value, out var parsedValue)
                ? parsedValue
                : null;
            _boundSubStat = boundSubStat;
            _boundSubStatValue = boundSubStatValue;
            if (_valueText != null) _valueText.text = value;
            if (_subStatBadge != null)
            {
                _subStatBadge.SetActive(boundSubStat.HasValue);
            }
            if (_subStatText != null)
            {
                _subStatText.gameObject.SetActive(false);
            }
            if (_subStatIcon != null)
            {
                _subStatIcon.sprite = boundSubStat.HasValue
                    ? SubStatIconProvider.Get(boundSubStat.Value)
                    : null;
                _subStatIcon.color = Color.white;
            }
        }

        private void EnsureSubStatBadge()
        {
            var iconRoot = transform.Find("Icon") as RectTransform;
            if (iconRoot == null)
            {
                return;
            }

            var iconLayout = iconRoot.GetComponent<LayoutElement>();
            if (iconLayout != null)
            {
                iconLayout.minWidth = 84f;
                iconLayout.preferredWidth = 84f;
                iconLayout.flexibleWidth = 0f;
            }

            var attributeLabel = iconRoot.Find("Label") as RectTransform;
            if (attributeLabel != null)
            {
                attributeLabel.anchorMin = Vector2.zero;
                attributeLabel.anchorMax = new Vector2(0.5f, 1f);
                attributeLabel.offsetMin = Vector2.zero;
                attributeLabel.offsetMax = Vector2.zero;
            }

            var existing = iconRoot.Find("SubStatBadge");
            var badge = existing != null
                ? existing.gameObject
                : new GameObject(
                    "SubStatBadge",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
            if (existing == null)
            {
                badge.transform.SetParent(iconRoot, false);
            }

            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 0.5f);
            badgeRect.pivot = new Vector2(1f, 0.5f);
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = new Vector2(40f, 40f);
            var badgeImage = badge.GetComponent<Image>();
            badgeImage.color = Color.white;
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;

            var labelTransform = badge.transform.Find("Label");
            var label = labelTransform != null
                ? labelTransform.GetComponent<TMP_Text>()
                : null;
            if (label == null)
            {
                var labelObject = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(badge.transform, false);
                var labelRect = (RectTransform)labelObject.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label = labelObject.GetComponent<TextMeshProUGUI>();
                label.font = _valueText != null ? _valueText.font : null;
                label.fontSize = 8f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }
            label.gameObject.SetActive(false);
            _subStatBadge = badge;
            _subStatText = label;
            _subStatIcon = badgeImage;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_boundValue.HasValue)
            {
                return;
            }

            _tooltip = PachimonStatTooltipView.GetOrCreate(this);
            _tooltip?.Show(
                this,
                CreateDescription(
                    _stat,
                    _boundValue.Value,
                    _boundSubStat,
                    _boundSubStatValue),
                eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            _tooltip?.Move(this, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _tooltip?.Hide(this);
        }

        private void OnDisable()
        {
            _tooltip?.Hide(this);
        }

        private void ApplyLabelVisual()
        {
            if (_labelVisualApplied)
            {
                return;
            }

            _labelVisualApplied = true;
            if (!AttributeRichText.IsAttribute(_stat))
            {
                ApplyNonAttributeColor();
                return;
            }

            var iconRoot = transform.Find("Icon");
            var label = iconRoot?.Find("Label")?.GetComponent<TMP_Text>();
            if (label == null)
            {
                return;
            }

            label.richText = true;
            var iconFontSize = AttributeRichText.StatLabelIconFontSize;
            label.fontSize = iconFontSize;
            label.GetComponent<ResponsiveTypographySize>()?.SetBaseFontSize(
                label,
                iconFontSize);
            label.text = AttributeRichText.GetIcon(_stat);
            var background = iconRoot.GetComponent<Image>();
            if (background != null)
            {
                background.color = GameUiPalette.Transparent;
            }
        }

        private void ApplyNonAttributeColor()
        {
            var iconRoot = transform.Find("Icon");
            var background = iconRoot?.GetComponent<Image>();
            var label = iconRoot?.Find("Label")?.GetComponent<TMP_Text>();
            var color = _stat switch
            {
                PachimonDisplayStat.DamageBonus
                    or PachimonDisplayStat.GenerationPower
                    or PachimonDisplayStat.Haste
                    or PachimonDisplayStat.Speed
                    or PachimonDisplayStat.ResistBonus
                    or PachimonDisplayStat.SustainPower
                    or PachimonDisplayStat.StatusMastery
                    or PachimonDisplayStat.StatusResistance =>
                    RewardElementPalette.TimingColor,
                _ => GameUiPalette.StatCard,
            };
            if (background != null)
            {
                background.color = color;
            }
            if (label != null)
            {
                label.color = AttributeCardPalette.GetReadableTextColor(
                    new[] { color });
            }
        }

        private static string CreateDescription(
            PachimonDisplayStat stat,
            int value,
            PachimonDisplayStat? boundSubStat,
            int? boundSubStatValue)
        {
            if (AttributeRichText.IsAttribute(stat))
            {
                return CreateAttributeDescription(
                    stat,
                    value,
                    boundSubStat,
                    boundSubStatValue);
            }

            return stat switch
            {
                PachimonDisplayStat.DamageBonus =>
                    CreateAmplificationDescription("与えるダメージ", value),
                PachimonDisplayStat.ResistBonus =>
                    CreateReductionDescription("受けるダメージ", value),
                PachimonDisplayStat.Speed =>
                    CreateTimingDescription("Skillの発生・硬直", value),
                PachimonDisplayStat.Haste =>
                    CreateTimingDescription("SkillのCD", value),
                PachimonDisplayStat.GenerationPower =>
                    CreateAmplificationDescription("生成物・天候の生成Value", value),
                PachimonDisplayStat.StatusMastery =>
                    CreateAmplificationDescription("与える状態Value", value),
                PachimonDisplayStat.SustainPower =>
                    CreateAmplificationDescription("HP回復量・シールド量", value),
                PachimonDisplayStat.StatusResistance =>
                    CreateReductionDescription("受ける状態Value", value),
                _ => string.Empty,
            };
        }

        private static string CreateAttributeDescription(
            PachimonDisplayStat stat,
            int value,
            PachimonDisplayStat? boundSubStat,
            int? boundSubStatValue)
        {
            var icon = AttributeRichText.GetIcon(stat);
            var reduction = (1m - SignedStatMath.ReductionMultiplier(value))
                * 100m;
            var amplification = (SignedStatMath.AmplificationMultiplier(value) - 1m)
                * 100m;
            var derivedValue = boundSubStatValue ?? value;
            var receivedLine = reduction >= 0m
                ? $"受ける{icon}ダメージを{FormatPercent(reduction)}%軽減する。"
                : $"受ける{icon}ダメージが{FormatPercent(-reduction)}%増加する。";
            var outgoingLine = amplification >= 0m
                ? $"与える{icon}ダメージを{FormatPercent(amplification)}%増加する。"
                : $"与える{icon}ダメージが{FormatPercent(-amplification)}%減少する。";
            var subStatLine = boundSubStat.HasValue
                ? CreateSubStatEffectDescription(boundSubStat.Value, derivedValue)
                : string.Empty;
            return $"{receivedLine}\n{outgoingLine}\n{subStatLine}";
        }

        private static string CreateSubStatEffectDescription(
            PachimonDisplayStat stat,
            int value)
        {
            return stat switch
            {
                PachimonDisplayStat.DamageBonus =>
                    CreateAmplificationDescription("与える全ダメージ", value),
                PachimonDisplayStat.ResistBonus =>
                    CreateReductionDescription("受ける全ダメージ", value),
                PachimonDisplayStat.Speed =>
                    CreateTimingDescription("Skillの発生・硬直", value),
                PachimonDisplayStat.Haste =>
                    CreateTimingDescription("SkillのCD", value),
                PachimonDisplayStat.GenerationPower =>
                    CreateAmplificationDescription("生成物・天候の生成Value", value),
                PachimonDisplayStat.StatusMastery =>
                    CreateAmplificationDescription("与える状態Value", value),
                PachimonDisplayStat.SustainPower =>
                    CreateAmplificationDescription("HP回復量・シールド量", value),
                PachimonDisplayStat.StatusResistance =>
                    CreateReductionDescription("受ける状態Value", value),
                _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null),
            };
        }

        private static string CreateReductionDescription(
            string subject,
            int value)
        {
            var reduction = (1m - SignedStatMath.ReductionMultiplier(value))
                * 100m;
            return reduction >= 0m
                ? $"{subject}の{FormatPercent(reduction)}%を軽減する。"
                : $"{subject}が{FormatPercent(-reduction)}%増加する。";
        }

        private static string CreateAmplificationDescription(
            string subject,
            int value)
        {
            var increase = (SignedStatMath.AmplificationMultiplier(value) - 1m)
                * 100m;
            return increase >= 0m
                ? $"{subject}を{FormatPercent(increase)}%増加する。"
                : $"{subject}が{FormatPercent(-increase)}%減少する。";
        }

        private static string CreateTimingDescription(
            string subject,
            int value)
        {
            var reduction = (1m - SignedStatMath.ReductionMultiplier(value))
                * 100m;
            return reduction >= 0m
                ? $"{subject}を{FormatPercent(reduction)}%短縮する。"
                : $"{subject}が{FormatPercent(-reduction)}%延長する。";
        }

        private static string FormatPercent(decimal value)
        {
            return value.ToString("0.##");
        }
    }
}
