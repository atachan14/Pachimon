using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Run
{
    public static class PachimonDurabilityCalculator
    {
        public static decimal Calculate(EffectivePachimonStats stats)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));

            return Calculate(stats.MaxHp, stats.ResistBonus);
        }

        public static decimal Calculate(int maxHp, int resistBonus)
        {
            if (maxHp < 0) throw new ArgumentOutOfRangeException(nameof(maxHp));

            return maxHp / SignedStatMath.ReductionMultiplier(resistBonus);
        }
    }

    public static class PachimonStatService
    {
        public static EffectivePachimonStats Calculate(
            PachimonInstance instance,
            TrainerModifierSet trainerModifiers,
            PassiveStatModifierRegistry passiveRegistry,
            IEnumerable<IStatModifier> contextModifiers = null)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var modifiers = instance.PermanentStatModifiers.AsEnumerable();
            if (contextModifiers != null)
            {
                modifiers = modifiers.Concat(contextModifiers);
            }

            return Calculate(
                instance.Stats,
                trainerModifiers,
                instance.PassiveIds,
                passiveRegistry,
                modifiers,
                instance.SubStatBindings);
        }

        public static EffectivePachimonStats Calculate(
            PachimonStats baseStats,
            TrainerModifierSet trainerModifiers,
            IEnumerable<int> passiveIds,
            PassiveStatModifierRegistry passiveRegistry,
            IEnumerable<IStatModifier> contextModifiers = null,
            PachimonSubStatBindings bindings = null)
        {
            if (baseStats == null) throw new ArgumentNullException(nameof(baseStats));
            if (passiveRegistry == null)
            {
                throw new ArgumentNullException(nameof(passiveRegistry));
            }

            var modifiers = new List<IStatModifier>();
            modifiers.AddRange(TrainerStatModifierFactory.Create(trainerModifiers));
            modifiers.AddRange(passiveRegistry.CreateModifiers(passiveIds));
            if (contextModifiers != null)
            {
                modifiers.AddRange(contextModifiers);
            }

            return EffectivePachimonStats.Calculate(
                baseStats,
                modifiers,
                bindings ?? PachimonSubStatBindings.CreateDefault());
        }
    }
}
