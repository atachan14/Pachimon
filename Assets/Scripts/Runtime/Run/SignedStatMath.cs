using System;

namespace Pachimon.Run
{
    public static class SignedStatMath
    {
        public static decimal AmplificationMultiplier(int stat)
        {
            return AmplificationMultiplier((decimal)stat);
        }

        public static decimal AmplificationMultiplier(decimal stat)
        {
            return stat >= 0
                ? 1m + stat / 100m
                : 100m / (100m - stat);
        }

        public static decimal ScaleFromBase(
            decimal baseValue,
            decimal stat,
            decimal scalingPercent = 100m)
        {
            if (baseValue < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(baseValue));
            }

            return baseValue * AmplificationMultiplier(
                stat * scalingPercent / 100m);
        }

        public static decimal ReductionMultiplier(int stat)
        {
            return ReductionMultiplier((decimal)stat);
        }

        public static decimal ReductionMultiplier(decimal stat)
        {
            return stat >= 0
                ? 100m / (100m + stat)
                : 1m + (-stat / 100m);
        }

        public static int FloorStat(decimal value, bool clampToNonNegative)
        {
            if (clampToNonNegative && value <= 0m)
            {
                return 0;
            }

            return FloorToInt(value);
        }

        public static int FloorNonNegative(decimal value, int minimum = 0)
        {
            if (minimum < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum));
            }

            if (value <= minimum)
            {
                return minimum;
            }

            return Math.Max(minimum, FloorToInt(value));
        }

        public static int CeilPositive(decimal value)
        {
            if (value <= 0m)
            {
                return 0;
            }

            if (value >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return Math.Max(1, decimal.ToInt32(decimal.Ceiling(value)));
        }

        private static int FloorToInt(decimal value)
        {
            if (value >= int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value <= int.MinValue)
            {
                return int.MinValue;
            }

            return decimal.ToInt32(decimal.Floor(value));
        }
    }
}
