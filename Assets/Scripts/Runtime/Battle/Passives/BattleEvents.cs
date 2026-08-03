using System;
using Pachimon.Reward;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public interface IBattleEvent
    {
        BattleState State { get; }
        BattleUnitState Source { get; }
        BattleUnitState Target { get; }
    }

    public abstract class BattleEvent : IBattleEvent
    {
        protected BattleEvent(
            BattleState state,
            BattleUnitState source = null,
            BattleUnitState target = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Source = source;
            Target = target;
        }

        public BattleState State { get; }
        public BattleUnitState Source { get; }
        public BattleUnitState Target { get; }
    }

    public sealed class BattleStartedEvent : BattleEvent
    {
        public BattleStartedEvent(BattleState state) : base(state) { }
    }

    public sealed class BeforeSkillEvent : BattleEvent
    {
        public BeforeSkillEvent(
            BattleState state,
            BattleUnitState source,
            SkillAsset skill)
            : base(state, source)
        {
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillAsset Skill { get; }
    }

    public sealed class BeforeAttributeDamageEvent : BattleEvent
    {
        public BeforeAttributeDamageEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            PachimonAttribute attribute,
            decimal unroundedDamage)
            : base(state, source, target)
        {
            if (unroundedDamage < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(unroundedDamage));
            }

            Attribute = attribute;
            UnroundedDamage = unroundedDamage;
        }

        public BeforeAttributeDamageEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageCalculationResult calculation)
            : this(
                state,
                source,
                target,
                calculation?.Context.Attribute
                    ?? throw new ArgumentNullException(nameof(calculation)),
                calculation.UnroundedDamage)
        {
            Calculation = calculation;
        }

        public PachimonAttribute Attribute { get; }
        public DamageCalculationResult Calculation { get; }
        public decimal UnroundedDamage { get; private set; }

        public void MultiplyDamage(int percent)
        {
            if (percent < 0) throw new ArgumentOutOfRangeException(nameof(percent));
            UnroundedDamage *= percent / 100m;
        }
    }

    public sealed class AttributeDamageAppliedEvent : BattleEvent
    {
        public AttributeDamageAppliedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageCalculationResult calculation,
            int finalDamage,
            int appliedDamage)
            : base(state, source, target)
        {
            Calculation = calculation
                ?? throw new ArgumentNullException(nameof(calculation));
            if (finalDamage < 0) throw new ArgumentOutOfRangeException(nameof(finalDamage));
            if (appliedDamage < 0) throw new ArgumentOutOfRangeException(nameof(appliedDamage));
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
        }

        public DamageCalculationResult Calculation { get; }
        public PachimonAttribute Attribute => Calculation.Context.Attribute;
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
    }

    public sealed class AttackReceivedEvent : BattleEvent
    {
        public AttackReceivedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageOriginKind originKind,
            int originId,
            bool isTrueDamage,
            PachimonAttribute? attribute,
            int finalDamage,
            int appliedDamage)
            : base(state, source, target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (originId <= 0) throw new ArgumentOutOfRangeException(nameof(originId));
            if (finalDamage < 0) throw new ArgumentOutOfRangeException(nameof(finalDamage));
            if (appliedDamage < 0) throw new ArgumentOutOfRangeException(nameof(appliedDamage));

            OriginKind = originKind;
            OriginId = originId;
            IsTrueDamage = isTrueDamage;
            Attribute = attribute;
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
        }

        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
        public bool IsTrueDamage { get; }
        public PachimonAttribute? Attribute { get; }
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
    }

    public sealed class SkillResolvedEvent : BattleEvent
    {
        public SkillResolvedEvent(BattleState state, SkillResolution resolution)
            : base(state, resolution?.User)
        {
            Resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
        }

        public SkillResolution Resolution { get; }
    }

    public sealed class UnitDefeatedEvent : BattleEvent
    {
        public UnitDefeatedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState defeatedUnit)
            : base(state, source, defeatedUnit)
        {
            if (defeatedUnit == null) throw new ArgumentNullException(nameof(defeatedUnit));
        }

        public BattleUnitState DefeatedUnit => Target;
    }

    public sealed class BattleEndedEvent : BattleEvent
    {
        public BattleEndedEvent(BattleState state, BattleOutcome outcome)
            : base(state)
        {
            if (outcome == BattleOutcome.Undecided)
            {
                throw new ArgumentException("Battle outcome is not decided.", nameof(outcome));
            }

            Outcome = outcome;
        }

        public BattleOutcome Outcome { get; }
    }
}
