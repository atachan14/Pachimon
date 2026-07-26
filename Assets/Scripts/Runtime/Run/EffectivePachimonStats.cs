using System;
using Pachimon.Reward;

namespace Pachimon.Run
{
    public sealed class EffectivePachimonStats
    {
        private readonly int[] _displayedValues = new int[(int)PachimonStatType.Count];

        public EffectivePachimonStats(PachimonStats baseStats, TrainerModifierSet modifiers)
        {
            if (baseStats == null)
            {
                throw new ArgumentNullException(nameof(baseStats));
            }

            for (var index = 0; index < _displayedValues.Length; index++)
            {
                var statType = (PachimonStatType)index;
                var valueAfterFlatModifiers = AddAndClampToNonNegative(
                    baseStats.GetDisplayedValue(statType),
                    modifiers?.GetStatAddition(statType) ?? 0);
                _displayedValues[index] = TryGetAttribute(statType, out var attribute)
                    ? ApplyPercentage(
                        valueAfterFlatModifiers,
                        modifiers?.GetAttributeMultiplierPercent(attribute) ?? 100)
                    : valueAfterFlatModifiers;
            }

        }

        public int MaxHp => GetValue(PachimonStatType.MaxHp);
        public int MaxMn => GetValue(PachimonStatType.MaxMn);
        public int DamageBonus => GetValue(PachimonStatType.DamageBonus);
        public int ResistBonus => GetValue(PachimonStatType.ResistBonus);

        public int GetValue(PachimonStatType statType)
        {
            var index = (int)statType;
            if (index < 0 || index >= _displayedValues.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(statType));
            }

            return _displayedValues[index];
        }

        private static int AddAndClampToNonNegative(int baseValue, int addition)
        {
            var result = (long)baseValue + addition;
            if (result <= 0)
            {
                return 0;
            }

            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        private static int ApplyPercentage(int value, int percentage)
        {
            var result = (long)value * percentage / 100;
            return result >= int.MaxValue ? int.MaxValue : (int)result;
        }

        private static bool TryGetAttribute(
            PachimonStatType statType,
            out PachimonAttribute attribute)
        {
            return PachimonStatTypeUtility.TryGetAttribute(statType, out attribute);
        }
    }
}
