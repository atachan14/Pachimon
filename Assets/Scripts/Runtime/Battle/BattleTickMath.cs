using System;

namespace Pachimon.Battle
{
    public static class BattleTickMath
    {
        public static int GetEffectiveTurnCost(int baseTurnCost, int speed)
        {
            if (baseTurnCost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseTurnCost));
            }

            return ApplySpeed(baseTurnCost, speed);
        }

        public static int GetEffectiveCooldown(int baseCooldown, int haste)
        {
            if (baseCooldown < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseCooldown));
            }

            if (haste < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(haste));
            }

            return baseCooldown == 0 ? 0 : ApplySpeed(baseCooldown, haste);
        }

        private static int ApplySpeed(int baseTicks, int speed)
        {
            if (speed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }

            var denominator = 100L + speed;
            var numerator = (long)baseTicks * 100L;
            var result = (numerator + denominator - 1L) / denominator;
            return (int)Math.Max(1L, Math.Min(result, int.MaxValue));
        }
    }
}
