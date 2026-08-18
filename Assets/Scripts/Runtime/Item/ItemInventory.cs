using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Items
{
    public sealed class ItemInventory
    {
        public const int Capacity = 9;

        private readonly ItemInstance[] _slots = new ItemInstance[Capacity];
        private int _nextInstanceNumber = 1;

        public IReadOnlyList<ItemInstance> Slots => _slots;
        public int Count => _slots.Count(item => item != null);
        public bool IsFull => Count >= Capacity;
        public event Action Changed;

        public ItemInstance GetAt(int slotIndex)
        {
            ValidateSlotIndex(slotIndex);
            return _slots[slotIndex];
        }

        public ItemInstance Get(string instanceId)
        {
            return string.IsNullOrWhiteSpace(instanceId)
                ? null
                : _slots.FirstOrDefault(item => item?.InstanceId == instanceId);
        }

        public bool TryAdd(int itemId, out ItemInstance item, out int slotIndex)
        {
            if (itemId <= 0)
            {
                item = null;
                slotIndex = -1;
                return false;
            }

            return TryAdd(new GeneratedItemData(itemId), out item, out slotIndex);
        }

        public bool TryAdd(
            GeneratedItemData generatedData,
            out ItemInstance item,
            out int slotIndex)
        {
            item = null;
            slotIndex = Array.FindIndex(_slots, slot => slot == null);
            if (generatedData == null || slotIndex < 0)
            {
                return false;
            }

            item = new ItemInstance(
                $"item_{_nextInstanceNumber:D6}",
                generatedData);
            _nextInstanceNumber++;
            _slots[slotIndex] = item;
            Changed?.Invoke();
            return true;
        }

        public bool TryAddAt(
            int slotIndex,
            int itemId,
            out ItemInstance item)
        {
            if (itemId <= 0)
            {
                ValidateSlotIndex(slotIndex);
                item = null;
                return false;
            }

            return TryAddAt(
                slotIndex,
                new GeneratedItemData(itemId),
                out item);
        }

        public bool TryAddAt(
            int slotIndex,
            GeneratedItemData generatedData,
            out ItemInstance item)
        {
            ValidateSlotIndex(slotIndex);
            item = null;
            if (generatedData == null || _slots[slotIndex] != null)
            {
                return false;
            }

            item = new ItemInstance(
                $"item_{_nextInstanceNumber:D6}",
                generatedData);
            _nextInstanceNumber++;
            _slots[slotIndex] = item;
            Changed?.Invoke();
            return true;
        }

        public bool TryRemove(string instanceId, out ItemInstance removedItem)
        {
            removedItem = null;
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            var slotIndex = Array.FindIndex(
                _slots,
                item => item?.InstanceId == instanceId);
            if (slotIndex < 0)
            {
                return false;
            }

            removedItem = _slots[slotIndex];
            _slots[slotIndex] = null;
            Changed?.Invoke();
            return true;
        }

        private static void ValidateSlotIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
        }
    }
}
