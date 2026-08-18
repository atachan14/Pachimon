using System;

namespace Pachimon.Run
{
    public sealed class PachimonStatGenerationSettings
    {
        public PachimonStatGenerationSettings(
            int attributeAllocationBudget = 800,
            int commonAllocationBudget = 600,
            int resourceMinimumValueUnits = 100,
            int resourceDisplayMultiplier = 5,
            int initialMaxAllocation = 100,
            int additionalMaxAllocation = 100)
        {
            if (attributeAllocationBudget < 0) throw new ArgumentOutOfRangeException(nameof(attributeAllocationBudget));
            if (commonAllocationBudget < 0) throw new ArgumentOutOfRangeException(nameof(commonAllocationBudget));
            if (resourceMinimumValueUnits < 0) throw new ArgumentOutOfRangeException(nameof(resourceMinimumValueUnits));
            if (resourceDisplayMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(resourceDisplayMultiplier));
            if (initialMaxAllocation < 0) throw new ArgumentOutOfRangeException(nameof(initialMaxAllocation));
            if (additionalMaxAllocation < 1) throw new ArgumentOutOfRangeException(nameof(additionalMaxAllocation));

            AttributeAllocationBudget = attributeAllocationBudget;
            CommonAllocationBudget = commonAllocationBudget;
            ResourceMinimumValueUnits = resourceMinimumValueUnits;
            ResourceDisplayMultiplier = resourceDisplayMultiplier;
            InitialMaxAllocation = initialMaxAllocation;
            AdditionalMaxAllocation = additionalMaxAllocation;
        }

        public int AttributeAllocationBudget { get; }
        public int CommonAllocationBudget { get; }
        public int ResourceMinimumValueUnits { get; }
        public int ResourceDisplayMultiplier { get; }
        public int InitialMaxAllocation { get; }
        public int AdditionalMaxAllocation { get; }
        public int TotalValueUnits =>
            checked(
                ResourceMinimumValueUnits * 2
                + AttributeAllocationBudget
                + CommonAllocationBudget);
    }
}
