using System;

namespace Pachimon.Items
{
    public sealed class HealingItemLogic : IItemLogic
    {
        public ItemUseFailureReason CanUse(ItemAsset item, ItemUseContext context)
        {
            if (item is not HealingItemAsset healingItem)
            {
                throw new ArgumentException(
                    "HealingItemLogic requires a HealingItemAsset.",
                    nameof(item));
            }

            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Affiliation != ItemTargetAffiliation.Ally)
            {
                return ItemUseFailureReason.InvalidTarget;
            }

            if (context.CurrentHp <= 0 && !healingItem.CanRevive)
            {
                return ItemUseFailureReason.InvalidTarget;
            }

            return context.CurrentHp >= context.EffectiveMaxHp
                ? ItemUseFailureReason.NoEffect
                : ItemUseFailureReason.None;
        }

        public int Apply(ItemAsset item, ItemUseContext context)
        {
            if (item is not HealingItemAsset healingItem)
            {
                throw new ArgumentException(
                    "HealingItemLogic requires a HealingItemAsset.",
                    nameof(item));
            }

            return context.RestoreHp(healingItem.HealAmount);
        }
    }
}
