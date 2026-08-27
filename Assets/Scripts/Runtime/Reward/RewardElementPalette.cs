using System;
using UnityEngine;

namespace Pachimon.Reward
{
    public static class RewardElementPalette
    {
        public static Color ResourceColor => FromRgb(0xF4, 0xF1, 0xE8);
        public static Color TimingColor => FromRgb(0xF0, 0x7F, 0xA5);
        public static Color CombatBonusColor => FromRgb(0x18, 0x1B, 0x1F);
        public static Color GoldColor => FromRgb(0xFF, 0x8A, 0x00);

        public static Color GetColor(RewardElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Attribute is PachimonAttribute attribute)
            {
                return GetAttributeColor(attribute);
            }

            return element.Kind switch
            {
                RewardElementKind.Speed => TimingColor,
                RewardElementKind.MaxHp => ResourceColor,
                RewardElementKind.MaxMn => ResourceColor,
                RewardElementKind.BonusGold => GoldColor,
                RewardElementKind.DamageBonus => TimingColor,
                RewardElementKind.ResistBonus => TimingColor,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static Color GetAttributeColor(PachimonAttribute attribute)
        {
            return attribute switch
            {
                PachimonAttribute.Fire => FromRgb(0xE8, 0x4B, 0x3C),
                PachimonAttribute.Aqua => FromRgb(0x35, 0x6A, 0xE0),
                PachimonAttribute.Leaf => FromRgb(0x28, 0x8A, 0x47),
                PachimonAttribute.Electric => FromRgb(0xF2, 0xC9, 0x4C),
                PachimonAttribute.Poison => FromRgb(0x9B, 0x59, 0xB6),
                PachimonAttribute.Wind => FromRgb(0x91, 0xC8, 0x3E),
                PachimonAttribute.Ice => FromRgb(0x62, 0xD5, 0xE6),
                PachimonAttribute.Dragon => FromRgb(0x8B, 0x5A, 0x3C),
                _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null),
            };
        }

        public static string GetColorHex(RewardElement element)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(GetColor(element))}";
        }

        public static string GetAttributeColorHex(PachimonAttribute attribute)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(GetAttributeColor(attribute))}";
        }

        private static Color FromRgb(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, byte.MaxValue);
        }
    }
}
