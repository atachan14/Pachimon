using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Run
{
    public static class TrainerModifierService
    {
        public static void AddStatModifier(
            TrainerModifierSet modifiers,
            IEnumerable<PachimonInstance> affectedPachimon,
            PachimonStatType statType,
            int amount,
            PassiveStatModifierRegistry passiveStatModifierRegistry)
        {
            if (modifiers == null)
            {
                throw new ArgumentNullException(nameof(modifiers));
            }

            if (affectedPachimon == null)
            {
                throw new ArgumentNullException(nameof(affectedPachimon));
            }

            if (passiveStatModifierRegistry == null)
            {
                throw new ArgumentNullException(nameof(passiveStatModifierRegistry));
            }

            var instances = affectedPachimon
                .Where(instance => instance != null)
                .Distinct()
                .ToArray();
            if (statType is not (PachimonStatType.MaxHp or PachimonStatType.MaxMn))
            {
                modifiers.AddStat(statType, amount);
                return;
            }

            var oldEffectiveMaximums = instances.ToDictionary(
                instance => instance,
                instance =>
                {
                    var stats = PachimonStatService.Calculate(
                        instance.Stats,
                        modifiers,
                        instance.PassiveIds,
                        passiveStatModifierRegistry);
                    return statType == PachimonStatType.MaxHp ? stats.MaxHp : stats.MaxMn;
                });
            modifiers.AddStat(statType, amount);

            foreach (var instance in instances)
            {
                var stats = PachimonStatService.Calculate(
                    instance.Stats,
                    modifiers,
                    instance.PassiveIds,
                    passiveStatModifierRegistry);
                if (statType == PachimonStatType.MaxHp)
                {
                    instance.ApplyEffectiveMaxHpChange(
                        oldEffectiveMaximums[instance],
                        stats.MaxHp);
                }
                else
                {
                    instance.ApplyEffectiveMaxMnChange(
                        oldEffectiveMaximums[instance],
                        stats.MaxMn);
                }
            }
        }
    }
}
