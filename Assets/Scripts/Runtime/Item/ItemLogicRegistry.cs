using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Items
{
    public sealed class ItemLogicRegistry
    {
        private readonly Dictionary<int, IItemLogic> _logicByItemId = new();

        public ItemLogicRegistry(ItemCatalog itemCatalog)
        {
            if (itemCatalog == null) throw new ArgumentNullException(nameof(itemCatalog));
            foreach (var item in itemCatalog.Items)
            {
                if (item is HealingItemAsset)
                {
                    _logicByItemId[item.ItemId] = new HealingItemLogic();
                }
                else if (item is DamageItemAsset)
                {
                    _logicByItemId[item.ItemId] = new DirectDamageItemLogic();
                }
                else if (item is SkillMachineItemAsset)
                {
                    _logicByItemId[item.ItemId] = new SkillMachineItemLogic();
                }
                else if (item is EngravingItemAsset)
                {
                    _logicByItemId[item.ItemId] = new EngravingItemLogic();
                }
            }
        }

        public bool TryGet(int itemId, out IItemLogic logic)
        {
            return _logicByItemId.TryGetValue(itemId, out logic);
        }

        public void RegisterOrReplace(int itemId, IItemLogic logic)
        {
            if (itemId <= 0) throw new ArgumentOutOfRangeException(nameof(itemId));
            _logicByItemId[itemId] = logic ?? throw new ArgumentNullException(nameof(logic));
        }
    }

    public sealed class EngravingItemLogic : IItemLogic
    {
        public ItemUseFailureReason CanUse(
            ItemAsset item,
            ItemInstance itemInstance,
            ItemUseContext context)
        {
            if (item is not EngravingItemAsset
                || itemInstance?.GeneratedData?.StatChanges == null
                || itemInstance.GeneratedData.StatChanges.Count != 2
                || context?.Affiliation != ItemTargetAffiliation.Ally
                || context.RunTarget == null)
            {
                return ItemUseFailureReason.InvalidTarget;
            }

            return itemInstance.GeneratedData.StatChanges.Any(
                change => change.Amount > 0)
                && itemInstance.GeneratedData.StatChanges.Any(
                    change => change.Amount < 0)
                    ? ItemUseFailureReason.None
                    : ItemUseFailureReason.InvalidTarget;
        }

        public int Apply(
            ItemAsset item,
            ItemInstance itemInstance,
            ItemUseContext context)
        {
            context.ApplyPermanentStatChanges(
                itemInstance.GeneratedData.StatChanges,
                $"item:{itemInstance.InstanceId}",
                item.DisplayName);
            return itemInstance.GeneratedData.StatChanges
                .Where(change => change.Amount > 0)
                .Sum(change => change.Amount);
        }
    }
}
