using System;
using Pachimon.Reward;
using Pachimon.Trainer;

namespace Pachimon.Run
{
    public static class EnemyTrainerScalingService
    {
        public static void Apply(
            TrainerModifierSet modifiers,
            int rowIndex,
            TrainerProfile profile,
            EnemyTrainerScalingSettings settings = null)
        {
            if (modifiers == null) throw new ArgumentNullException(nameof(modifiers));
            if (rowIndex < 0) throw new ArgumentOutOfRangeException(nameof(rowIndex));

            settings ??= new EnemyTrainerScalingSettings();
            AddToAllStats(
                modifiers,
                checked(rowIndex * settings.StatPerRow),
                settings);

            if (profile == null)
            {
                return;
            }

            switch (profile.Role)
            {
                case TrainerRole.GymLeader:
                    AddAttribute(
                        modifiers,
                        profile.FavoredAttribute,
                        settings.GymFavoredAttributeBonus);
                    AddAttribute(
                        modifiers,
                        profile.WeakAttribute,
                        settings.GymWeakAttributePenalty);
                    break;
                case TrainerRole.Elite:
                    AddToAllStats(
                        modifiers,
                        settings.EliteAllStatBonus,
                        settings);
                    AddAttribute(
                        modifiers,
                        profile.FavoredAttribute,
                        settings.EliteFavoredAttributeBonus);
                    AddAttribute(
                        modifiers,
                        profile.WeakAttribute,
                        settings.EliteWeakAttributePenalty);
                    break;
            }
        }

        public static int PreserveMissingResource(
            int currentValue,
            int previousMaximum,
            int newMaximum)
        {
            if (previousMaximum < 0) throw new ArgumentOutOfRangeException(nameof(previousMaximum));
            if (newMaximum < 0) throw new ArgumentOutOfRangeException(nameof(newMaximum));

            var missing = Math.Max(0, previousMaximum - currentValue);
            return Math.Max(0, newMaximum - missing);
        }

        private static void AddToAllStats(
            TrainerModifierSet modifiers,
            int amount,
            EnemyTrainerScalingSettings settings)
        {
            for (var index = 0; index < (int)PachimonStatType.Count; index++)
            {
                var statType = (PachimonStatType)index;
                modifiers.AddStat(
                    statType,
                    settings.ScaleForStat(statType, amount));
            }
        }

        private static void AddAttribute(
            TrainerModifierSet modifiers,
            PachimonAttribute? attribute,
            int amount)
        {
            if (attribute.HasValue)
            {
                modifiers.AddStat(
                    PachimonStatTypeUtility.FromAttribute(attribute.Value),
                    amount);
            }
        }
    }
}
