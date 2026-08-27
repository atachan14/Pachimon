using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Reward
{
    public sealed class NodeRewardGenerator
    {
        public const int BattleRewardCount = 69;
        public const int GymRewardCount = 24;
        public const int TotalBattleGold = 69000;
        public const int TotalGymGold = 24000;
        public const int MinimumGold = 500;
        public const int MaximumGold = 1500;
        public const int AttributeCopiesPerSlot = 7;
        public const int NonAttributeCopiesPerSlot = 4;
        public const int BonusGoldCopiesPerSlot = 5;

        private readonly Random _random;

        public NodeRewardGenerator(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public IReadOnlyList<NodeReward> CreateBattleRewards()
        {
            var firstElements = CreateElementDeck();
            var secondElements = CreateElementDeck();
            Shuffle(firstElements);
            PairWithoutDuplicates(firstElements, secondElements);
            var goldValues = CreateGoldValues(BattleRewardCount, TotalBattleGold);
            return Enumerable.Range(0, BattleRewardCount)
                .Select(index => new NodeReward(
                    goldValues[index],
                    firstElements[index],
                    secondElements[index],
                    null))
                .ToArray();
        }

        public IReadOnlyList<NodeReward> CreateGymRewards()
        {
            var badges = new List<PachimonAttribute>(GymRewardCount);
            foreach (PachimonAttribute attribute in Enum.GetValues(typeof(PachimonAttribute)))
            {
                for (var count = 0; count < 3; count++)
                {
                    badges.Add(attribute);
                }
            }

            Shuffle(badges);
            var goldValues = CreateGoldValues(GymRewardCount, TotalGymGold);
            return Enumerable.Range(0, GymRewardCount)
                .Select(index => new NodeReward(
                    goldValues[index],
                    null,
                    null,
                    badges[index]))
                .ToArray();
        }

        private static List<RewardElement> CreateElementDeck()
        {
            var elements = new List<RewardElement>(BattleRewardCount);
            foreach (PachimonAttribute attribute in Enum.GetValues(typeof(PachimonAttribute)))
            {
                for (var count = 0; count < AttributeCopiesPerSlot; count++)
                {
                    elements.Add(RewardElement.CreateAttribute(attribute));
                }
            }

            foreach (var kind in NonAttributeKinds)
            {
                for (var count = 0; count < NonAttributeCopiesPerSlot; count++)
                {
                    elements.Add(RewardElement.Create(kind));
                }
            }

            for (var count = 0; count < BonusGoldCopiesPerSlot; count++)
            {
                elements.Add(RewardElement.Create(RewardElementKind.BonusGold));
            }

            if (elements.Count != BattleRewardCount)
            {
                throw new InvalidOperationException(
                    $"Reward element deck has {elements.Count} entries; expected {BattleRewardCount}.");
            }

            return elements;
        }

        private void PairWithoutDuplicates(
            IReadOnlyList<RewardElement> firstElements,
            IList<RewardElement> secondElements)
        {
            if (firstElements.Count != secondElements.Count)
            {
                throw new ArgumentException(
                    "Reward Element decks must contain the same number of entries.");
            }

            var sourceElements = secondElements.ToArray();
            var candidateIndices = new int[firstElements.Count][];
            for (var firstIndex = 0; firstIndex < firstElements.Count; firstIndex++)
            {
                var candidates = Enumerable.Range(0, sourceElements.Length)
                    .Where(secondIndex =>
                        !IsSameElement(firstElements[firstIndex], sourceElements[secondIndex]))
                    .ToList();
                Shuffle(candidates);
                candidateIndices[firstIndex] = candidates.ToArray();
            }

            var firstOrder = Enumerable.Range(0, firstElements.Count).ToList();
            Shuffle(firstOrder);
            var matchedFirstBySecond = Enumerable.Repeat(-1, sourceElements.Length).ToArray();
            var matchedSecondByFirst = Enumerable.Repeat(-1, firstElements.Count).ToArray();

            foreach (var firstIndex in firstOrder)
            {
                if (!TryAssign(firstIndex, new bool[sourceElements.Length]))
                {
                    throw new InvalidOperationException(
                        "Reward Element decks cannot be paired without duplicates.");
                }
            }

            for (var firstIndex = 0; firstIndex < firstElements.Count; firstIndex++)
            {
                secondElements[firstIndex] = sourceElements[matchedSecondByFirst[firstIndex]];
            }

            bool TryAssign(int firstIndex, bool[] visitedSecondIndices)
            {
                foreach (var secondIndex in candidateIndices[firstIndex])
                {
                    if (visitedSecondIndices[secondIndex])
                    {
                        continue;
                    }

                    visitedSecondIndices[secondIndex] = true;
                    var previousFirstIndex = matchedFirstBySecond[secondIndex];
                    if (previousFirstIndex >= 0
                        && !TryAssign(previousFirstIndex, visitedSecondIndices))
                    {
                        continue;
                    }

                    matchedFirstBySecond[secondIndex] = firstIndex;
                    matchedSecondByFirst[firstIndex] = secondIndex;
                    return true;
                }

                return false;
            }
        }

        private int[] CreateGoldValues(int count, int totalGold)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (totalGold < count * MinimumGold || totalGold > count * MaximumGold)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalGold),
                    "Gold budget cannot fit within configured bounds.");
            }

            var values = new int[count];
            for (var index = 0; index < values.Length; index++)
            {
                values[index] = MinimumGold;
            }

            var remaining = totalGold - values.Sum();
            while (remaining > 0)
            {
                var candidates = Enumerable.Range(0, values.Length)
                    .Where(index => values[index] < MaximumGold)
                    .ToArray();
                if (candidates.Length == 0)
                {
                    throw new InvalidOperationException("Gold budget cannot fit within configured bounds.");
                }

                var index = candidates[_random.Next(candidates.Length)];
                var capacity = MaximumGold - values[index];
                var maximumChunk = Math.Min(Math.Min(capacity, remaining), 100);
                values[index] += _random.Next(1, maximumChunk + 1);
                remaining = totalGold - values.Sum();
            }

            return values;
        }

        private static bool IsSameElement(RewardElement first, RewardElement second)
        {
            return first.Kind == second.Kind && first.Attribute == second.Attribute;
        }

        private static readonly RewardElementKind[] NonAttributeKinds =
        {
            RewardElementKind.MaxHp,
            RewardElementKind.MaxMn,
        };

        private void Shuffle<T>(IList<T> values)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = _random.Next(index + 1);
                (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
            }
        }
    }
}
