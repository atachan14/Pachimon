using System;
using Pachimon.Data;

namespace Pachimon.Run
{
    public sealed class PachimonStatsGenerator
    {
        private static readonly int[] GeneratedStatIndices =
        {
            (int)PachimonStatType.MaxHp,
            (int)PachimonStatType.MaxMn,
            (int)PachimonStatType.Fire,
            (int)PachimonStatType.Aqua,
            (int)PachimonStatType.Leaf,
            (int)PachimonStatType.Electric,
            (int)PachimonStatType.Poison,
            (int)PachimonStatType.Ice,
            (int)PachimonStatType.Wind,
            (int)PachimonStatType.Dragon,
        };

        private readonly PachimonStatGenerationSettings _settings;

        public PachimonStatsGenerator(PachimonStatGenerationSettings settings = null)
        {
            _settings = settings ?? new PachimonStatGenerationSettings();
        }

        public PachimonStats Generate(
            Random random,
            PachimonSpeciesAsset species = null)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));

            var statCount = (int)PachimonStatType.Count;
            var valueUnits = new int[statCount];
            var initialValue = ApplySpeciesInitialStats(
                valueUnits,
                GeneratedStatIndices,
                species);
            ValidateInitialBudget(
                species,
                initialValue,
                _settings.AllocationBudget);
            AllocateBudget(
                valueUnits,
                GeneratedStatIndices,
                _settings.AllocationBudget - initialValue,
                random);

            var stats = new PachimonStats(
                valueUnits,
                _settings.ResourceDisplayMultiplier,
                specialStatDivisor: 1,
                resourceBaseValue: _settings.ResourceBaseValue);
            if (stats.GetTotalValueUnits() != _settings.TotalValueUnits)
            {
                throw new InvalidOperationException("Pachimon stat generation did not spend its full value budget.");
            }

            return stats;
        }

        private int ApplySpeciesInitialStats(
            int[] valueUnits,
            int[] statIndices,
            PachimonSpeciesAsset species)
        {
            if (species?.InitialStats == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var statIndex in statIndices)
            {
                var statType = (PachimonStatType)statIndex;
                var initialValue = species.InitialStats.GetValueUnits(
                    statType,
                    _settings.ResourceDisplayMultiplier);
                valueUnits[statIndex] = checked(
                    valueUnits[statIndex] + initialValue);
                total = checked(total + initialValue);
            }

            return total;
        }

        private static void ValidateInitialBudget(
            PachimonSpeciesAsset species,
            int initialValue,
            int budget)
        {
            if (initialValue <= budget)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Pachimon Species {species?.SpeciesId} ({species?.DisplayName}) "
                + $"uses {initialValue} initial Stat units, "
                + $"but its Budget is {budget}.");
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
