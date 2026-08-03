using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Run
{
    public sealed class StatContribution
    {
        public StatContribution(
            PachimonStatType statType,
            StatModifierOperation operation,
            decimal value,
            StatModifierSource source)
        {
            StatType = statType;
            Operation = operation;
            Value = value;
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public PachimonStatType StatType { get; }
        public StatModifierOperation Operation { get; }
        public decimal Value { get; }
        public StatModifierSource Source { get; }
    }

    public sealed class StatCalculationResult
    {
        private readonly int[] _finalValues;
        private readonly decimal[] _unroundedValues;
        private readonly StatContribution[] _contributions;

        internal StatCalculationResult(
            int[] finalValues,
            decimal[] unroundedValues,
            IEnumerable<StatContribution> contributions)
        {
            _finalValues = finalValues != null
                ? (int[])finalValues.Clone()
                : throw new ArgumentNullException(nameof(finalValues));
            _unroundedValues = unroundedValues != null
                ? (decimal[])unroundedValues.Clone()
                : throw new ArgumentNullException(nameof(unroundedValues));
            _contributions = contributions?.ToArray()
                ?? throw new ArgumentNullException(nameof(contributions));
        }

        public IReadOnlyList<StatContribution> Contributions => _contributions;

        public int GetValue(PachimonStatType statType)
        {
            return _finalValues[GetStatIndex(statType)];
        }

        public decimal GetUnroundedValue(PachimonStatType statType)
        {
            return _unroundedValues[GetStatIndex(statType)];
        }

        public IReadOnlyList<StatContribution> GetContributions(
            PachimonStatType statType)
        {
            GetStatIndex(statType);
            return _contributions
                .Where(contribution => contribution.StatType == statType)
                .ToArray();
        }

        private static int GetStatIndex(PachimonStatType statType)
        {
            var index = (int)statType;
            if (index < 0 || index >= (int)PachimonStatType.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(statType));
            }

            return index;
        }
    }
}
