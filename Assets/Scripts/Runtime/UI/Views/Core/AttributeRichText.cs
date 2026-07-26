using System;
using Pachimon.Data;
using Pachimon.Reward;
using Pachimon.Skills;

namespace Pachimon.UI
{
    public static class AttributeRichText
    {
        private const string SpriteAssetName = "AttributeIcons";
        public const float StatLabelIconFontSize = 40f;

        public static string GetIcon(AllocationType type)
        {
            return TryGetAttribute(type, out var attribute)
                ? $"<sprite=\"{SpriteAssetName}\" name=\"{attribute}\">"
                : string.Empty;
        }

        public static string GetIcon(PachimonDisplayStat stat)
        {
            return TryGetAllocationType(stat, out var type)
                ? GetIcon(type)
                : string.Empty;
        }

        public static bool IsAttribute(PachimonDisplayStat stat)
        {
            return TryGetAllocationType(stat, out _);
        }

        public static string Colorize(AllocationType type, object value)
        {
            if (!TryGetAttribute(type, out var attribute))
            {
                return value?.ToString() ?? string.Empty;
            }

            return $"<color={RewardElementPalette.GetAttributeColorHex(attribute)}>"
                + $"{value}</color>";
        }

        public static bool TryGetDisplayStat(
            AllocationType type,
            out PachimonDisplayStat displayStat)
        {
            displayStat = type switch
            {
                AllocationType.Fire => PachimonDisplayStat.Fire,
                AllocationType.Aqua => PachimonDisplayStat.Aqua,
                AllocationType.Leaf => PachimonDisplayStat.Leaf,
                AllocationType.Electric => PachimonDisplayStat.Electric,
                AllocationType.Poison => PachimonDisplayStat.Poison,
                AllocationType.Ice => PachimonDisplayStat.Ice,
                AllocationType.Wind => PachimonDisplayStat.Wind,
                AllocationType.Dragon => PachimonDisplayStat.Dragon,
                _ => default,
            };
            return type != AllocationType.Unassigned;
        }

        private static bool TryGetAttribute(
            AllocationType type,
            out PachimonAttribute attribute)
        {
            if (type < AllocationType.Fire || type > AllocationType.Dragon)
            {
                attribute = default;
                return false;
            }

            attribute = (PachimonAttribute)((int)type - 1);
            return true;
        }

        private static bool TryGetAllocationType(
            PachimonDisplayStat stat,
            out AllocationType type)
        {
            type = stat switch
            {
                PachimonDisplayStat.Fire => AllocationType.Fire,
                PachimonDisplayStat.Aqua => AllocationType.Aqua,
                PachimonDisplayStat.Leaf => AllocationType.Leaf,
                PachimonDisplayStat.Electric => AllocationType.Electric,
                PachimonDisplayStat.Poison => AllocationType.Poison,
                PachimonDisplayStat.Ice => AllocationType.Ice,
                PachimonDisplayStat.Wind => AllocationType.Wind,
                PachimonDisplayStat.Dragon => AllocationType.Dragon,
                _ => AllocationType.Unassigned,
            };
            return type != AllocationType.Unassigned;
        }
    }

    public static class SkillDetailDescriptionFormatter
    {
        public static string Format(
            SkillAsset skill,
            PachimonPreviewContent owner)
        {
            if (skill == null)
            {
                return "説明未設定";
            }

            if (owner?.IsRevealed == true
                && AttributeRichText.TryGetDisplayStat(
                    skill.AllocationType,
                    out var displayStat)
                && owner.TryGetStat(displayStat, out var attributeValue))
            {
                const int baseDamage = 100;
                var displayedDamage = (int)Math.Min(
                    int.MaxValue,
                    ((long)baseDamage * (100L + attributeValue)) / 100L);
                var icon = AttributeRichText.GetIcon(skill.AllocationType);
                return $"敵の先頭に{AttributeRichText.Colorize(skill.AllocationType, displayedDamage)}"
                    + $"（{baseDamage} + {icon}"
                    + $"{AttributeRichText.Colorize(skill.AllocationType, attributeValue)}"
                    + $" × {baseDamage}%）の{icon}ダメージを与える。";
            }

            return string.IsNullOrWhiteSpace(skill.Description)
                ? "説明未設定"
                : skill.Description;
        }
    }
}
