using System;

namespace Pachimon.Items
{
    public sealed class DirectDamageItemLogic : IItemLogic
    {
        public ItemUseFailureReason CanUse(
            ItemAsset item,
            ItemInstance itemInstance,
            ItemUseContext context)
        {
            if (item is not DamageItemAsset)
            {
                throw new ArgumentException(
                    "DirectDamageItemLogic requires a DamageItemAsset.",
                    nameof(item));
            }

            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Affiliation != ItemTargetAffiliation.Enemy)
            {
                return ItemUseFailureReason.InvalidTarget;
            }

            return context.CurrentHp > 0
                ? ItemUseFailureReason.None
                : ItemUseFailureReason.NoEffect;
        }

        public int Apply(
            ItemAsset item,
            ItemInstance itemInstance,
            ItemUseContext context)
        {
            if (item is not DamageItemAsset damageItem)
            {
                throw new ArgumentException(
                    "DirectDamageItemLogic requires a DamageItemAsset.",
                    nameof(item));
            }

            return context.ApplyDamage(damageItem.DamageAmount, item.ItemId);
        }
    }
}
