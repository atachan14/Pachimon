using System;
using Pachimon.Reward;
using UnityEngine;

namespace Pachimon.Trainer
{
    public static class TrainerColorSchemeResolver
    {
        public static TrainerColorScheme FromBattleReward(NodeReward reward)
        {
            if (reward == null) throw new ArgumentNullException(nameof(reward));
            if (reward.FirstElement == null || reward.SecondElement == null)
            {
                throw new ArgumentException(
                    "Battle Reward requires first and second elements.",
                    nameof(reward));
            }

            return new TrainerColorScheme(
                RewardElementPalette.GetColor(reward.FirstElement),
                RewardElementPalette.GetColor(reward.SecondElement));
        }

        public static TrainerColorScheme FromAttribute(PachimonAttribute attribute)
        {
            var color = RewardElementPalette.GetAttributeColor(attribute);
            return new TrainerColorScheme(color, color);
        }

        public static bool TryFromTheme(TrainerTheme theme, out TrainerColorScheme colors)
        {
            if (TryGetThemeColor(theme, out var color))
            {
                colors = new TrainerColorScheme(color, color);
                return true;
            }

            colors = default;
            return false;
        }

        private static bool TryGetThemeColor(TrainerTheme theme, out Color color)
        {
            switch (theme)
            {
                case TrainerTheme.Fire:
                    color = RewardElementPalette.GetAttributeColor(PachimonAttribute.Fire);
                    return true;
                case TrainerTheme.Aqua:
                    color = RewardElementPalette.GetAttributeColor(PachimonAttribute.Aqua);
                    return true;
                case TrainerTheme.Leaf:
                    color = RewardElementPalette.GetAttributeColor(PachimonAttribute.Leaf);
                    return true;
                case TrainerTheme.Electric:
                    color = RewardElementPalette.GetAttributeColor(PachimonAttribute.Electric);
                    return true;
                case TrainerTheme.Poison:
                    color = RewardElementPalette.GetAttributeColor(PachimonAttribute.Poison);
                    return true;
                case TrainerTheme.Wind:
                    color = RewardElementPalette.GetAttributeColor(PachimonAttribute.Wind);
                    return true;
                case TrainerTheme.Ice:
                    color = RewardElementPalette.GetAttributeColor(PachimonAttribute.Ice);
                    return true;
                case TrainerTheme.Dragon:
                    color = RewardElementPalette.GetAttributeColor(PachimonAttribute.Dragon);
                    return true;
                default:
                    color = default;
                    return false;
            }
        }
    }
}
