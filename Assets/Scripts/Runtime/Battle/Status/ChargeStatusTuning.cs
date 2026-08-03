using System;

namespace Pachimon.Battle
{
    public sealed class ChargeStatusTuning
    {
        public ChargeStatusTuning(
            int chargingResistBonusPercent,
            int chargingElectricPercent,
            int chargedDurationPercent,
            int chargedElectricPercent,
            int chargedSpeedPercent)
        {
            if (chargingResistBonusPercent < 0
                || chargingElectricPercent < 0
                || chargedDurationPercent <= 0
                || chargedElectricPercent < 0
                || chargedSpeedPercent < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(chargingResistBonusPercent));
            }

            ChargingResistBonusPercent = chargingResistBonusPercent;
            ChargingElectricPercent = chargingElectricPercent;
            ChargedDurationPercent = chargedDurationPercent;
            ChargedElectricPercent = chargedElectricPercent;
            ChargedSpeedPercent = chargedSpeedPercent;
        }

        public int ChargingResistBonusPercent { get; }
        public int ChargingElectricPercent { get; }
        public int ChargedDurationPercent { get; }
        public int ChargedElectricPercent { get; }
        public int ChargedSpeedPercent { get; }
    }
}
