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

            var hpFull = context.CurrentHp >= context.EffectiveMaxHp;
            var mnFull = context.CurrentMn >= context.EffectiveMaxMn;
            var isFull = healingItem.ResourceType switch
            {
                RecoveryResourceType.Hp => hpFull,
                RecoveryResourceType.Mn => mnFull,
                RecoveryResourceType.HpAndMn => hpFull && mnFull,
                _ => throw new ArgumentOutOfRangeException(),
            };
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

            var recoveryValue = itemInstance?.GeneratedData.PrimaryEffectValue
                ?? healingItem.RecoveryAmount;
            var hpAmount = GetRecoveryAmount(
                healingItem,
                recoveryValue,
                context.EffectiveMaxHp);
            var mnAmount = GetRecoveryAmount(
                healingItem,
                recoveryValue,
                context.EffectiveMaxMn);
            return healingItem.ResourceType switch
            {
                RecoveryResourceType.Hp => context.RestoreHp(hpAmount),
                RecoveryResourceType.Mn => context.RestoreMn(mnAmount),
                RecoveryResourceType.HpAndMn => checked(
                    context.RestoreHp(hpAmount)
                    + context.RestoreMn(mnAmount)),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public int Apply(ItemAsset item, ItemUseContext context)
        {
            return Apply(item, null, context);
        }

        private static int GetRecoveryAmount(
            HealingItemAsset item,
            int recoveryValue,
            int maximumValue)
        {
            return item.ValueMode == RecoveryValueMode.MaximumPercent
                ? Math.Max(1, checked(maximumValue * recoveryValue) / 100)
                : recoveryValue;
        }

    }
}
