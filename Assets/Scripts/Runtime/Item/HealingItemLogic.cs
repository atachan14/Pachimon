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

            if (context.CurrentHp <= 0 && !healingItem.CanRevive)
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

            var maximum = healingItem.ResourceType == RecoveryResourceType.Hp
                ? context.EffectiveMaxHp
                : context.EffectiveMaxMn;
            var recoveryAmount = CalculateRecoveryAmount(
                maximum,
                itemInstance?.GeneratedData.PrimaryEffectValue
                    ?? healingItem.RecoveryPercent);
            return healingItem.ResourceType == RecoveryResourceType.Hp
                ? context.RestoreHp(recoveryAmount)
                : context.RestoreMn(recoveryAmount);
        }

        public int Apply(ItemAsset item, ItemUseContext context)
        {
            return Apply(item, null, context);
        }

        private static int CalculateRecoveryAmount(int maximum, int percent)
        {
            if (maximum <= 0 || percent <= 0)
            {
                return 0;
            }

            return Math.Max(1, checked((int)((long)maximum * percent / 100)));
        }
    }
}
