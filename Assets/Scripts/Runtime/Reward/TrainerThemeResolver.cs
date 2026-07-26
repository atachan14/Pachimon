using System;
using Pachimon.Trainer;

namespace Pachimon.Reward
{
    public static class TrainerThemeResolver
    {
        public static TrainerTheme FromBattleReward(NodeReward reward)
        {
            if (reward == null)
            {
                throw new ArgumentNullException(nameof(reward));
            }

            var element = reward.FirstElement;
            if (element == null)
            {
                throw new InvalidOperationException("Battle Reward has no first RewardElement.");
            }

            return element.Kind switch
            {
                RewardElementKind.Attribute => FromAttribute(
                    element.Attribute
                    ?? throw new InvalidOperationException("Attribute Reward has no attribute.")),
                RewardElementKind.Speed => TrainerTheme.Speed,
                RewardElementKind.MaxHp => TrainerTheme.MaxHp,
                RewardElementKind.MaxMn => TrainerTheme.MaxMn,
                RewardElementKind.BonusGold => TrainerTheme.Gold,
                RewardElementKind.DamageBonus => TrainerTheme.DamageBonus,
                RewardElementKind.ResistBonus => TrainerTheme.ResistBonus,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static TrainerTheme FromAttribute(PachimonAttribute attribute)
        {
            return attribute switch
            {
                PachimonAttribute.Fire => TrainerTheme.Fire,
                PachimonAttribute.Aqua => TrainerTheme.Aqua,
                PachimonAttribute.Leaf => TrainerTheme.Leaf,
                PachimonAttribute.Electric => TrainerTheme.Electric,
                PachimonAttribute.Poison => TrainerTheme.Poison,
                PachimonAttribute.Wind => TrainerTheme.Wind,
                PachimonAttribute.Ice => TrainerTheme.Ice,
                PachimonAttribute.Dragon => TrainerTheme.Dragon,
                _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null),
            };
        }
    }
}
