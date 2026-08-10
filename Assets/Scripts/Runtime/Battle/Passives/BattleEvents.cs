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

    public sealed class BeforeFieldEffectValueAppliedEvent : BattleEvent
    {
        public BeforeFieldEffectValueAppliedEvent(
            BattleState state,
            BattleUnitState source,
            BattleFieldEffectId effectId,
            BattleSide targetSide,
            int value)
            : base(state, source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            EffectId = effectId;
            TargetSide = targetSide;
            Value = value;
        }

        public BattleFieldEffectId EffectId { get; }
        public BattleSide TargetSide { get; }
        public int Value { get; private set; }

        public void SetValue(int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
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
        public decimal OutgoingMultiplier { get; private set; } = 1m;

        public void MultiplyDamage(int percent)
        {
            if (percent < 0) throw new ArgumentOutOfRangeException(nameof(percent));
            MultiplyDamage(percent / 100m);
        }

        public void MultiplyDamage(decimal multiplier)
        {
            if (multiplier < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier));
            }
            UnroundedDamage *= multiplier;
            OutgoingMultiplier *= multiplier;
        }
    }

    public sealed class BeforeWeatherDecayEvent : BattleEvent
    {
        public BeforeWeatherDecayEvent(
            BattleState state,
            BattleWeatherInstance weather,
            decimal decayPerTick)
            : base(state, weather?.Source)
        {
            Weather = weather ?? throw new ArgumentNullException(nameof(weather));
            if (decayPerTick < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(decayPerTick));
            }
            DecayPerTick = decayPerTick;
        }

        public BattleWeatherInstance Weather { get; }
        public decimal DecayPerTick { get; private set; }

        public void MultiplyDecay(int percent)
        {
            if (percent < 0) throw new ArgumentOutOfRangeException(nameof(percent));
            DecayPerTick *= percent / 100m;
        }
    }

    public sealed class AttributeDamageAppliedEvent : BattleEvent
    {
        public AttributeDamageAppliedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageCalculationResult calculation,
            decimal preDefenseDamage,
            int finalDamage,
            int appliedDamage,
            int shieldAbsorbedDamage = 0)
            : base(state, source, target)
        {
            Calculation = calculation
                ?? throw new ArgumentNullException(nameof(calculation));
            if (preDefenseDamage < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(preDefenseDamage));
            }
            if (finalDamage < 0) throw new ArgumentOutOfRangeException(nameof(finalDamage));
            if (appliedDamage < 0) throw new ArgumentOutOfRangeException(nameof(appliedDamage));
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
            PreDefenseDamage = preDefenseDamage;
        }

        public DamageCalculationResult Calculation { get; }
        public PachimonAttribute Attribute => Calculation.Context.Attribute;
        public decimal PreDefenseDamage { get; }
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
        public int ShieldAbsorbedDamage { get; }
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
            int appliedDamage,
            int shieldAbsorbedDamage = 0)
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
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
        }

        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
        public bool IsTrueDamage { get; }
        public PachimonAttribute? Attribute { get; }
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
        public int ShieldAbsorbedDamage { get; }
    }

    public sealed class AttackEvadedEvent : BattleEvent
    {
        public AttackEvadedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageOriginKind originKind,
            int originId)
            : base(state, source, target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (originId <= 0) throw new ArgumentOutOfRangeException(nameof(originId));
            OriginKind = originKind;
            OriginId = originId;
        }

        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
    }

    public sealed class StatusDamageAppliedEvent : BattleEvent
    {
        public StatusDamageAppliedEvent(
            BattleState state,
            BattleUnitState target,
            BattleStatusId statusId,
            PachimonAttribute attribute,
            int finalDamage,
            int appliedDamage,
            int shieldAbsorbedDamage = 0)
            : base(state, source: null, target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (finalDamage < 0) throw new ArgumentOutOfRangeException(nameof(finalDamage));
            if (appliedDamage < 0) throw new ArgumentOutOfRangeException(nameof(appliedDamage));
            StatusId = statusId;
            Attribute = attribute;
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
        }

        public BattleStatusId StatusId { get; }
        public PachimonAttribute Attribute { get; }
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
        public int ShieldAbsorbedDamage { get; }
    }

    public sealed class DamageAppliedEvent : BattleEvent
    {
        public DamageAppliedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageOriginKind originKind,
            int originId,
            bool isTrueDamage,
            PachimonAttribute? attribute,
            int finalDamage,
            int appliedDamage,
            int shieldAbsorbedDamage)
            : base(state, source, target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (originId <= 0) throw new ArgumentOutOfRangeException(nameof(originId));
            if (finalDamage < 0) throw new ArgumentOutOfRangeException(nameof(finalDamage));
            if (appliedDamage < 0) throw new ArgumentOutOfRangeException(nameof(appliedDamage));
            if (shieldAbsorbedDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(shieldAbsorbedDamage));
            }

            OriginKind = originKind;
            OriginId = originId;
            IsTrueDamage = isTrueDamage;
            Attribute = attribute;
            FinalDamage = finalDamage;
            AppliedDamage = appliedDamage;
            ShieldAbsorbedDamage = shieldAbsorbedDamage;
        }

        public DamageOriginKind OriginKind { get; }
        public int OriginId { get; }
        public bool IsTrueDamage { get; }
        public PachimonAttribute? Attribute { get; }
        public int FinalDamage { get; }
        public int AppliedDamage { get; }
        public int ShieldAbsorbedDamage { get; }
        public int ReceivedDamage => checked(AppliedDamage + ShieldAbsorbedDamage);
    }

    public sealed class ToxinAppliedEvent : BattleEvent
    {
        public ToxinAppliedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            int appliedValue)
            : base(state, source, target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (appliedValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(appliedValue));
            }

            AppliedValue = appliedValue;
        }

        public int AppliedValue { get; }
    }

    public sealed class StatusValueAppliedEvent : BattleEvent
    {
        public StatusValueAppliedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            BattleStatusId statusId,
            int appliedValue)
            : base(state, source, target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (appliedValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(appliedValue));
            }
            StatusId = statusId;
            AppliedValue = appliedValue;
        }

        public BattleStatusId StatusId { get; }
        public int AppliedValue { get; }
    }

    public sealed class SkillStatusAppliedEvent : BattleEvent
    {
        public SkillStatusAppliedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            BattleStatusId statusId,
            int appliedValue)
            : base(state, source, target)
        {
            if (appliedValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(appliedValue));
            }
            StatusId = statusId;
            AppliedValue = appliedValue;
        }

        public BattleStatusId StatusId { get; }
        public int AppliedValue { get; }
    }

    public sealed class ShieldAppliedEvent : BattleEvent
    {
        public ShieldAppliedEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            int appliedValue,
            int? durationTicks,
            bool isSharedEffect)
            : base(state, source, target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (appliedValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(appliedValue));
            }

            AppliedValue = appliedValue;
            DurationTicks = durationTicks;
            IsSharedEffect = isSharedEffect;
        }

        public int AppliedValue { get; }
        public int? DurationTicks { get; }
        public bool IsSharedEffect { get; }
    }

    public sealed class HpRestoredEvent : BattleEvent
    {
        public HpRestoredEvent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            int restoredValue,
            bool isSharedEffect)
            : base(state, source, target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (restoredValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(restoredValue));
            }

            RestoredValue = restoredValue;
            IsSharedEffect = isSharedEffect;
        }

        public int RestoredValue { get; }
        public bool IsSharedEffect { get; }
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

    public sealed class ChainResolvedEvent : BattleEvent
    {
        public ChainResolvedEvent(
            BattleState state,
            BattleUnitState source,
            SkillAsset skill,
            int completedAdditionalChainCount)
            : base(state, source)
        {
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            if (completedAdditionalChainCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedAdditionalChainCount));
            }

            CompletedAdditionalChainCount = completedAdditionalChainCount;
        }

        public SkillAsset Skill { get; }
        public int CompletedAdditionalChainCount { get; }
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
