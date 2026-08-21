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
        private bool _labelVisualApplied;
        private int? _boundValue;
        private PachimonStatTooltipView _tooltip;

        public PachimonDisplayStat Stat => _stat;

        public void Configure(PachimonDisplayStat stat, TMP_Text valueText)
        {
            _stat = stat;
            _valueText = valueText;
        }

        public void Bind(string value)
        {
            ApplyLabelVisual();
            _boundValue = int.TryParse(value, out var parsedValue)
                ? parsedValue
                : null;
            if (_valueText != null) _valueText.text = value;
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
                CreateDescription(_stat, _boundValue.Value),
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
                PachimonDisplayStat.Speed or PachimonDisplayStat.Haste =>
                    RewardElementPalette.TimingColor,
                PachimonDisplayStat.DamageBonus
                    or PachimonDisplayStat.ResistBonus =>
                    RewardElementPalette.CombatBonusColor,
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
            int value)
        {
            if (AttributeRichText.IsAttribute(stat))
            {
                return CreateReductionDescription(
                    $"{AttributeRichText.GetIcon(stat)}ダメージ",
                    value);
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
                _ => string.Empty,
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
