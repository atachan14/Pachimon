using System;

namespace Pachimon.Run
{
    public sealed class PachimonStats
    {
        private readonly int[] _valueUnits;

        public PachimonStats(
            int[] valueUnits,
            int resourceDisplayMultiplier,
            int specialStatDivisor)
        {
            if (valueUnits == null) throw new ArgumentNullException(nameof(valueUnits));
            if (valueUnits.Length != (int)PachimonStatType.Count)
            {
                throw new ArgumentException("Unexpected Pachimon stat count.", nameof(valueUnits));
            }

            if (resourceDisplayMultiplier < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(resourceDisplayMultiplier));
            }

            if (specialStatDivisor < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(specialStatDivisor));
            }

            _valueUnits = (int[])valueUnits.Clone();
            ResourceDisplayMultiplier = resourceDisplayMultiplier;
            SpecialStatDivisor = specialStatDivisor;
        }

        public int ResourceDisplayMultiplier { get; }
        public int SpecialStatDivisor { get; }
        public int MaxHp => GetDisplayedValue(PachimonStatType.MaxHp);
        public int MaxMn => GetDisplayedValue(PachimonStatType.MaxMn);

        public int GetDisplayedValue(PachimonStatType statType)
        {
            var valueUnits = GetValueUnits(statType);
            if (PachimonStatTypeUtility.IsResource(statType))
            {
                return checked(valueUnits * ResourceDisplayMultiplier);
            }

            return PachimonStatTypeUtility.IsSpecialScaledStat(statType)
                ? valueUnits / SpecialStatDivisor
                : valueUnits;
        }

        public int GetValueUnits(PachimonStatType statType)
        {
            var index = (int)statType;
            if (index < 0 || index >= _valueUnits.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(statType));
            }

            return _valueUnits[index];
        }

        public int GetTotalValueUnits()
        {
            var total = 0;
            foreach (var value in _valueUnits) total += value;
            return total;
        }
    }
}
