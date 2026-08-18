using System;
using Pachimon.Battle;

namespace Pachimon.UI
{
    public static class StatusDescriptionValueProviderRegistry
    {
        public static bool TryCreateContext(
            BattleStatusInstance status,
            out DescriptionTemplateContext context)
        {
            context = new DescriptionTemplateContext();
            if (status == null)
            {
                return false;
            }

            context.Set("value", status.Value)
                .Set("totalValue", checked(status.Value * status.StackCount))
                .Set("stackCount", status.StackCount)
                .Set("remainingTicks",
                    status.RemainingTicks?.ToString() ?? "Battle中")
                .Set("source", status.Source?.DisplayName ?? "状態効果");

            switch (status.Definition)
            {
                case SlowStatusAsset slow:
                    context.Set("decayPerTick", slow.DecayPerTick);
                    break;
                case ToxinStatusAsset toxin:
                    context.Set("damagePerTickRatio", toxin.DamagePerTickRatio)
                        .Set("decayPerTickRatio", toxin.DecayPerTickRatio)
                        .Set("damagePerTick", FormatDecimal(
                            status.Value * toxin.DamagePerTickRatio / 100m))
                        .Set("decayPerTick", FormatDecimal(
                            status.Value * toxin.DecayPerTickRatio / 100m));
                    break;
                case ChargeStatusAsset charge:
                    AddChargeValues(status, charge, context);
                    break;
                case FreezeStatusAsset freeze:
                    context.Set("fireDamagePerDecay", freeze.FireDamagePerDecay);
                    break;
                case KnockoutStatusAsset knockout:
                    context.Set("damageDurationRatio", knockout.DamageDurationRatio);
                    break;
                case FlyingStatusAsset flying:
                    context.Set("windSpeedRatio", flying.WindSpeedRatio);
                    break;
                case LaunchCeremonyStatusAsset launch:
                    context.Set("aquaMultiplier", launch.AquaMultiplierPercent)
                        .Set("manaReductionRatio", launch.ManaReductionAquaRatio);
                    break;
                case WindErosionStatusAsset erosion:
                    context.Set("decayPerTick", erosion.DecayPerTick);
                    break;
            }

            switch (status.RuntimeData)
            {
                case FrozenBreakRuntimeState frozenBreak:
                    context.Set("healingPerTick",
                            FormatDecimal(frozenBreak.HealPerTick))
                        .Set("totalDuration", frozenBreak.TotalDurationTicks);
                    break;
                case HealingWindRuntimeData healingWind:
                    context.Set("windBonus", healingWind.WindBonus)
                        .Set("speedBonus", healingWind.SpeedBonus);
                    break;
                case DragonDanceRuntimeData dragonDance:
                    context.Set("dragonBonus", dragonDance.DragonBonus)
                        .Set("speedBonus", dragonDance.SpeedBonus);
                    break;
            }

            return true;
        }

        private static void AddChargeValues(
            BattleStatusInstance status,
            ChargeStatusAsset definition,
            DescriptionTemplateContext context)
        {
            if (status.RuntimeData is not ChargeStatusRuntimeState charge)
            {
                return;
            }

            context.Set("phase", charge.Phase);
            if (charge.Phase == ChargePhase.Charging)
            {
                context.Set("resistBonus", decimal.Floor(
                        status.Value
                        * definition.ChargingResistBonusRatio / 100m))
                    .Set("electricMultiplier",
                        definition.ChargingElectricRatio);
                return;
            }

            context.Set("speedBonus", decimal.Floor(
                    status.Value * definition.ChargedSpeedRatio / 100m))
                .Set("electricMultiplier", definition.ChargedElectricRatio)
                .Set("durationRatio", definition.ChargedDurationRatio);
        }

        private static string FormatDecimal(decimal value) =>
            value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
