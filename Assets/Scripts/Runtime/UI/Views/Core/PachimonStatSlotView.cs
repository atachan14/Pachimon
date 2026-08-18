using TMPro;
using Pachimon.Reward;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class PachimonStatSlotView : MonoBehaviour
    {
        [SerializeField] private PachimonDisplayStat _stat;
        [SerializeField] private TMP_Text _valueText;
        private bool _labelVisualApplied;

        public PachimonDisplayStat Stat => _stat;

        public void Configure(PachimonDisplayStat stat, TMP_Text valueText)
        {
            _stat = stat;
            _valueText = valueText;
        }

        public void Bind(string value)
        {
            ApplyLabelVisual();
            if (_valueText != null) _valueText.text = value;
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
    }
}
