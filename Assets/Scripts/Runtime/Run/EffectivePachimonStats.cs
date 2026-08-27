using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Run
{
    public sealed class EffectivePachimonStats
    {
        private static readonly StatCalculator Calculator = new();

        public EffectivePachimonStats(PachimonStats baseStats, TrainerModifierSet modifiers)
            : this(Calculator.Calculate(
                baseStats,
                TrainerStatModifierFactory.Create(modifiers),
                PachimonSubStatBindings.CreateDefault()))
        {
        }

        private EffectivePachimonStats(StatCalculationResult calculation)
        {
            Calculation = calculation
                ?? throw new System.ArgumentNullException(nameof(calculation));
        }

        public StatCalculationResult Calculation { get; }
        public int MaxHp => GetValue(PachimonStatType.MaxHp);
        public int MaxMn => GetValue(PachimonStatType.MaxMn);
        public int DamageBonus => GetValue(PachimonStatType.DamageBonus);
        public int ResistBonus => GetValue(PachimonStatType.ResistBonus);

        public static EffectivePachimonStats Calculate(
            PachimonStats baseStats,
            IEnumerable<IStatModifier> modifiers,
            PachimonSubStatBindings bindings = null)
        {
            return new EffectivePachimonStats(Calculator.Calculate(
                baseStats,
                modifiers,
                bindings));
        }

        public int GetValue(PachimonStatType statType)
        {
            return Calculation.GetValue(statType);
        }
    }
}
