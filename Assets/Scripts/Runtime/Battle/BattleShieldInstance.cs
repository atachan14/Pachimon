using System;

namespace Pachimon.Battle
{
    public sealed class BattleShieldInstance
    {
        internal BattleShieldInstance(
            long applicationOrder,
            int value,
            int? durationTicks)
        {
            if (applicationOrder < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(applicationOrder));
            }
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (durationTicks.HasValue && durationTicks.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }

            ApplicationOrder = applicationOrder;
            Value = value;
            RemainingTicks = durationTicks;
        }

        public long ApplicationOrder { get; }
        public int Value { get; private set; }
        public int? RemainingTicks { get; private set; }
        public bool IsExpired => Value <= 0 || RemainingTicks == 0;

        internal int Absorb(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            var absorbed = Math.Min(Value, damage);
            Value -= absorbed;
            return absorbed;
        }

        internal void Advance(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (RemainingTicks.HasValue)
            {
                RemainingTicks = Math.Max(0, RemainingTicks.Value - ticks);
            }
        }

        internal BattleShieldInstance CreateSimulationClone()
        {
            return new BattleShieldInstance(
                ApplicationOrder,
                Value,
                RemainingTicks);
        }
    }

    public readonly struct BattleShieldAbsorptionResult
    {
        public BattleShieldAbsorptionResult(
            int incomingDamage,
            int absorbedDamage)
        {
            IncomingDamage = incomingDamage;
            AbsorbedDamage = absorbedDamage;
        }

        public int IncomingDamage { get; }
        public int AbsorbedDamage { get; }
        public int RemainingDamage => IncomingDamage - AbsorbedDamage;
    }
}
