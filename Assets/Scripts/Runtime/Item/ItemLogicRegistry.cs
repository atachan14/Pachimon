using System;
using System.Collections.Generic;

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
}
