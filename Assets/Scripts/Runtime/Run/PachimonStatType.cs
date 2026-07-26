using System;
using Pachimon.Reward;

namespace Pachimon.Run
{
    public enum PachimonStatType
    {
        MaxHp = 0,
        MaxMn = 1,
        Fire = 2,
        Aqua = 3,
        Leaf = 4,
        Electric = 5,
        Poison = 6,
        Ice = 7,
        Wind = 8,
        Dragon = 9,
        Speed = 10,
        Haste = 11,
        DamageBonus = 12,
        ResistBonus = 13,
        Count = 14,
    }

    public static class PachimonStatTypeUtility
    {
        public static PachimonStatType FromAttribute(PachimonAttribute attribute)
        {
            return attribute switch
            {
                PachimonAttribute.Fire => PachimonStatType.Fire,
                PachimonAttribute.Aqua => PachimonStatType.Aqua,
                PachimonAttribute.Leaf => PachimonStatType.Leaf,
                PachimonAttribute.Electric => PachimonStatType.Electric,
                PachimonAttribute.Poison => PachimonStatType.Poison,
                PachimonAttribute.Ice => PachimonStatType.Ice,
                PachimonAttribute.Wind => PachimonStatType.Wind,
                PachimonAttribute.Dragon => PachimonStatType.Dragon,
                _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null),
            };
        }

        public static bool TryGetAttribute(
            PachimonStatType statType,
            out PachimonAttribute attribute)
        {
            if (statType < PachimonStatType.Fire || statType > PachimonStatType.Dragon)
            {
                attribute = default;
                return false;
            }

            attribute = (PachimonAttribute)(
                (int)statType - (int)PachimonStatType.Fire);
            return true;
        }

        public static bool IsResource(PachimonStatType statType)
        {
            return statType is PachimonStatType.MaxHp or PachimonStatType.MaxMn;
        }

        public static bool IsSpecialScaledStat(PachimonStatType statType)
        {
            return statType is PachimonStatType.Speed
                or PachimonStatType.Haste
                or PachimonStatType.DamageBonus
                or PachimonStatType.ResistBonus;
        }
    }
}
