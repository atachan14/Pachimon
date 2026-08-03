using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class BattleStatusFactory
    {
        public static BattleStatusInstance CreateCharging(
            BattleUnitState source,
            int value,
            ChargeSkillAsset skill)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return new BattleStatusInstance(
                BattleStatusId.Charging,
                BattleStatusCategory.Charge,
                source,
                value,
                durationTicks: CalculateDuration(
                    value,
                    skill.ChargingDurationPercent),
                tuning: CreateChargeTuning(skill));
        }

        public static BattleStatusInstance CreateCharged(
            BattleStatusInstance charging)
        {
            if (charging == null) throw new ArgumentNullException(nameof(charging));
            if (charging.StatusId != BattleStatusId.Charging
                || charging.Tuning is not ChargeStatusTuning tuning)
            {
                throw new ArgumentException(
                    "A Charging status with Charge tuning is required.",
                    nameof(charging));
            }

            return new BattleStatusInstance(
                BattleStatusId.Charged,
                BattleStatusCategory.Charge,
                charging.Source,
                charging.Value,
                durationTicks: CalculateDuration(
                    charging.Value,
                    tuning.ChargedDurationPercent),
                tuning: tuning);
        }

        private static ChargeStatusTuning CreateChargeTuning(
            ChargeSkillAsset skill)
        {
            return new ChargeStatusTuning(
                skill.ChargingResistBonusPercent,
                skill.ChargingElectricPercent,
                skill.ChargedDurationPercent,
                skill.ChargedElectricPercent,
                skill.ChargedSpeedPercent);
        }

        private static int CalculateDuration(int value, int percent)
        {
            return Math.Max(
                1,
                SignedStatMath.FloorNonNegative(value * percent / 100m));
        }
    }
}
