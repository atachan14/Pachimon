using System;

namespace Pachimon.Run
{
    public sealed class EnemyTrainerScalingSettings
    {
        public EnemyTrainerScalingSettings(
            int statPerRow = 4,
            int resourceMultiplier = 5,
            int gymFavoredAttributeBonus = 100,
            int gymWeakAttributePenalty = -100,
            int eliteAllStatBonus = 100,
            int eliteFavoredAttributeBonus = 300,
            int eliteWeakAttributePenalty = -300)
        {
            if (statPerRow < 0) throw new ArgumentOutOfRangeException(nameof(statPerRow));
            if (resourceMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(resourceMultiplier));
            if (gymFavoredAttributeBonus < 0) throw new ArgumentOutOfRangeException(nameof(gymFavoredAttributeBonus));
            if (gymWeakAttributePenalty > 0) throw new ArgumentOutOfRangeException(nameof(gymWeakAttributePenalty));
            if (eliteAllStatBonus < 0) throw new ArgumentOutOfRangeException(nameof(eliteAllStatBonus));
            if (eliteFavoredAttributeBonus < 0) throw new ArgumentOutOfRangeException(nameof(eliteFavoredAttributeBonus));
            if (eliteWeakAttributePenalty > 0) throw new ArgumentOutOfRangeException(nameof(eliteWeakAttributePenalty));

            StatPerRow = statPerRow;
            ResourceMultiplier = resourceMultiplier;
            GymFavoredAttributeBonus = gymFavoredAttributeBonus;
            GymWeakAttributePenalty = gymWeakAttributePenalty;
            EliteAllStatBonus = eliteAllStatBonus;
            EliteFavoredAttributeBonus = eliteFavoredAttributeBonus;
            EliteWeakAttributePenalty = eliteWeakAttributePenalty;
        }

        public int StatPerRow { get; }
        public int ResourceMultiplier { get; }
        public int GymFavoredAttributeBonus { get; }
        public int GymWeakAttributePenalty { get; }
        public int EliteAllStatBonus { get; }
        public int EliteFavoredAttributeBonus { get; }
        public int EliteWeakAttributePenalty { get; }

        public int ScaleForStat(PachimonStatType statType, int amount)
        {
            return PachimonStatTypeUtility.IsResource(statType)
                ? checked(amount * ResourceMultiplier)
                : amount;
        }
    }
}
