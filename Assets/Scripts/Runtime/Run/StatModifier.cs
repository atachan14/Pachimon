using System;

namespace Pachimon.Run
{
    public enum StatModifierOperation
    {
        Base = 0,
        DirectAdditive = 1,
        DerivedAdditive = 2,
        DirectMultiplicative = 3,
        DerivedMultiplicative = 4,
    }

    public enum StatModifierSourceType
    {
        Base = 0,
        TrainerMod = 1,
        Badge = 2,
        Passive = 3,
        Item = 4,
        Skill = 5,
        StatusEffect = 6,
        FieldEffect = 7,
    }

    public sealed class StatModifierSource
    {
        public StatModifierSource(
            StatModifierSourceType sourceType,
            string sourceId,
            string displayName)
        {
            SourceType = sourceType;
            SourceId = sourceId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }

        public StatModifierSourceType SourceType { get; }
        public string SourceId { get; }
        public string DisplayName { get; }
    }

    public sealed class StatValueSnapshot
    {
        private readonly decimal[] _values;

        internal StatValueSnapshot(decimal[] values)
        {
            _values = values != null
                ? (decimal[])values.Clone()
                : throw new ArgumentNullException(nameof(values));
        }

        public decimal GetValue(PachimonStatType statType)
        {
            return _values[GetStatIndex(statType)];
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

    public interface IStatModifier
    {
        PachimonStatType TargetStat { get; }
        StatModifierOperation Operation { get; }
        StatModifierSource Source { get; }
        decimal Evaluate(StatValueSnapshot referenceStats);
    }

    public sealed class FixedStatModifier : IStatModifier
    {
        private readonly decimal _value;

        public FixedStatModifier(
            PachimonStatType targetStat,
            StatModifierOperation operation,
            decimal value,
            StatModifierSource source)
        {
            if (operation is not (
                    StatModifierOperation.DirectAdditive
                    or StatModifierOperation.DirectMultiplicative))
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }

            TargetStat = targetStat;
            Operation = operation;
            _value = value;
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public PachimonStatType TargetStat { get; }
        public StatModifierOperation Operation { get; }
        public StatModifierSource Source { get; }

        public decimal Evaluate(StatValueSnapshot referenceStats)
        {
            if (referenceStats == null)
            {
                throw new ArgumentNullException(nameof(referenceStats));
            }

            return _value;
        }
    }

    public sealed class DerivedStatModifier : IStatModifier
    {
        private readonly Func<StatValueSnapshot, decimal> _calculate;

        public DerivedStatModifier(
            PachimonStatType targetStat,
            StatModifierOperation operation,
            Func<StatValueSnapshot, decimal> calculate,
            StatModifierSource source)
        {
            if (operation is not (
                    StatModifierOperation.DerivedAdditive
                    or StatModifierOperation.DerivedMultiplicative))
            {
                throw new ArgumentOutOfRangeException(nameof(operation));
            }

            TargetStat = targetStat;
            Operation = operation;
            _calculate = calculate
                ?? throw new ArgumentNullException(nameof(calculate));
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public PachimonStatType TargetStat { get; }
        public StatModifierOperation Operation { get; }
        public StatModifierSource Source { get; }

        public decimal Evaluate(StatValueSnapshot referenceStats)
        {
            return _calculate(referenceStats
                ?? throw new ArgumentNullException(nameof(referenceStats)));
        }
    }
}
