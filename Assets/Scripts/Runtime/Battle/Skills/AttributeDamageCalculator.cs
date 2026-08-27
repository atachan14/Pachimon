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
            var attackerAttribute = context.AttackerAttributeValue
                ?? context.AttackerStats.GetValue(statType);
            var damageBonus = context.ApplyDamageBonusMultiplier
                ? context.AttackerStats.DamageBonus
                : 0m;
            var attackerAttributeMultiplier = context.ApplyAttackerAttributeMultiplier
                ? SignedStatMath.CombineAmplificationStats(
                    attackerAttribute,
                    damageBonus)
                : 1m;
            var damageBonusMultiplier = !context.ApplyAttackerAttributeMultiplier
                                        && context.ApplyDamageBonusMultiplier
                ? SignedStatMath.CombineAmplificationStats(
                      attackerAttribute,
                      damageBonus)
                  / SignedStatMath.AmplificationMultiplier(attackerAttribute)
                : 1m;
            var preDefenseDamage = context.BaseDamage
                * attackerAttributeMultiplier
                * damageBonusMultiplier;
            var resistBonusFixedPenetration = Math.Max(
                0m,
                context.Penetration.ResistBonusFixed);
            var effectiveDefenderAttribute = PenetrationMath.ApplyToDefense(
                context.DefenderStats.GetValue(statType),
                context.Penetration.AttributePercentage,
                context.Penetration.AttributeFixed);
            var effectiveResistBonus = PenetrationMath.ApplyToDefense(
                context.DefenderStats.ResistBonus,
                context.Penetration.ResistBonusPercentage,
                resistBonusFixedPenetration);
            var defenderAttributeMultiplier =
                SignedStatMath.CombineReductionStats(
                    effectiveDefenderAttribute,
                    effectiveResistBonus);
            var resistBonusMultiplier = 1m;
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
