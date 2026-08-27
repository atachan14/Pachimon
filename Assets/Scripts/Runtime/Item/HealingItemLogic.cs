using System;

namespace Pachimon.Items
{
    public sealed class HealingItemLogic : IItemLogic
    {
        public ItemUseFailureReason CanUse(
            ItemAsset item,
            ItemInstance itemInstance,
            ItemUseContext context)
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

            var isDefeated = context.CurrentHp <= 0;
            if (healingItem.DefeatedOnly && !isDefeated)
            {
                return ItemUseFailureReason.InvalidTarget;
            }

            if (isDefeated && !healingItem.CanRevive)
            {
                return ItemUseFailureReason.InvalidTarget;
            }

            var isFull = healingItem.ResourceType == RecoveryResourceType.Hp
                ? context.CurrentHp >= context.EffectiveMaxHp
                : context.CurrentMn >= context.EffectiveMaxMn;
            return isFull
                ? ItemUseFailureReason.NoEffect
                : ItemUseFailureReason.None;
        }

        public int Apply(
            ItemAsset item,
            ItemInstance itemInstance,
            ItemUseContext context)
        {
            if (item is not HealingItemAsset healingItem)
            {
                throw new ArgumentException(
                    "HealingItemLogic requires a HealingItemAsset.",
                    nameof(item));
            }

            var recoveryAmount = itemInstance?.GeneratedData.PrimaryEffectValue
                ?? healingItem.RecoveryAmount;
            return healingItem.ResourceType == RecoveryResourceType.Hp
                ? context.RestoreHp(recoveryAmount)
                : context.RestoreMn(recoveryAmount);
        }

        public int Apply(ItemAsset item, ItemUseContext context)
        {
            return Apply(item, null, context);
        }

    }
}
