using System;
using System.Linq;

namespace Pachimon.Battle
{
    public readonly struct ShieldApplicationPlan
    {
        public ShieldApplicationPlan(decimal value, decimal? durationTicks)
        {
            Value = value;
            DurationTicks = durationTicks;
        }

        public decimal Value { get; }
        public decimal? DurationTicks { get; }
    }

    public sealed class BattleSupportEffectRuntime
    {
        private readonly BattleState _state;

        public BattleSupportEffectRuntime(BattleState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public BattleShieldInstance ApplyShield(
            BattleUnitState source,
            BattleUnitState target,
            int value,
            int? durationTicks = null,
            bool isSharedEffect = false)
        {
            ValidateUnit(target, nameof(target));
            if (source != null) ValidateUnit(source, nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));

            var plan = _state.Passives.ModifyShield(
                _state,
                source,
                target,
                new ShieldApplicationPlan(
                    ApplySustainPower(source, value),
                    durationTicks));
            value = Pachimon.Run.SignedStatMath.FloorNonNegative(plan.Value);
            durationTicks = plan.DurationTicks.HasValue
                ? Math.Max(1, Pachimon.Run.SignedStatMath.CeilPositive(
                    plan.DurationTicks.Value))
                : null;
            if (value <= 0)
                throw new InvalidOperationException("Modified Shield value must be positive.");
            var shield = target.AddShield(value, durationTicks);
            _state.Events.Publish(new ShieldAppliedEvent(
                _state,
                source,
                target,
                value,
                durationTicks,
                isSharedEffect));
            return shield;
        }

        public int RemoveAllShields(BattleUnitState target)
        {
            ValidateUnit(target, nameof(target));
            return checked(
                target.RemoveAllShields()
                + _state.Fields.RemoveShieldEffects(target.Side));
        }

        public int RestoreHp(
            BattleUnitState source,
            BattleUnitState target,
            int requestedValue,
            bool isSharedEffect = false,
            bool applySustainPower = true)
        {
            ValidateUnit(target, nameof(target));
            if (source != null) ValidateUnit(source, nameof(source));
            if (requestedValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedValue));
            }

            var modifiedValue = _state.Passives.ModifyHealing(
                _state,
                source,
                target,
                applySustainPower
                    ? ApplySustainPower(source, requestedValue)
                    : requestedValue);
            var restoredValue = target.RestoreHp(
                Pachimon.Run.SignedStatMath.FloorNonNegative(modifiedValue));
            if (restoredValue > 0)
            {
                _state.Events.Publish(new HpRestoredEvent(
                    _state,
                    source,
                    target,
                    restoredValue,
                    isSharedEffect));
            }

            return restoredValue;
        }

        public static decimal ApplySustainPower(
            BattleUnitState source,
            decimal value)
        {
            if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value));
            if (source == null) return value;
            var attribute = source.SubStatBindings.GetAttribute(
                Pachimon.Run.PachimonStatType.SustainPower);
            return Pachimon.Run.SignedStatMath.ReplacePreAppliedAmplification(
                value,
                source.GetBattleStatValue(attribute),
                source.GetBattleStatValue(
                    Pachimon.Run.PachimonStatType.SustainPower));
        }

        private void ValidateUnit(BattleUnitState unit, string parameterName)
        {
            if (unit == null) throw new ArgumentNullException(parameterName);
            if (!_state.Player.Units.Concat(_state.Enemy.Units).Contains(unit))
            {
                throw new ArgumentException(
                    "The Unit does not belong to this Battle.",
                    parameterName);
            }
        }
    }
}
