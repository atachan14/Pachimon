using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Passives;

namespace Pachimon.Run
{
    public sealed class PassiveStatModifierRegistry
    {
        private readonly PassiveCatalog _catalog;

        public PassiveStatModifierRegistry(PassiveCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public bool TryGetDefinition(int passiveId, out PassiveAsset definition)
        {
            definition = _catalog.Get(passiveId);
            return definition != null;
        }

        public IReadOnlyList<IStatModifier> CreateModifiers(
            IEnumerable<int> passiveIds)
        {
            if (passiveIds == null)
            {
                return Array.Empty<IStatModifier>();
            }

            return passiveIds
                .Distinct()
                .Select(_catalog.Get)
                .Where(definition => definition is DerivedAdditivePassiveAsset
                    or DragonSkeletonPassiveAsset
                    or DragonGuardPassiveAsset)
                .SelectMany(CreateModifiers)
                .ToArray();
        }

        private static IEnumerable<IStatModifier> CreateModifiers(
            PassiveAsset definition)
        {
            if (definition is DerivedAdditivePassiveAsset derived)
            {
                yield return CreateModifier(derived);
                yield break;
            }

            if (definition is not DragonSkeletonPassiveAsset skeleton)
            {
                if (definition is DragonGuardPassiveAsset guard)
                {
                    var guardSource = new StatModifierSource(
                        StatModifierSourceType.Passive,
                        $"passive:{guard.PassiveId}",
                        guard.DisplayName);
                    yield return new DerivedStatModifier(
                        PachimonStatType.ResistBonus,
                        StatModifierOperation.DerivedAdditive,
                        stats => decimal.Floor(
                            stats.GetValue(PachimonStatType.Dragon)
                            * guard.ResistFromDragonRatio / 100m),
                        guardSource);
                }
                yield break;
            }

            var source = new StatModifierSource(
                StatModifierSourceType.Passive,
                $"passive:{skeleton.PassiveId}",
                skeleton.DisplayName);
            yield return new DerivedStatModifier(
                PachimonStatType.Dragon,
                StatModifierOperation.DerivedAdditive,
                stats => decimal.Floor(
                    stats.GetValue(PachimonStatType.Speed)
                    * skeleton.DragonFromSpeedRatio / 100m),
                source);
            yield return new DerivedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DerivedAdditive,
                stats => decimal.Floor(
                    stats.GetValue(PachimonStatType.Dragon)
                    * skeleton.SpeedFromDragonRatio / 100m),
                source);
        }

        private static IStatModifier CreateModifier(
            DerivedAdditivePassiveAsset definition)
        {
            return new DerivedStatModifier(
                definition.TargetStat,
                StatModifierOperation.DerivedAdditive,
                stats =>
                {
                    var contribution = Math.Max(
                        definition.MinimumContribution,
                        stats.GetValue(definition.ReferenceStat)
                        * definition.Percent
                        / 100m);
                    return definition.FloorContribution
                        ? SignedStatMath.FloorStat(
                            contribution,
                            clampToNonNegative: false)
                        : contribution;
                },
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{definition.PassiveId}",
                    definition.DisplayName));
        }
    }
}
