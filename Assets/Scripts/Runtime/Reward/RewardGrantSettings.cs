using System;

namespace Pachimon.Reward
{
    [Obsolete("Use ModValueSettings instead.")]
    public sealed class RewardGrantSettings
    {
        public RewardGrantSettings(int regularStatAmount, int maxHpAmount)
        {
            RegularStatAmount = regularStatAmount;
            MaxHpAmount = maxHpAmount;
        }

        public int RegularStatAmount { get; }
        public int MaxHpAmount { get; }

        public static RewardGrantSettings Default { get; } = new(50, 500);
    }
}
