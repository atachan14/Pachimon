using Pachimon.Reward;

namespace Pachimon.Battle
{
    public static class BattleDamageLogFormatter
    {
        private const string SpriteAssetName = "AttributeIcons";

        public static string FormatDamage(
            string targetName,
            int damage,
            PachimonAttribute? attribute,
            bool isTrueDamage)
        {
            if (isTrueDamage || !attribute.HasValue)
            {
                return $"{targetName}\u306b{damage}\u306e\u78ba\u5b9a\u30c0\u30e1\u30fc\u30b8\uff01";
            }

            return $"{targetName}\u306b{FormatValue(damage, attribute.Value)}\u306e\u30c0\u30e1\u30fc\u30b8\uff01";
        }

        public static string FormatShieldAbsorption(
            string targetName,
            int damage,
            PachimonAttribute? attribute,
            bool isTrueDamage)
        {
            var displayedDamage = isTrueDamage || !attribute.HasValue
                ? damage.ToString()
                : FormatValue(damage, attribute.Value);
            var subject = string.IsNullOrWhiteSpace(targetName)
                ? "Shield"
                : $"{targetName}\u306eShield";
            return $"{subject}\u304c{displayedDamage}\u306e\u30c0\u30e1\u30fc\u30b8\u3092\u5438\u53ce\u3057\u305f\uff01";
        }

        public static string FormatValue(int value, PachimonAttribute attribute)
        {
            var icon = $"<sprite=\"{SpriteAssetName}\" name=\"{attribute}\">";
            var color = RewardElementPalette.GetAttributeColorHex(attribute);
            return $"{icon}<color={color}>{value}</color>";
        }
    }
}