using System;
using System.Collections.Generic;

namespace Pachimon.Run
{
    public static class PachimonStatService
    {
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
