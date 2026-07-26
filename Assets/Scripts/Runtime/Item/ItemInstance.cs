using System;

namespace Pachimon.Items
{
    public sealed class ItemInstance
    {
        public ItemInstance(string instanceId, int itemId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("Instance ID is required.", nameof(instanceId));
            }

            if (itemId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            InstanceId = instanceId;
            ItemId = itemId;
        }

        public string InstanceId { get; }
        public int ItemId { get; }
    }
}
