using System;

namespace Pachimon.Battle
{
    public sealed class DamageCalculationResult
    {
        public DamageCalculationResult(
            DamageContext context,
            decimal attackerAttributeMultiplier,
            decimal damageBonusMultiplier,
            decimal preDefenseDamage,
            decimal effectiveDefenderAttribute,
            decimal effectiveResistBonus,
            decimal defenderAttributeMultiplier,
            decimal resistBonusMultiplier,
            decimal unroundedDamage,
            int finalDamage)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            AttackerAttributeMultiplier = attackerAttributeMultiplier;
            DamageBonusMultiplier = damageBonusMultiplier;
            PreDefenseDamage = preDefenseDamage;
            EffectiveDefenderAttribute = effectiveDefenderAttribute;
            EffectiveResistBonus = effectiveResistBonus;
            DefenderAttributeMultiplier = defenderAttributeMultiplier;
            ResistBonusMultiplier = resistBonusMultiplier;
            UnroundedDamage = unroundedDamage;
            FinalDamage = finalDamage;
        }

        public DamageContext Context { get; }
        public decimal AttackerAttributeMultiplier { get; }
        public decimal DamageBonusMultiplier { get; }
        public decimal PreDefenseDamage { get; }
        public decimal EffectiveDefenderAttribute { get; }
        public decimal EffectiveResistBonus { get; }
        public decimal DefenderAttributeMultiplier { get; }
        public decimal ResistBonusMultiplier { get; }
        public decimal UnroundedDamage { get; }
        public int FinalDamage { get; }
    }
}
