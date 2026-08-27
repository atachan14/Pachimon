using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Run
{
    public sealed class StatCalculator
    {
        public StatCalculationResult Calculate(
            PachimonStats baseStats,
            IEnumerable<IStatModifier> modifiers = null,
            PachimonSubStatBindings bindings = null)
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
            var baseReferenceValues = (decimal[])baseValues.Clone();
            ApplySubStatDerivations(
                baseReferenceValues,
                new StatValueSnapshot(baseValues),
                bindings,
                contributions: null);
            var baseSnapshot = new StatValueSnapshot(baseReferenceValues);

            var directAdditiveValues = (decimal[])baseValues.Clone();
            ApplyAdditiveModifiers(
                directAdditiveValues,
                modifierArray,
                StatModifierOperation.DirectAdditive,
                baseSnapshot,
                contributions);

            var directReferenceValues = (decimal[])directAdditiveValues.Clone();
            ApplySubStatDerivations(
                directReferenceValues,
                new StatValueSnapshot(directAdditiveValues),
                bindings,
                contributions: null);
            var directAdditiveSnapshot = new StatValueSnapshot(
                directReferenceValues);
            var additiveValues = (decimal[])directAdditiveValues.Clone();
            ApplyAdditiveModifiers(
                additiveValues,
                modifierArray,
                StatModifierOperation.DerivedAdditive,
                directAdditiveSnapshot,
                contributions);

            var additiveReferenceValues = (decimal[])additiveValues.Clone();
            ApplySubStatDerivations(
                additiveReferenceValues,
                new StatValueSnapshot(additiveValues),
                bindings,
                contributions: null);
            var additiveSnapshot = new StatValueSnapshot(additiveReferenceValues);
            var finalValues = (decimal[])additiveValues.Clone();
            ApplyMultiplicativeModifiers(
                finalValues,
                modifierArray,
                StatModifierOperation.DirectMultiplicative,
                baseSnapshot,
                contributions,
                subStatsOnly: false);
            ApplyMultiplicativeModifiers(
                finalValues,
                modifierArray,
                StatModifierOperation.DerivedMultiplicative,
                additiveSnapshot,
                contributions,
                subStatsOnly: false);
            ApplySubStatDerivations(
                finalValues,
                new StatValueSnapshot(finalValues),
                bindings,
                contributions);
            var finalReferenceSnapshot = new StatValueSnapshot(finalValues);
            ApplyMultiplicativeModifiers(
                finalValues,
                modifierArray,
                StatModifierOperation.DirectMultiplicative,
                baseSnapshot,
                contributions,
                subStatsOnly: true);
            ApplyMultiplicativeModifiers(
                finalValues,
                modifierArray,
                StatModifierOperation.DerivedMultiplicative,
                finalReferenceSnapshot,
                contributions,
                subStatsOnly: true);

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

        private static void ApplySubStatDerivations(
            decimal[] values,
            StatValueSnapshot referenceStats,
            PachimonSubStatBindings bindings,
            ICollection<StatContribution> contributions)
        {
            if (bindings == null)
            {
                return;
            }

            foreach (var subStat in PachimonSubStatBindings.SubStats)
            {
                var attribute = bindings.GetAttribute(subStat);
                var ratio = bindings.GetDerivationRatio(subStat);
                var value = referenceStats.GetValue(attribute) * ratio / 100m;
                values[(int)subStat] += value;
                contributions?.Add(new StatContribution(
                        subStat,
                        StatModifierOperation.DerivedAdditive,
                        value,
                        new StatModifierSource(
                            StatModifierSourceType.Base,
                            $"binding:{attribute}:{subStat}:{ratio}",
                            $"{attribute} Binding ({ratio}%)")));
            }
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
            ICollection<StatContribution> contributions,
            bool subStatsOnly)
        {
            foreach (var modifier in modifiers.Where(item =>
                         item.Operation == operation
                         && PachimonSubStatBindings.IsSubStat(item.TargetStat)
                            == subStatsOnly))
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
