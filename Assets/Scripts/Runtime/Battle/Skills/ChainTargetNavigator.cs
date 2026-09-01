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
            int baseChainCount,
            BattleStatusId chainStatusId)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (baseChainCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseChainCount));
            }

            return checked(
                baseChainCount
                + SkillChainRuntime.GetAdditionalChainCount(
                    user,
                    chainStatusId));
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

    public static class SkillChainRuntime
    {
        public static int GetAdditionalChainCount(
            BattleUnitState unit,
            BattleStatusId chainStatusId)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            ValidateStatusId(chainStatusId);
            return unit.GetStatus(chainStatusId)?.Value ?? 0;
        }

        public static void Add(
            BattleUnitState target,
            BattleUnitState source,
            BattleStatusId chainStatusId,
            int amount)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));
            ValidateStatusId(chainStatusId);
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

            var total = checked(
                GetAdditionalChainCount(target, chainStatusId) + amount);
            target.ApplyOrReplaceStatus(new BattleStatusInstance(
                chainStatusId,
                BattleStatusCategory.None,
                source,
                total));
        }

        private static void ValidateStatusId(BattleStatusId statusId)
        {
            if (statusId is not (
                BattleStatusId.ChainBurnChain
                or BattleStatusId.ChainVinesChain
                or BattleStatusId.CuttingDanceChain))
            {
                throw new ArgumentOutOfRangeException(nameof(statusId));
            }
        }
    }
}
