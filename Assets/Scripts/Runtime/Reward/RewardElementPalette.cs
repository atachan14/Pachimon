using System;
using UnityEngine;

namespace Pachimon.Reward
{
    public static class RewardElementPalette
    {
        public static Color GetColor(RewardElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Attribute is PachimonAttribute attribute)
            {
                return GetAttributeColor(attribute);
            }

            return element.Kind switch
            {
                RewardElementKind.Speed => FromRgb(0x8E, 0x63, 0xCE),
                RewardElementKind.MaxHp => FromRgb(0xF4, 0xF4, 0xEF),
                RewardElementKind.MaxMn => FromRgb(0x5E, 0xC4, 0xD6),
                RewardElementKind.BonusGold => FromRgb(0xE5, 0x9A, 0x23),
                RewardElementKind.DamageBonus => FromRgb(0x25, 0x2A, 0x30),
                RewardElementKind.ResistBonus => FromRgb(0xA7, 0xB5, 0xC0),
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
                PachimonAttribute.Poison => FromRgb(0xFF, 0xA7, 0xDF),
                PachimonAttribute.Wind => FromRgb(0x91, 0xC8, 0x3E),
                PachimonAttribute.Ice => FromRgb(0x62, 0xD5, 0xE6),
                PachimonAttribute.Dragon => FromRgb(0x70, 0x78, 0x87),
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
