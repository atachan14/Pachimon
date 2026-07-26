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
            int damage)
            : base(state, source, target)
        {
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
            Attribute = attribute;
            Damage = damage;
        }

        public PachimonAttribute Attribute { get; }
        public int Damage { get; private set; }

        public void MultiplyDamage(int percent)
        {
            if (percent < 0) throw new ArgumentOutOfRangeException(nameof(percent));
            var multiplied = ((long)Damage * percent) / 100L;
            Damage = (int)Math.Max(1L, Math.Min(multiplied, int.MaxValue));
        }
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
