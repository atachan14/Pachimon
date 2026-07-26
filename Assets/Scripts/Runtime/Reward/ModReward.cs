using System;

namespace Pachimon.Reward
{
    public enum RewardElementKind
    {
        Attribute = 0,
        MaxHp = 1,
        MaxMn = 2,
        Speed = 3,
        DamageBonus = 4,
        ResistBonus = 5,
        BonusGold = 6,
    }

    public sealed class RewardElement
    {
        private RewardElement(RewardElementKind kind, PachimonAttribute? attribute)
        {
            Kind = kind;
            Attribute = attribute;
        }

        public RewardElementKind Kind { get; }
        public PachimonAttribute? Attribute { get; }

        public static RewardElement CreateAttribute(PachimonAttribute attribute)
        {
            return new RewardElement(RewardElementKind.Attribute, attribute);
        }

        public static RewardElement Create(RewardElementKind kind)
        {
            if (kind == RewardElementKind.Attribute)
            {
                throw new ArgumentException(
                    "Use an attribute factory for an attribute RewardElement.",
                    nameof(kind));
            }

            return new RewardElement(kind, null);
        }
    }
}
