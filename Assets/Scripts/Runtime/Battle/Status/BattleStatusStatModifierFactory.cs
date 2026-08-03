using System;
using System.Collections.Generic;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public static class BattleStatusStatModifierFactory
    {
        public static IReadOnlyList<IStatModifier> Create(
            IEnumerable<BattleStatusInstance> statuses)
        {
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));

            var modifiers = new List<IStatModifier>();
            foreach (var status in statuses)
            {
                if (status == null)
                {
                    continue;
                }

                AddSlowModifier(status, modifiers);
                AddChargeModifiers(status, modifiers);
            }

            return modifiers;
        }

        private static void AddSlowModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if ((status.Categories & BattleStatusCategory.Slow) == 0)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DirectAdditive,
                -checked(status.Value * status.StackCount),
                CreateSource(status)));
        }

        private static void AddChargeModifiers(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.Tuning is not ChargeStatusTuning tuning)
            {
                return;
            }

            var source = CreateSource(status);
            if (status.StatusId == BattleStatusId.Charging)
            {
                modifiers.Add(new FixedStatModifier(
                    PachimonStatType.ResistBonus,
                    StatModifierOperation.DirectAdditive,
                    status.Value * tuning.ChargingResistBonusPercent / 100m,
                    source));
                modifiers.Add(new FixedStatModifier(
                    PachimonStatType.Electric,
                    StatModifierOperation.DirectMultiplicative,
                    tuning.ChargingElectricPercent / 100m,
                    source));
                return;
            }

            if (status.StatusId != BattleStatusId.Charged)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DirectAdditive,
                status.Value * tuning.ChargedSpeedPercent / 100m,
                source));
            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Electric,
                StatModifierOperation.DirectMultiplicative,
                tuning.ChargedElectricPercent / 100m,
                source));
        }

        private static StatModifierSource CreateSource(
            BattleStatusInstance status)
        {
            return new StatModifierSource(
                StatModifierSourceType.StatusEffect,
                $"status:{(int)status.StatusId}",
                status.DisplayName);
        }
    }
}
