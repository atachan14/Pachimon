using System;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public static class AttributeDamageCalculator
    {
        public static int Calculate(
            int baseDamage,
            EffectivePachimonStats attackerStats,
            EffectivePachimonStats defenderStats,
            PachimonAttribute attribute)
        {
            if (baseDamage <= 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (attackerStats == null) throw new ArgumentNullException(nameof(attackerStats));
            if (defenderStats == null) throw new ArgumentNullException(nameof(defenderStats));

            var attributeStat = PachimonStatTypeUtility.FromAttribute(attribute);
            var power = attackerStats.GetValue(attributeStat);
            var damageBonus = attackerStats.DamageBonus;
            var resist = defenderStats.GetValue(attributeStat);
            var resistBonus = defenderStats.ResistBonus;
            var rawDamage = ((long)baseDamage * (100L + power)) / 100L;
            var afterDamageBonus = (rawDamage * (100L + damageBonus)) / 100L;
            var afterAttribute = (afterDamageBonus * 100L) / (100L + resist);
            var finalDamage = (afterAttribute * 100L) / (100L + resistBonus);
            return (int)Math.Max(1L, Math.Min(finalDamage, int.MaxValue));
        }
    }
}
