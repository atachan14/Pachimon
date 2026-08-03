using System.Collections.Generic;
using Pachimon.Reward;

namespace Pachimon.Run
{
    public static class TrainerStatModifierFactory
    {
        public static IReadOnlyList<IStatModifier> Create(
            TrainerModifierSet modifiers)
        {
            if (modifiers == null)
            {
                return System.Array.Empty<IStatModifier>();
            }

            var result = new List<IStatModifier>();
            for (var index = 0; index < (int)PachimonStatType.Count; index++)
            {
                var statType = (PachimonStatType)index;
                var addition = modifiers.GetStatAddition(statType);
                if (addition == 0)
                {
                    continue;
                }

                result.Add(new FixedStatModifier(
                    statType,
                    StatModifierOperation.DirectAdditive,
                    addition,
                    new StatModifierSource(
                        StatModifierSourceType.TrainerMod,
                        $"trainer-mod:{statType}",
                        "Mod")));
            }

            for (var index = 0; index <= (int)PachimonAttribute.Dragon; index++)
            {
                var attribute = (PachimonAttribute)index;
                var badgeCount = modifiers.GetBadgeCount(attribute);
                if (badgeCount == 0)
                {
                    continue;
                }

                result.Add(new FixedStatModifier(
                    PachimonStatTypeUtility.FromAttribute(attribute),
                    StatModifierOperation.DirectMultiplicative,
                    modifiers.GetAttributeMultiplierPercent(attribute) / 100m,
                    new StatModifierSource(
                        StatModifierSourceType.Badge,
                        $"badge:{attribute}",
                        $"{attribute} Badge x{badgeCount}")));
            }

            return result;
        }
    }
}
