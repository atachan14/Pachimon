using System;

namespace Pachimon.Items
{
    public sealed class ItemUseService
    {
        private readonly ItemCatalog _itemCatalog;
        private readonly ItemLogicRegistry _logicRegistry;

        public ItemUseService(
            ItemCatalog itemCatalog,
            ItemLogicRegistry logicRegistry = null)
        {
            _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
            _logicRegistry = logicRegistry ?? new ItemLogicRegistry(itemCatalog);
        }

        public ItemUseResult TryUse(
            ItemInventory inventory,
            string itemInstanceId,
            ItemUseContext context)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var itemInstance = inventory.Get(itemInstanceId);
            if (itemInstance == null)
            {
                return ItemUseResult.Failure(
                    ItemUseFailureReason.ItemNotOwned,
                    itemInstanceId,
                    context.TargetInstanceId);
            }

            var item = _itemCatalog.Get(itemInstance.ItemId);
            if (item == null)
            {
                return ItemUseResult.Failure(
                    ItemUseFailureReason.ItemDefinitionMissing,
                    itemInstanceId,
                    context.TargetInstanceId);
            }

            if (!_logicRegistry.TryGet(item.ItemId, out var logic))
            {
                return ItemUseResult.Failure(
                    ItemUseFailureReason.ItemLogicMissing,
                    itemInstanceId,
                    context.TargetInstanceId);
            }

            var failureReason = logic.CanUse(item, context);
            if (failureReason != ItemUseFailureReason.None)
            {
                return ItemUseResult.Failure(
                    failureReason,
                    itemInstanceId,
                    context.TargetInstanceId);
            }

            var effectAmount = logic.Apply(item, context);
            if (effectAmount <= 0)
            {
                return ItemUseResult.Failure(
                    ItemUseFailureReason.NoEffect,
                    itemInstanceId,
                    context.TargetInstanceId);
            }

            if (!inventory.TryRemove(itemInstanceId, out _))
            {
                throw new InvalidOperationException(
                    $"Used Item '{itemInstanceId}' could not be removed from Inventory.");
            }

            return ItemUseResult.Success(
                itemInstanceId,
                context.TargetInstanceId,
                effectAmount);
        }
    }
}
