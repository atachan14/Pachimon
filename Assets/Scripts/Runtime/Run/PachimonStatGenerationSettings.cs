using System;

namespace Pachimon.Run
{
    public sealed class PachimonStatGenerationSettings
    {
        public PachimonStatGenerationSettings(
            int allocationBudget = 500,
            int resourceBaseValue = PachimonStatValueUnits.ResourceBaseValue,
            int resourceDisplayMultiplier = PachimonStatValueUnits.ResourceDisplayMultiplier,
            int initialMaxAllocation = 50,
            int additionalMaxAllocation = 50)
        {
            if (allocationBudget < 0) throw new ArgumentOutOfRangeException(nameof(allocationBudget));
            if (resourceBaseValue < 0) throw new ArgumentOutOfRangeException(nameof(resourceBaseValue));
            if (resourceDisplayMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(resourceDisplayMultiplier));
            if (initialMaxAllocation < 0) throw new ArgumentOutOfRangeException(nameof(initialMaxAllocation));
            if (additionalMaxAllocation < 1) throw new ArgumentOutOfRangeException(nameof(additionalMaxAllocation));

            AllocationBudget = allocationBudget;
            ResourceBaseValue = resourceBaseValue;
            ResourceDisplayMultiplier = resourceDisplayMultiplier;
            InitialMaxAllocation = initialMaxAllocation;
            AdditionalMaxAllocation = additionalMaxAllocation;
        }

        public int AllocationBudget { get; }
        public int ResourceBaseValue { get; }
        public int ResourceDisplayMultiplier { get; }
        public int InitialMaxAllocation { get; }
        public int AdditionalMaxAllocation { get; }
        public int TotalValueUnits => AllocationBudget;
    }
}
