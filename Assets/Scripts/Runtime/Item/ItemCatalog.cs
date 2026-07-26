using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pachimon.Items
{
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Pachimon/Items/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private List<ItemAsset> _items = new();

        public IReadOnlyList<ItemAsset> Items => _items;

        public ItemAsset Get(int itemId)
        {
            return _items.FirstOrDefault(item => item != null && item.ItemId == itemId);
        }

        public IReadOnlyList<string> ValidateContent()
        {
            var errors = new List<string>();
            var validItems = _items.Where(item => item != null).ToArray();
            if (validItems.Length != _items.Count)
            {
                errors.Add("ItemCatalog contains a null entry.");
            }

            foreach (var duplicateId in validItems
                         .GroupBy(item => item.ItemId)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
            {
                errors.Add($"Duplicate Item ID: {duplicateId}");
            }

            foreach (var item in validItems)
            {
                item.CollectValidationErrors(errors);
            }

            return errors;
        }

#if UNITY_EDITOR
        public void SetItemsForEditor(IEnumerable<ItemAsset> items)
        {
            _items = new List<ItemAsset>(items);
        }
#endif
    }
}
