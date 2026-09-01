using System;
using System.Collections.Generic;

namespace Pachimon.Battle
{
    public enum SkillHitOutcome
    {
        Hit = 0,
        Evaded = 1,
        Blocked = 2,
    }

    public sealed class SkillHit
    {
        private readonly HashSet<int> _triggeredPassiveIds = new();
        private bool _weaknessCaptured;
        private int _weaknessValue;
        private bool _damageWasResolved;
        private bool _damageReachedTarget;

        internal SkillHit(
            BattleState state,
            BattleUnitState source,
            BattleUnitState intendedTarget,
            DamageOriginKind originKind,
            int originId)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            IntendedTarget = intendedTarget
                ?? throw new ArgumentNullException(nameof(intendedTarget));
            OriginKind = originKind;
            OriginId = originId;
            Target = state.Statuses.ResolveAttackTarget(
                source,
                intendedTarget,
                isAttack: true);
            Outcome = source.Side != Target.Side
                && state.Statuses.TryEvadeAttack(
                    source,
                    Target,
                    originKind,
                    originId)
                ? SkillHitOutcome.Evaded
                : SkillHitOutcome.Hit;
        }

        internal BattleState State { get; }
        public BattleUnitState Source { get; }
        public BattleUnitState IntendedTarget { get; }
        public BattleUnitState Target { get; }
        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
        public SkillHitOutcome Outcome { get; private set; }
        public BattleFieldEffectInstance InterceptedFieldEffect { get; private set; }
        public bool WasEvaded => Outcome == SkillHitOutcome.Evaded;
        public bool WasBlocked => Outcome == SkillHitOutcome.Blocked;
        internal bool DamageWasResolved => _damageWasResolved;
        internal bool CanApplyStatus => Target.IsAlive
            && !WasEvaded
            && (!WasBlocked || _damageReachedTarget);
        public int WeaknessValue => CaptureWeaknessValue();

        public bool ApplyStatus(BattleStatusInstance status)
        {
            return State.Statuses.ApplyAttackStatus(this, status);
        }

        internal void Evade()
        {
            Outcome = SkillHitOutcome.Evaded;
        }

        internal void RecordDamageInterception(
            BattleFieldEffectInstance fieldEffect,
            bool reachedTarget)
        {
            _damageWasResolved = true;
            _damageReachedTarget |= reachedTarget;
            if (fieldEffect != null)
            {
                InterceptedFieldEffect = fieldEffect;
            }
            if (!WasEvaded)
            {
                Outcome = _damageReachedTarget
                    ? SkillHitOutcome.Hit
                    : SkillHitOutcome.Blocked;
            }
        }

        internal void BlockStatus(BattleFieldEffectInstance fieldEffect = null)
        {
            if (fieldEffect != null)
            {
                InterceptedFieldEffect = fieldEffect;
            }
            if (!WasEvaded)
            {
                Outcome = SkillHitOutcome.Blocked;
            }
        }

        public bool TryMarkPassiveTriggered(int passiveId)
        {
            if (passiveId <= 0)
                throw new ArgumentOutOfRangeException(nameof(passiveId));
            return _triggeredPassiveIds.Add(passiveId);
        }

        private int CaptureWeaknessValue()
        {
            if (_weaknessCaptured) return _weaknessValue;
            _weaknessCaptured = true;
            var barrier = State.Fields.GetAttackBarrier(Source, Target);
            if (barrier != null)
            {
                InterceptedFieldEffect = barrier;
                if (State.Fields.TryConsumeStatus(
                        barrier,
                        BattleStatusId.Weakness,
                        out var fieldWeakness))
                {
                    _weaknessValue = fieldWeakness.Value;
                }
            }
            else if (State.Statuses.TryConsumeStatus(
                         Target,
                         BattleStatusId.Weakness,
                         out var weakness))
            {
                _weaknessValue = weakness.Value;
            }
            return _weaknessValue;
        }

        internal void Validate(
            BattleState state,
            BattleUnitState source,
            BattleUnitState intendedTarget)
        {
            if (!ReferenceEquals(State, state)
                || !ReferenceEquals(Source, source)
                || !ReferenceEquals(IntendedTarget, intendedTarget))
            {
                throw new ArgumentException(
                    "SkillHit does not belong to this damage application.");
            }
        }
    }
}
