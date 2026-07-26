using System;
using Pachimon.Reward;

namespace Pachimon.Run
{
    public sealed class TrainerModifierSet
    {
        public const int BadgeBonusPercentPerBadge = 30;

        private readonly int[] _statAdditions = new int[(int)PachimonStatType.Count];
        private readonly int[] _badgeCounts = new int[(int)PachimonAttribute.Dragon + 1];

        public int GetStatAddition(PachimonStatType statType)
        {
            return _statAdditions[GetStatIndex(statType)];
        }

        public void AddStat(PachimonStatType statType, int amount)
        {
            var index = GetStatIndex(statType);
            _statAdditions[index] = AddChecked(_statAdditions[index], amount);
        }

        public int GetBadgeCount(PachimonAttribute attribute)
        {
            return _badgeCounts[GetAttributeIndex(attribute)];
        }

        public int GetAttributeMultiplierPercent(PachimonAttribute attribute)
        {
            return checked(
                100 + GetBadgeCount(attribute) * BadgeBonusPercentPerBadge);
        }

        public void AddBadge(PachimonAttribute attribute)
        {
            var index = GetAttributeIndex(attribute);
            _badgeCounts[index] = AddChecked(_badgeCounts[index], 1);
        }

        public EffectivePachimonStats ApplyTo(PachimonStats baseStats)
        {
            return new EffectivePachimonStats(baseStats, this);
        }

        private static int GetStatIndex(PachimonStatType statType)
        {
            var index = (int)statType;
            if (index < 0 || index >= (int)PachimonStatType.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(statType));
            }

            return index;
        }

        private static int GetAttributeIndex(PachimonAttribute attribute)
        {
            var index = (int)attribute;
            if (index < 0 || index > (int)PachimonAttribute.Dragon)
            {
                throw new ArgumentOutOfRangeException(nameof(attribute));
            }

            return index;
        }

        private static int AddChecked(int current, int amount)
        {
            var result = (long)current + amount;
            if (result > int.MaxValue || result < int.MinValue)
            {
                throw new OverflowException("Trainer modifier value exceeded the Int32 range.");
            }

            return (int)result;
        }
    }
}
