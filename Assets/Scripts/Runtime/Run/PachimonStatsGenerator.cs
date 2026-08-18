using System;

namespace Pachimon.Run
{
    public sealed class PachimonStatsGenerator
    {
        private static readonly int[] AttributeStatIndices =
        {
            (int)PachimonStatType.Fire,
            (int)PachimonStatType.Aqua,
            (int)PachimonStatType.Leaf,
            (int)PachimonStatType.Electric,
            (int)PachimonStatType.Poison,
            (int)PachimonStatType.Ice,
            (int)PachimonStatType.Wind,
            (int)PachimonStatType.Dragon,
        };

        private static readonly int[] CommonStatIndices =
        {
            (int)PachimonStatType.MaxHp,
            (int)PachimonStatType.MaxMn,
            (int)PachimonStatType.Speed,
            (int)PachimonStatType.Haste,
            (int)PachimonStatType.DamageBonus,
            (int)PachimonStatType.ResistBonus,
        };

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
            valueUnits[(int)PachimonStatType.MaxHp] = _settings.ResourceMinimumValueUnits;
            valueUnits[(int)PachimonStatType.MaxMn] = _settings.ResourceMinimumValueUnits;
            AllocateBudget(
                valueUnits,
                AttributeStatIndices,
                _settings.AttributeAllocationBudget,
                random);
            AllocateBudget(
                valueUnits,
                CommonStatIndices,
                _settings.CommonAllocationBudget,
                random);

            var stats = new PachimonStats(
                valueUnits,
                _settings.ResourceDisplayMultiplier,
                specialStatDivisor: 1);
            if (stats.GetTotalValueUnits() != _settings.TotalValueUnits)
            {
                throw new InvalidOperationException("Pachimon stat generation did not spend its full value budget.");
            }

            return stats;
        }

        private void AllocateBudget(
            int[] valueUnits,
            int[] statIndices,
            int budget,
            Random random)
        {
            var remainingBudget = budget;
            var initialOrder = CreateShuffledOrder(statIndices, random);
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
                var statIndex = statIndices[random.Next(0, statIndices.Length)];
                var allocation = random.Next(1, _settings.AdditionalMaxAllocation + 1);
                allocation = Math.Min(allocation, remainingBudget);
                valueUnits[statIndex] += allocation;
                remainingBudget -= allocation;
            }
        }

        private static int[] CreateShuffledOrder(int[] statIndices, Random random)
        {
            var indices = (int[])statIndices.Clone();
            for (var index = indices.Length - 1; index > 0; index--)
            {
                var swapIndex = random.Next(0, index + 1);
                (indices[index], indices[swapIndex]) = (indices[swapIndex], indices[index]);
            }

            return indices;
        }
    }
}
