using System;
using Pachimon.Map;
using Pachimon.Reward;
using Pachimon.Trainer;

namespace Pachimon.Run
{
    public static class EnemyTrainerModifierFactory
    {
        public static TrainerModifierSet Create(MapNode node)
        {
            var modifiers = new TrainerModifierSet();
            switch (node?.Content)
            {
                case BattleNodeContent battle:
                    ApplyRewardTrainerStatus(modifiers, battle.NodeReward);
                    break;
                case GymNodeContent gym
                    when gym.NodeReward?.BadgeAttribute is PachimonAttribute attribute:
                    modifiers.AddBadge(attribute);
                    break;
                case EliteNodeContent:
                    foreach (PachimonAttribute eliteAttribute in
                             Enum.GetValues(typeof(PachimonAttribute)))
                    {
                        modifiers.AddBadge(eliteAttribute);
                    }
                    break;
            }

            var profile = node?.Content switch
            {
                BattleNodeContent battle => battle.TrainerProfile,
                GymNodeContent gym => gym.TrainerProfile,
                EliteNodeContent elite => elite.TrainerProfile,
                _ => null,
            };
            EnemyTrainerScalingService.Apply(
                modifiers,
                node?.RowIndex ?? 0,
                profile);
            return modifiers;
        }

        private static void ApplyRewardTrainerStatus(
            TrainerModifierSet modifiers,
            NodeReward reward)
        {
            if (reward == null)
            {
                return;
            }

            for (var index = 0; index < reward.Elements.Count; index++)
            {
                var element = reward.Elements[index];
                if (element == null || element.Kind == RewardElementKind.BonusGold)
                {
                    continue;
                }

                var statType = element.Kind switch
                {
                    RewardElementKind.Attribute when element.Attribute.HasValue =>
                        PachimonStatTypeUtility.FromAttribute(element.Attribute.Value),
                    RewardElementKind.MaxHp => PachimonStatType.MaxHp,
                    RewardElementKind.MaxMn => PachimonStatType.MaxMn,
                    RewardElementKind.Speed => PachimonStatType.Speed,
                    RewardElementKind.DamageBonus => PachimonStatType.DamageBonus,
                    RewardElementKind.ResistBonus => PachimonStatType.ResistBonus,
                    _ => PachimonStatType.Count,
                };
                if (statType == PachimonStatType.Count)
                {
                    continue;
                }

                modifiers.AddStat(
                    statType,
                    ModValueSettings.RuntimeDefault.GetAmount(
                        element.Kind,
                        index == 1));
            }
        }
    }

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
                checked(
                    settings.BaseStatAdjustment
                    + rowIndex * settings.StatPerRow),
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
                if (!PachimonStatTypeUtility.IsGeneratedStat(statType))
                {
                    continue;
                }
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
