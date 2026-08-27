using System;

namespace Pachimon.Run
{
    public sealed class PachimonSkillSlot
    {
        public PachimonSkillSlot(int slotId, int skillId, int upgradeLevel = 0)
        {
            if (slotId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotId));
            }

            if (skillId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skillId));
            }

            if (upgradeLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upgradeLevel));
            }

            SlotId = slotId;
            SkillId = skillId;
            UpgradeLevel = upgradeLevel;
        }

        public int SlotId { get; }

        public int SkillId { get; }

        public int UpgradeLevel { get; private set; }

        public void Upgrade()
        {
            UpgradeLevel = checked(UpgradeLevel + 1);
        }

        public PachimonSkillSlot CreateCopy()
        {
            return new PachimonSkillSlot(SlotId, SkillId, UpgradeLevel);
        }
    }

    public static class SkillUpgradeMath
    {
        public static decimal GetTimingMultiplier(int upgradeLevel)
        {
            ValidateLevel(upgradeLevel);
            var multiplier = 1m;
            for (var level = 0; level < upgradeLevel; level++)
            {
                multiplier *= 2m / 3m;
            }

            return multiplier;
        }

        public static decimal ScaleTiming(decimal baseValue, int upgradeLevel)
        {
            if (baseValue < 0m) throw new ArgumentOutOfRangeException(nameof(baseValue));
            return baseValue * GetTimingMultiplier(upgradeLevel);
        }

        public static decimal ScaleManaCost(decimal baseValue, int upgradeLevel)
        {
            if (baseValue < 0m) throw new ArgumentOutOfRangeException(nameof(baseValue));
            ValidateLevel(upgradeLevel);
            var value = baseValue;
            for (var level = 0; level < upgradeLevel; level++)
            {
                if (value > int.MaxValue / 1.5m)
                {
                    return int.MaxValue;
                }

                value *= 1.5m;
            }

            return value;
        }

        public static string FormatDisplayName(string displayName, int upgradeLevel)
        {
            return upgradeLevel > 0
                ? $"{displayName} +{upgradeLevel}"
                : displayName ?? string.Empty;
        }

        private static void ValidateLevel(int upgradeLevel)
        {
            if (upgradeLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(upgradeLevel));
            }
        }
    }
}
