using System;
using System.Linq;

namespace Pachimon.Battle
{
    public sealed class ChainTargetNavigator
    {
        private readonly BattleSideState _targetSide;
        private int _currentSlot;
        private int _direction = 1;
        private bool _started;

        public ChainTargetNavigator(BattleSideState targetSide)
        {
            _targetSide = targetSide
                ?? throw new ArgumentNullException(nameof(targetSide));
        }

        public BattleUnitState GetNext()
        {
            var living = _targetSide.GetAllTargetable()
                .OrderBy(unit => unit.SlotIndex)
                .ToArray();
            if (living.Length == 0)
            {
                return null;
            }

            if (!_started)
            {
                _started = true;
                _currentSlot = living[0].SlotIndex;
                return living[0];
            }

            if (living.Length == 1)
            {
                _currentSlot = living[0].SlotIndex;
                return living[0];
            }

            var next = FindInDirection(living, _direction);
            if (next == null)
            {
                _direction *= -1;
                next = FindInDirection(living, _direction);
            }

            next ??= living.FirstOrDefault(unit => unit.SlotIndex == _currentSlot)
                ?? (_direction > 0 ? living[0] : living[^1]);
            _currentSlot = next.SlotIndex;
            return next;
        }

        public static decimal GetDamageRatio(
            int hitIndex,
            int additionalChainCount)
        {
            if (additionalChainCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(additionalChainCount));
            }
            if (hitIndex < 0 || hitIndex > additionalChainCount)
            {
                throw new ArgumentOutOfRangeException(nameof(hitIndex));
            }

            return (additionalChainCount + 1m - hitIndex)
                / (additionalChainCount + 1m);
        }

        public static int GetEffectiveAdditionalChainCount(
            BattleUnitState user,
            int baseChainCount)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (baseChainCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseChainCount));
            }

            var addChain = AddChainRuntime.GetWholeChains(user);
            return checked(baseChainCount + addChain);
        }

        private BattleUnitState FindInDirection(
            BattleUnitState[] living,
            int direction)
        {
            return direction > 0
                ? living.FirstOrDefault(unit => unit.SlotIndex > _currentSlot)
                : living.LastOrDefault(unit => unit.SlotIndex < _currentSlot);
        }
    }

    public static class AddChainRuntime
    {
        public const int UnitsPerChain = 100;

        public static int GetStoredUnits(BattleUnitState unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            return unit.GetStatus(BattleStatusId.AddChain)?.Value ?? 0;
        }

        public static int GetWholeChains(BattleUnitState unit)
        {
            return GetStoredUnits(unit) / UnitsPerChain;
        }

        public static void AddUnits(
            BattleUnitState target,
            BattleUnitState source,
            int units)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units));

            var totalUnits = checked(GetStoredUnits(target) + units);
            target.ApplyOrReplaceStatus(new BattleStatusInstance(
                BattleStatusId.AddChain,
                BattleStatusCategory.None,
                source,
                totalUnits));
        }

        public static string FormatUnits(int units)
        {
            if (units < 0) throw new ArgumentOutOfRangeException(nameof(units));
            return (units / (decimal)UnitsPerChain).ToString("0.0#");
        }
    }
}
