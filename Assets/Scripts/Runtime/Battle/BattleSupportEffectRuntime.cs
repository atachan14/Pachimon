using System;
using System.Linq;

namespace Pachimon.Battle
{
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

        public int RestoreHp(
            BattleUnitState source,
            BattleUnitState target,
            int requestedValue,
            bool isSharedEffect = false)
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
                requestedValue);
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
