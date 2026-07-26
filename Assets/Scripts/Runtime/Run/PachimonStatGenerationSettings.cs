using System;

namespace Pachimon.Run
{
    public sealed class PachimonStatGenerationSettings
    {
        public PachimonStatGenerationSettings(
            int allocationBudget = 1300,
            int maxHpMinimumValueUnits = 50,
            int maxMnMinimumValueUnits = 50,
            int resourceDisplayMultiplier = 10,
            int specialStatDivisor = 3,
            int initialMaxAllocation = 100,
            int additionalMaxAllocation = 100)
        {
            if (allocationBudget < 0) throw new ArgumentOutOfRangeException(nameof(allocationBudget));
            if (maxHpMinimumValueUnits < 0) throw new ArgumentOutOfRangeException(nameof(maxHpMinimumValueUnits));
            if (maxMnMinimumValueUnits < 0) throw new ArgumentOutOfRangeException(nameof(maxMnMinimumValueUnits));
            if (resourceDisplayMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(resourceDisplayMultiplier));
            if (specialStatDivisor < 1) throw new ArgumentOutOfRangeException(nameof(specialStatDivisor));
            if (initialMaxAllocation < 0) throw new ArgumentOutOfRangeException(nameof(initialMaxAllocation));
            if (additionalMaxAllocation < 1) throw new ArgumentOutOfRangeException(nameof(additionalMaxAllocation));

            AllocationBudget = allocationBudget;
            MaxHpMinimumValueUnits = maxHpMinimumValueUnits;
            MaxMnMinimumValueUnits = maxMnMinimumValueUnits;
            ResourceDisplayMultiplier = resourceDisplayMultiplier;
            SpecialStatDivisor = specialStatDivisor;
            InitialMaxAllocation = initialMaxAllocation;
            AdditionalMaxAllocation = additionalMaxAllocation;
        }

        public int AllocationBudget { get; }
        public int MaxHpMinimumValueUnits { get; }
        public int MaxMnMinimumValueUnits { get; }
        public int ResourceDisplayMultiplier { get; }
        public int SpecialStatDivisor { get; }
        public int InitialMaxAllocation { get; }
        public int AdditionalMaxAllocation { get; }
        public int TotalValueUnits =>
            MaxHpMinimumValueUnits + MaxMnMinimumValueUnits + AllocationBudget;
    }
}
