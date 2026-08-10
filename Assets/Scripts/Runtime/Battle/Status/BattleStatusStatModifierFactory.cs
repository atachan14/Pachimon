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
                AddToxinGrowthModifier(status, modifiers);
                AddFireGrowthModifier(status, modifiers);
                AddIceGrowthModifier(status, modifiers);
                AddLeafGrowthModifier(status, modifiers);
                AddComboMasterModifier(status, modifiers);
                AddBurnModifier(status, modifiers);
                AddLaunchCeremonyModifier(status, modifiers);
                AddWindModifiers(status, modifiers);
                AddSweetScienceModifier(status, modifiers);
                AddDragonDanceModifiers(status, modifiers);
            }

            return modifiers;
        }

        private static void AddDragonDanceModifiers(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.Definition is not DragonDanceStatusAsset
                || status.RuntimeData is not DragonDanceRuntimeData dance)
            {
                return;
            }

            var source = CreateSource(status);
            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Dragon,
                StatModifierOperation.DirectAdditive,
                dance.DragonBonus,
                source));
            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DirectAdditive,
                dance.SpeedBonus,
                source));
        }

        private static void AddSweetScienceModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.Definition is not SweetScienceStatusAsset)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DirectAdditive,
                status.Value,
                CreateSource(status)));
        }

        private static void AddWindModifiers(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            var source = CreateSource(status);
            if (status.Definition is FlyingStatusAsset flying)
            {
                modifiers.Add(new DerivedStatModifier(
                    PachimonStatType.Speed,
                    StatModifierOperation.DerivedAdditive,
                    stats => decimal.Floor(
                        stats.GetValue(PachimonStatType.Wind)
                        * flying.WindSpeedRatio / 100m),
                    source));
            }

            if (status.Definition is WindErosionStatusAsset)
            {
                modifiers.Add(new FixedStatModifier(
                    PachimonStatType.ResistBonus,
                    StatModifierOperation.DirectAdditive,
                    -status.Value,
                    source));
            }

            if (status.Definition is HealingWindStatusAsset
                && status.RuntimeData is HealingWindRuntimeData healing)
            {
                modifiers.Add(new FixedStatModifier(
                    PachimonStatType.Wind,
                    StatModifierOperation.DirectAdditive,
                    healing.WindBonus,
                    source));
                modifiers.Add(new FixedStatModifier(
                    PachimonStatType.Speed,
                    StatModifierOperation.DirectAdditive,
                    healing.SpeedBonus,
                    source));
            }

            if (status.Definition is StillAirStatusAsset)
            {
                modifiers.Add(new FixedStatModifier(
                    PachimonStatType.Wind,
                    StatModifierOperation.DirectMultiplicative,
                    0m,
                    source));
            }

        }

        private static void AddLaunchCeremonyModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.Definition is not LaunchCeremonyStatusAsset definition)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Aqua,
                StatModifierOperation.DirectMultiplicative,
                definition.AquaMultiplierPercent / 100m,
                CreateSource(status)));
        }

        private static void AddBurnModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.StatusId != BattleStatusId.Burn)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.DamageBonus,
                StatModifierOperation.DirectAdditive,
                -checked(status.Value * status.StackCount),
                CreateSource(status)));
        }

        private static void AddComboMasterModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.StatusId != BattleStatusId.ComboMasterBonus)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.DamageBonus,
                StatModifierOperation.DirectAdditive,
                checked(status.Value * status.StackCount),
                CreateSource(status)));
        }

        private static void AddFireGrowthModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.StatusId != BattleStatusId.FireGrowth)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Fire,
                StatModifierOperation.DirectAdditive,
                checked(status.Value * status.StackCount),
                CreateSource(status)));
        }

        private static void AddIceGrowthModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.StatusId != BattleStatusId.IceGrowth)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Ice,
                StatModifierOperation.DirectAdditive,
                checked(status.Value * status.StackCount),
                CreateSource(status)));
        }

        private static void AddLeafGrowthModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.StatusId != BattleStatusId.LeafGrowth) return;
            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Leaf,
                StatModifierOperation.DirectAdditive,
                checked(status.Value * status.StackCount),
                CreateSource(status)));
        }

        private static void AddToxinGrowthModifier(
            BattleStatusInstance status,
            ICollection<IStatModifier> modifiers)
        {
            if (status.StatusId != BattleStatusId.ToxinGrowth)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Poison,
                StatModifierOperation.DirectMultiplicative,
                1m + status.Value * status.StackCount / 100m,
                CreateSource(status)));
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
            if (status.Definition is not ChargeStatusAsset definition
                || status.RuntimeData is not ChargeStatusRuntimeState state)
            {
                return;
            }

            var source = CreateSource(status);
            if (state.Phase == ChargePhase.Charging)
            {
                modifiers.Add(new FixedStatModifier(
                    PachimonStatType.ResistBonus,
                    StatModifierOperation.DirectAdditive,
                    status.Value
                        * definition.ChargingResistBonusRatio / 100m,
                    source));
                modifiers.Add(new FixedStatModifier(
                    PachimonStatType.Electric,
                    StatModifierOperation.DirectMultiplicative,
                    definition.ChargingElectricRatio / 100m,
                    source));
                return;
            }

            if (state.Phase != ChargePhase.Charged)
            {
                return;
            }

            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Speed,
                StatModifierOperation.DirectAdditive,
                status.Value * definition.ChargedSpeedRatio / 100m,
                source));
            modifiers.Add(new FixedStatModifier(
                PachimonStatType.Electric,
                StatModifierOperation.DirectMultiplicative,
                definition.ChargedElectricRatio / 100m,
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
