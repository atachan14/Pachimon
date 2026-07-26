using System;

namespace Pachimon.Run
{
    public sealed class PachimonStatsGenerator
    {
        private readonly PachimonStatGenerationSettings _settings;

        public PachimonStatsGenerator(PachimonStatGenerationSettings settings = null)
        {
            _settings = settings ?? new PachimonStatGenerationSettings();
        }

        public PachimonStats Generate(Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));

            var statCount = (int)PachimonStatType.Count;
            var valueUnits = new int[statCount];
            valueUnits[(int)PachimonStatType.MaxHp] = _settings.MaxHpMinimumValueUnits;
            valueUnits[(int)PachimonStatType.MaxMn] = _settings.MaxMnMinimumValueUnits;
            var remainingBudget = _settings.AllocationBudget;
            var initialOrder = CreateShuffledStatOrder(statCount, random);

            foreach (var statIndex in initialOrder)
            {
                if (remainingBudget == 0) break;
                var maximum = Math.Min(_settings.InitialMaxAllocation, remainingBudget);
                var allocation = random.Next(0, maximum + 1);
                valueUnits[statIndex] += allocation;
                remainingBudget -= allocation;
            }

            while (remainingBudget > 0)
            {
                var statIndex = random.Next(0, statCount);
                var allocation = random.Next(1, _settings.AdditionalMaxAllocation + 1);
                allocation = Math.Min(allocation, remainingBudget);
                valueUnits[statIndex] += allocation;
                remainingBudget -= allocation;
            }

            var stats = new PachimonStats(
                valueUnits,
                _settings.ResourceDisplayMultiplier,
                _settings.SpecialStatDivisor);
            if (stats.GetTotalValueUnits() != _settings.TotalValueUnits)
            {
                throw new InvalidOperationException("Pachimon stat generation did not spend its full value budget.");
            }

            return stats;
        }

        private static int[] CreateShuffledStatOrder(int statCount, Random random)
        {
            var indices = new int[statCount];
            for (var index = 0; index < statCount; index++) indices[index] = index;
            for (var index = indices.Length - 1; index > 0; index--)
            {
                var swapIndex = random.Next(0, index + 1);
                (indices[index], indices[swapIndex]) = (indices[swapIndex], indices[index]);
            }

            return indices;
        }
    }
}
