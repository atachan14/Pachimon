using UnityEngine;

namespace Pachimon.UI
{
    public static class GameUiPalette
    {
        public static Color HeaderBackground => FromRgb(0xFA, 0xF3, 0xB5);
        public static Color HeaderText => PrimaryText;
        public static Color LeftPaneBackground => FromRgb(0xC6, 0xFA, 0xBE);
        public static Color MainPaneBackground => Color.white;
        public static Color RightPaneBackground => FromRgb(0xF6, 0xC5, 0xC0);
        public static Color Card => FromRgba(0xFF, 0xFF, 0xFF, 0xCC);
        public static Color StatCard => FromRgba(0xFF, 0xFF, 0xFF, 0xD9);
        public static Color StatusSection => FromRgba(0xFF, 0xFF, 0xFF, 0xD9);
        public static Color StatusChip => FromRgb(0xDD, 0xE6, 0xE9);
        public static Color SkillSection => FromRgb(0xE3, 0xF1, 0xF5);
        public static Color SkillChip => FromRgb(0x2F, 0x75, 0x85);
        public static Color PassiveSection => FromRgb(0xF4, 0xE9, 0xD8);
        public static Color PassiveChip => FromRgb(0x9A, 0x6A, 0x2D);
        public static Color GoldCard => FromRgb(0xFF, 0xF0, 0xC2);
        public static Color PrimaryText => FromRgb(0x26, 0x32, 0x38);
        public static Color SecondaryText => FromRgb(0x66, 0x72, 0x77);
        public static Color Border => FromRgb(0xBC, 0xC8, 0xCC);
        public static Color ButtonAccent => FromRgb(0x3D, 0x7F, 0x6B);
        public static Color ButtonNeutral => FromRgb(0x6E, 0x7D, 0x82);
        public static Color OnAccentText => Color.white;
        public static Color MissingGraphic => FromRgb(0xD8, 0xE1, 0xE4);
        public static Color Transparent => new(1f, 1f, 1f, 0f);

        private static Color FromRgb(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, byte.MaxValue);
        }

        private static Color FromRgba(byte red, byte green, byte blue, byte alpha)
        {
            return new Color32(red, green, blue, alpha);
        }
    }
}
