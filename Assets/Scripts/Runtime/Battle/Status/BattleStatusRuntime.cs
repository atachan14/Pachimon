using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleStatusRuntime
    {
        private const int LeakOriginId = (int)BattleStatusId.Leak;
        private const int MaxReactionDepth = 32;

        private readonly BattleState _state;
        private readonly List<SkillEffectResult> _collectedEffects = new();
        private bool _isCollecting;
        private int _reactionDepth;

        public BattleStatusRuntime(BattleState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void ApplyStatus(
            BattleUnitState target,
            BattleStatusInstance status)
        {
            ValidateTarget(target);
            if (status == null) throw new ArgumentNullException(nameof(status));
            status = ApplyIncomingValueReduction(target, status);
            if (status.Value == 0
                && (status.Categories
                    & (BattleStatusCategory.Slow | BattleStatusCategory.Leak)) != 0)
            {
                return;
            }

            if ((status.Categories & BattleStatusCategory.Slow) != 0)
            {
                var existing = target.GetStatus(status.StatusId);
                if (existing != null)
                {
                    existing.AddValue(status.Value);
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }
            }
            else if (status.IsTimed)
            {
                target.AddStatusInstance(status);
            }
            else
            {
                target.ApplyOrReplaceStatus(status);
            }
            RefreshActionClockPause(target);
        }

        private static BattleStatusInstance ApplyIncomingValueReduction(
            BattleUnitState target,
            BattleStatusInstance status)
        {
            var defenseStat = status.StatusId switch
            {
                BattleStatusId.Paralysis => PachimonStatType.Electric,
                BattleStatusId.Chill => PachimonStatType.Ice,
                _ => (PachimonStatType?)null,
            };
            if (!defenseStat.HasValue || status.Value <= 0)
            {
                return status;
            }

            var reducedValue = SignedStatMath.FloorNonNegative(
                status.Value
                * SignedStatMath.ReductionMultiplier(
                    target.GetBattleStatValue(defenseStat.Value)));
            return new BattleStatusInstance(
                status.StatusId,
                status.Categories,
                status.Source,
                reducedValue,
                status.StackCount,
                status.RemainingTicks,
                status.Tuning);
        }

        public bool TryConsumeStatus(
            BattleUnitState target,
            BattleStatusId statusId,
            out BattleStatusInstance status)
        {
            ValidateTarget(target);
            var consumed = target.TryConsumeStatus(statusId, out status);
            if (consumed)
            {
                RefreshActionClockPause(target);
            }

            return consumed;
        }

        public int GetNextExpirationTicks()
        {
            return GetAllUnits()
                .SelectMany(unit => unit.Statuses)
                .Where(status => status.RemainingTicks.HasValue)
                .Select(status => status.RemainingTicks.Value)
                .DefaultIfEmpty(int.MaxValue)
                .Min();
        }

        internal void AdvanceTime(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (ticks == 0)
            {
                return;
            }

            foreach (var unit in GetAllUnits())
            {
                var expiredStatuses = unit.AdvanceStatuses(ticks);
                foreach (var expired in expiredStatuses)
                {
                    HandleStatusExpired(unit, expired);
                }
                RefreshActionClockPause(unit);
            }
        }

        private void HandleStatusExpired(
            BattleUnitState target,
            BattleStatusInstance expired)
        {
            if (expired.StatusId == BattleStatusId.Charging)
            {
                ApplyStatus(target, BattleStatusFactory.CreateCharged(expired));
            }
        }

        internal void RefreshAllActionClockPauses()
        {
            foreach (var unit in GetAllUnits())
            {
                RefreshActionClockPause(unit);
            }
        }

        public void BeginSkillResolution()
        {
            _collectedEffects.Clear();
            _isCollecting = true;
            _reactionDepth = 0;
        }

        public IReadOnlyList<SkillEffectResult> EndSkillResolution()
        {
            var effects = _collectedEffects.ToArray();
            _collectedEffects.Clear();
            _isCollecting = false;
            _reactionDepth = 0;
            return effects;
        }

        public void CancelSkillResolution()
        {
            _collectedEffects.Clear();
            _isCollecting = false;
            _reactionDepth = 0;
        }

        public void HandleAttributeDamageApplied(
            AttributeDamageAppliedEvent damageEvent)
        {
            if (damageEvent == null)
            {
                throw new ArgumentNullException(nameof(damageEvent));
            }

            if (damageEvent.Attribute != PachimonAttribute.Electric
                || !damageEvent.Target.TryConsumeStatus(
                    BattleStatusId.Leak,
                    out var leak))
            {
                return;
            }

            ResolveLeak(damageEvent, leak);
        }

        private void ResolveLeak(
            AttributeDamageAppliedEvent trigger,
            BattleStatusInstance leak)
        {
            if (_reactionDepth >= MaxReactionDepth)
            {
                throw new InvalidOperationException(
                    "Status reaction depth exceeded the safety limit.");
            }

            var rawExtraDamage =
                trigger.AppliedDamage * leak.Value / 100m;
            if (rawExtraDamage <= 0m)
            {
                return;
            }

            _reactionDepth++;
            try
            {
                var targetSide = trigger.Target.Side == BattleSide.Player
                    ? _state.Player
                    : _state.Enemy;
                foreach (var target in targetSide.GetAllLiving().ToArray())
                {
                    var result = BattleAttributeDamageService.Apply(
                        _state,
                        trigger.Source,
                        target,
                        new DamageContext(
                            DamageOriginKind.Status,
                            LeakOriginId,
                            rawExtraDamage,
                            trigger.Source.GetBattleStats(),
                            target.GetBattleStats(),
                            PachimonAttribute.Electric,
                            isAttack: false,
                            applyAttackerAttributeMultiplier: false,
                            penetrationPercent: 0m,
                            applyDamageBonusMultiplier: false,
                            applyOutgoingModifiers: true));
                    if (_isCollecting)
                    {
                        _collectedEffects.Add(new SkillEffectResult(
                            target,
                            result.AppliedDamage,
                            isTrueDamage: false));
                    }
                }
            }
            finally
            {
                _reactionDepth--;
            }
        }

        private void RefreshActionClockPause(BattleUnitState unit)
        {
            unit.SetActionClockPaused(
                unit.HasStatusCategory(BattleStatusCategory.Stun));
        }

        private IEnumerable<BattleUnitState> GetAllUnits()
        {
            return _state.Player.Units.Concat(_state.Enemy.Units);
        }

        private void ValidateTarget(BattleUnitState target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!GetAllUnits().Contains(target))
            {
                throw new ArgumentException(
                    "The Status target does not belong to this Battle.",
                    nameof(target));
            }
        }
    }
}
