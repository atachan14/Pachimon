using System;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public static class AttributeDamageCalculator
    {
        public static DamageCalculationResult Calculate(DamageContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var statType = PachimonStatTypeUtility.FromAttribute(context.Attribute);
            var attackerAttributeMultiplier = context.ApplyAttackerAttributeMultiplier
                ? SignedStatMath.AmplificationMultiplier(
                    context.AttackerStats.GetValue(statType))
                : 1m;
            var damageBonusMultiplier = context.ApplyDamageBonusMultiplier
                ? SignedStatMath.AmplificationMultiplier(
                    context.AttackerStats.DamageBonus)
                : 1m;
            var preDefenseDamage = context.BaseDamage
                * attackerAttributeMultiplier
                * damageBonusMultiplier;
            var penetrationMultiplier = 1m - context.PenetrationPercent / 100m;
            var effectiveDefenderAttribute =
                context.DefenderStats.GetValue(statType) * penetrationMultiplier;
            var effectiveResistBonus =
                context.DefenderStats.ResistBonus * penetrationMultiplier;
            var defenderAttributeMultiplier =
                SignedStatMath.ReductionMultiplier(effectiveDefenderAttribute);
            var resistBonusMultiplier =
                SignedStatMath.ReductionMultiplier(effectiveResistBonus);
            var unroundedDamage = preDefenseDamage
                * defenderAttributeMultiplier
                * resistBonusMultiplier;
            return new DamageCalculationResult(
                context,
                attackerAttributeMultiplier,
                damageBonusMultiplier,
                preDefenseDamage,
                effectiveDefenderAttribute,
                effectiveResistBonus,
                defenderAttributeMultiplier,
                resistBonusMultiplier,
                unroundedDamage,
                FinalizeNormalDamage(unroundedDamage));
        }

        public static decimal CalculateUnrounded(
            int baseDamage,
            EffectivePachimonStats attackerStats,
            EffectivePachimonStats defenderStats,
            PachimonAttribute attribute)
        {
            if (baseDamage <= 0) throw new ArgumentOutOfRangeException(nameof(baseDamage));
            if (attackerStats == null) throw new ArgumentNullException(nameof(attackerStats));
            if (defenderStats == null) throw new ArgumentNullException(nameof(defenderStats));

            return Calculate(new DamageContext(
                DamageOriginKind.Skill,
                originId: 1,
                baseDamage,
                attackerStats,
                defenderStats,
                attribute,
                isAttack: true)).UnroundedDamage;
        }

        public static int FinalizeNormalDamage(decimal unroundedDamage)
        {
            return unroundedDamage <= 0m
                ? 0
                : SignedStatMath.FloorNonNegative(unroundedDamage, 1);
        }

        public static int Calculate(
            int baseDamage,
            EffectivePachimonStats attackerStats,
            EffectivePachimonStats defenderStats,
            PachimonAttribute attribute)
        {
            return FinalizeNormalDamage(CalculateUnrounded(
                baseDamage,
                attackerStats,
                defenderStats,
                attribute));
        }
    }
}
