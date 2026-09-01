using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Items
{
    public sealed class GeneratedStatChange
    {
        public GeneratedStatChange(PachimonStatType statType, int amount)
        {
            if (statType < 0 || statType >= PachimonStatType.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(statType));
            }
            if (amount == 0) throw new ArgumentOutOfRangeException(nameof(amount));
            StatType = statType;
            Amount = amount;
        }

        public PachimonStatType StatType { get; }
        public int Amount { get; }
    }

    public sealed class GeneratedItemData
    {
        private readonly GeneratedStatChange[] _statChanges;

        public GeneratedItemData(
            int itemId,
            int? primaryEffectValue = null,
            IEnumerable<GeneratedStatChange> statChanges = null,
            EquipmentSlot? equipmentSlot = null)
        {
            if (itemId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            if (primaryEffectValue.HasValue && primaryEffectValue.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(primaryEffectValue));
            }

            ItemId = itemId;
            PrimaryEffectValue = primaryEffectValue;
            EquipmentSlot = equipmentSlot;
            _statChanges = statChanges?.ToArray()
                ?? Array.Empty<GeneratedStatChange>();
            if (_statChanges.Any(change => change == null))
            {
                throw new ArgumentException(
                    "Generated Stat changes cannot contain null.",
                    nameof(statChanges));
            }
        }

        public int ItemId { get; }

        public int? PrimaryEffectValue { get; }

        public IReadOnlyList<GeneratedStatChange> StatChanges => _statChanges;

        public EquipmentSlot? EquipmentSlot { get; }
    }

    public sealed class ItemInstance
    {
        public ItemInstance(string instanceId, int itemId)
            : this(instanceId, new GeneratedItemData(itemId))
        {
        }

        public ItemInstance(string instanceId, GeneratedItemData generatedData)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("Instance ID is required.", nameof(instanceId));
            }

            InstanceId = instanceId;
            GeneratedData = generatedData
                ?? throw new ArgumentNullException(nameof(generatedData));
        }

        public string InstanceId { get; }
        public GeneratedItemData GeneratedData { get; }
        public int ItemId => GeneratedData.ItemId;
    }

    public static class ItemDisplayNameFormatter
    {
        public static string Format(ItemAsset item, GeneratedItemData generatedData)
        {
            if (item == null)
            {
                return generatedData == null
                    ? "Item"
                    : $"Item #{generatedData.ItemId}";
            }

            if (item is HealingItemAsset healingItem)
            {
                var recoveryAmount = generatedData?.PrimaryEffectValue
                    ?? healingItem.RecoveryAmount;
                var suffix = healingItem.ValueMode == RecoveryValueMode.MaximumPercent
                    ? $"{recoveryAmount}%"
                    : recoveryAmount.ToString();
                return healingItem.DefeatedOnly
                    ? $"{item.DisplayName}（{suffix}）"
                    : $"{item.DisplayName}（+{suffix}）";
            }

            if (item is EngravingItemAsset
                && generatedData?.StatChanges.FirstOrDefault(
                    change => change.Amount > 0) is { } mainEffect)
            {
                return $"{item.DisplayName}（+{mainEffect.Amount}）";
            }

            return item.DisplayName;
        }
    }
}
