using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Run
{
    public sealed class StatCalculator
    {
        public StatCalculationResult Calculate(
            PachimonStats baseStats,
            IEnumerable<IStatModifier> modifiers = null)
        {
            if (baseStats == null)
            {
                throw new ArgumentNullException(nameof(baseStats));
            }

            var modifierArray = modifiers?.ToArray() ?? Array.Empty<IStatModifier>();
            if (modifierArray.Any(modifier => modifier == null))
            {
                throw new ArgumentException(
                    "Stat modifiers cannot contain null.",
                    nameof(modifiers));
            }

            var contributions = new List<StatContribution>();
            var baseValues = CreateBaseValues(baseStats, contributions);
            var baseSnapshot = new StatValueSnapshot(baseValues);

            var directAdditiveValues = (decimal[])baseValues.Clone();
            ApplyAdditiveModifiers(
                directAdditiveValues,
                modifierArray,
                StatModifierOperation.DirectAdditive,
                baseSnapshot,
                contributions);

            var directAdditiveSnapshot = new StatValueSnapshot(directAdditiveValues);
            var additiveValues = (decimal[])directAdditiveValues.Clone();
            ApplyAdditiveModifiers(
                additiveValues,
                modifierArray,
                StatModifierOperation.DerivedAdditive,
                directAdditiveSnapshot,
                contributions);

            var additiveSnapshot = new StatValueSnapshot(additiveValues);
            var finalValues = (decimal[])additiveValues.Clone();
            ApplyMultiplicativeModifiers(
                finalValues,
                modifierArray,
                StatModifierOperation.DirectMultiplicative,
                baseSnapshot,
                contributions);
            ApplyMultiplicativeModifiers(
                finalValues,
                modifierArray,
                StatModifierOperation.DerivedMultiplicative,
                additiveSnapshot,
                contributions);

            var finalizedValues = new int[(int)PachimonStatType.Count];
            for (var index = 0; index < finalizedValues.Length; index++)
            {
                var statType = (PachimonStatType)index;
                finalizedValues[index] = SignedStatMath.FloorStat(
                    finalValues[index],
                    PachimonStatTypeUtility.IsResource(statType));
            }

            return new StatCalculationResult(
                finalizedValues,
                finalValues,
                contributions);
        }

        private static decimal[] CreateBaseValues(
            PachimonStats baseStats,
            ICollection<StatContribution> contributions)
        {
            var values = new decimal[(int)PachimonStatType.Count];
            for (var index = 0; index < values.Length; index++)
            {
                var statType = (PachimonStatType)index;
                var value = baseStats.GetDisplayedValue(statType);
                values[index] = value;
                contributions.Add(new StatContribution(
                    statType,
                    StatModifierOperation.Base,
                    value,
                    new StatModifierSource(
                        StatModifierSourceType.Base,
                        $"base:{statType}",
                        "基本")));
            }

            return values;
        }

        private static void ApplyAdditiveModifiers(
            decimal[] values,
            IEnumerable<IStatModifier> modifiers,
            StatModifierOperation operation,
            StatValueSnapshot referenceStats,
            ICollection<StatContribution> contributions)
        {
            foreach (var modifier in modifiers.Where(item => item.Operation == operation))
            {
                var value = modifier.Evaluate(referenceStats);
                values[(int)modifier.TargetStat] += value;
                contributions.Add(new StatContribution(
                    modifier.TargetStat,
                    operation,
                    value,
                    modifier.Source));
            }
        }

        private static void ApplyMultiplicativeModifiers(
            decimal[] values,
            IEnumerable<IStatModifier> modifiers,
            StatModifierOperation operation,
            StatValueSnapshot referenceStats,
            ICollection<StatContribution> contributions)
        {
            foreach (var modifier in modifiers.Where(item => item.Operation == operation))
            {
                var multiplier = modifier.Evaluate(referenceStats);
                values[(int)modifier.TargetStat] *= multiplier;
                contributions.Add(new StatContribution(
                    modifier.TargetStat,
                    operation,
                    multiplier,
                    modifier.Source));
            }
        }
    }
}
