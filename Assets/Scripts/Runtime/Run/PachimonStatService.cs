using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Run
{
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
                modifiers);
        }

        public static EffectivePachimonStats Calculate(
            PachimonStats baseStats,
            TrainerModifierSet trainerModifiers,
            IEnumerable<int> passiveIds,
            PassiveStatModifierRegistry passiveRegistry,
            IEnumerable<IStatModifier> contextModifiers = null)
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

            return EffectivePachimonStats.Calculate(baseStats, modifiers);
        }
    }
}
