using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class BattleStatusFactory
    {
        public static BattleStatusInstance CreateToxin(
            BattleUnitState source,
            int value,
            ToxinStatusAsset definition)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var toxin = new BattleStatusInstance(
                BattleStatusId.Toxin,
                BattleStatusCategory.Toxin,
                source: null,
                value: 0,
                definition: definition);
            toxin.AddToxinApplication(source, value);
            return toxin;
        }

        public static BattleStatusInstance CreateStun(
            BattleUnitState source,
            int durationTicks,
            StunStatusAsset definition)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (durationTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new BattleStatusInstance(
                BattleStatusId.Stun,
                BattleStatusCategory.Stun,
                source,
                value: 0,
                durationTicks: durationTicks,
                definition: definition);
        }

        public static BattleStatusInstance CreateFreeze(
            BattleUnitState source,
            int value,
            FreezeStatusAsset definition)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new BattleStatusInstance(
                BattleStatusId.Freeze,
                BattleStatusCategory.Stun,
                source,
                value,
                definition: definition);
        }

        public static BattleStatusInstance CreateSlow(
            BattleUnitState source,
            int value,
            SlowStatusAsset definition)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new BattleStatusInstance(
                definition.StatusId,
                BattleStatusCategory.Slow,
                source,
                value,
                definition: definition);
        }

        public static BattleStatusInstance CreateBurn(
            BattleUnitState source,
            int value,
            BurnStatusAsset definition)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new BattleStatusInstance(
                BattleStatusId.Burn,
                BattleStatusCategory.Burn,
                source,
                value,
                definition: definition);
        }

        public static BattleStatusInstance CreateCharging(
            BattleUnitState source,
            int value,
            ChargeStatusAsset definition)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            return new BattleStatusInstance(
                BattleStatusId.Charge,
                BattleStatusCategory.Charge,
                source,
                value,
                runtimeData: new ChargeStatusRuntimeState(
                    ChargePhase.Charging),
                definition: definition);
        }

        public static BattleStatusInstance CreateCharged(
            BattleStatusInstance charging)
        {
            if (charging == null) throw new ArgumentNullException(nameof(charging));
            if (charging.StatusId != BattleStatusId.Charge
                || charging.Definition is not ChargeStatusAsset definition
                || charging.RuntimeData is not ChargeStatusRuntimeState state
                || state.Phase != ChargePhase.Charging)
            {
                throw new ArgumentException(
                    "A Charge status in the Charging phase is required.",
                    nameof(charging));
            }

            return new BattleStatusInstance(
                BattleStatusId.Charge,
                BattleStatusCategory.Charge,
                charging.Source,
                charging.Value,
                durationTicks: CalculateDuration(
                    charging.Value,
                    definition.ChargedDurationRatio),
                runtimeData: new ChargeStatusRuntimeState(
                    ChargePhase.Charged),
                definition: definition);
        }

        private static int CalculateDuration(int value, int percent)
        {
            return Math.Max(
                1,
                SignedStatMath.FloorNonNegative(value * percent / 100m));
        }
    }
}
