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
                .OfType<DerivedAdditivePassiveAsset>()
                .Select(CreateModifier)
                .ToArray();
        }

        private static IStatModifier CreateModifier(
            DerivedAdditivePassiveAsset definition)
        {
            return new DerivedStatModifier(
                definition.TargetStat,
                StatModifierOperation.DerivedAdditive,
                stats => Math.Max(
                    definition.MinimumContribution,
                    stats.GetValue(definition.ReferenceStat)
                    * definition.Percent
                    / 100m),
                new StatModifierSource(
                    StatModifierSourceType.Passive,
                    $"passive:{definition.PassiveId}",
                    definition.DisplayName));
        }
    }
}
