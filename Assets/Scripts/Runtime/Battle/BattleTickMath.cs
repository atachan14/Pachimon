using System;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public static class BattleTickMath
    {
        public static decimal GetProgressPerTick(int timingStat)
        {
            return 1m / SignedStatMath.ReductionMultiplier(timingStat);
        }

        public static int GetTicksToComplete(
            decimal remainingWork,
            int timingStat)
        {
            if (remainingWork <= 0m)
            {
                return 0;
            }

            return SignedStatMath.CeilPositive(
                remainingWork / GetProgressPerTick(timingStat));
        }

        public static int GetEffectiveStartup(int baseStartup, int speed)
        {
            return GetEffectiveStartup(baseStartup, speed, 1m);
        }

        public static int GetEffectiveStartup(
            int baseStartup,
            int speed,
            decimal skillMultiplier)
        {
            if (baseStartup < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseStartup));
            }

            return baseStartup == 0
                ? 0
                : ApplyTimingStat(baseStartup, speed, skillMultiplier);
        }

        public static int GetEffectiveRecovery(int baseRecovery, int speed)
        {
            return GetEffectiveRecovery(baseRecovery, speed, 1m);
        }

        public static int GetEffectiveRecovery(
            int baseRecovery,
            int speed,
            decimal skillMultiplier)
        {
            if (baseRecovery < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRecovery));
            }

            return baseRecovery == 0
                ? 0
                : ApplyTimingStat(baseRecovery, speed, skillMultiplier);
        }

        public static int GetEffectiveCooldown(int baseCooldown, int haste)
        {
            return GetEffectiveCooldown(baseCooldown, haste, 1m);
        }

        public static int GetEffectiveCooldown(
            int baseCooldown,
            int haste,
            decimal skillMultiplier)
        {
            if (baseCooldown < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseCooldown));
            }

            return baseCooldown == 0
                ? 0
                : ApplyTimingStat(baseCooldown, haste, skillMultiplier);
        }

        private static int ApplyTimingStat(
            int baseTicks,
            int stat,
            decimal skillMultiplier)
        {
            if (skillMultiplier <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(skillMultiplier));
            }

            var unroundedTicks = baseTicks
                * SignedStatMath.ReductionMultiplier(stat)
                * skillMultiplier;
            return SignedStatMath.CeilPositive(unroundedTicks);
        }
    }
}
