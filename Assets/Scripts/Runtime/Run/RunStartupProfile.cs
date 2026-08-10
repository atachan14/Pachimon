using System.Collections.Generic;
using Pachimon.Items;
using UnityEngine;

namespace Pachimon.Run
{
    [CreateAssetMenu(
        fileName = "RunStartupProfile",
        menuName = "Pachimon/Run/Startup Profile")]
    public sealed class RunStartupProfile : ScriptableObject
    {
        [SerializeField, Min(0)] private int _startingGold = 100;
        [SerializeField] private List<ItemAsset> _startingItems =
            new(new ItemAsset[ItemInventory.Capacity]);

        public int StartingGold => _startingGold;
        public IReadOnlyList<ItemAsset> StartingItems => _startingItems;

        public IReadOnlyList<string> ValidateContent(ItemCatalog itemCatalog)
        {
            var errors = new List<string>();
            if (_startingItems.Count != ItemInventory.Capacity)
            {
                errors.Add(
                    $"Starting Items must contain exactly "
                    + $"{ItemInventory.Capacity} slots.");
            }

            for (var index = 0; index < _startingItems.Count; index++)
            {
                var item = _startingItems[index];
                if (item == null)
                {
                    continue;
                }
                if (itemCatalog == null
                    || !ReferenceEquals(
                        itemCatalog.Get(item.ItemId),
                        item))
                {
                    errors.Add(
                        $"Starting Item '{item.DisplayName}' is not "
                        + "registered in the selected ItemCatalog.");
                }
            }

            return errors;
        }
    }

}
