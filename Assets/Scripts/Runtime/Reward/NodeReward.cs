using System;
using System.Collections.Generic;

namespace Pachimon.Reward
{
    public sealed class NodeReward
    {
        public NodeReward(
            int gold,
            RewardElement firstElement,
            RewardElement secondElement,
            PachimonAttribute? badgeAttribute)
        {
            if ((firstElement == null) != (secondElement == null))
            {
                throw new ArgumentException("Reward elements must be supplied as a pair.");
            }

            Gold = gold;
            FirstElement = firstElement;
            SecondElement = secondElement;
            Elements = firstElement == null
                ? Array.Empty<RewardElement>()
                : new[] { firstElement, secondElement };
            BadgeAttribute = badgeAttribute;
        }

        public int Gold { get; }
        public RewardElement FirstElement { get; }
        public RewardElement SecondElement { get; }
        public IReadOnlyList<RewardElement> Elements { get; }
        public PachimonAttribute? BadgeAttribute { get; }
        public bool IsBonusGold => FirstElement?.Kind == RewardElementKind.BonusGold
            || SecondElement?.Kind == RewardElementKind.BonusGold;
    }
}
