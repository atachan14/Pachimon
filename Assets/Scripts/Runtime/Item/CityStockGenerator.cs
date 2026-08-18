using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using Pachimon.Skills;
using Pachimon.Run;
using Pachimon.Reward;

namespace Pachimon.Items
{
    public sealed class CityStockRequest
    {
        public CityStockRequest(string cityGroupId, int shopSeed)
        {
            if (string.IsNullOrWhiteSpace(cityGroupId))
            {
                throw new ArgumentException("City Group ID is required.", nameof(cityGroupId));
            }

            CityGroupId = cityGroupId;
            ShopSeed = shopSeed;
        }

        public string CityGroupId { get; }
        public int ShopSeed { get; }
    }

    public sealed class CityStockGenerator
    {
        public const int MinimumPricePercent = 70;
        public const int MaximumPricePercent = 130;
        public const int MinimumEffectPercent = 70;
        public const int MaximumEffectPercent = 130;
        public const int PotionTotalCopies = 40;
        public const int MnPotionTotalCopies = 40;
        public const int MachineCopiesPerPoolPerCity = 1;
        public const int EngravingCopiesPerStat = 32;
        public const int EquipmentCopiesPerDefinition = 2;
        public const int EquipmentPerCity = 6;
        public const int MinimumEquipmentEffectPercent = 80;
        public const int MaximumEquipmentEffectPercent = 120;

        public IReadOnlyDictionary<string, IReadOnlyList<CityStockEntry>> Generate(
            IReadOnlyList<CityStockRequest> requests,
            ItemCatalog itemCatalog)
        {
            ValidateRequests(requests);
            if (itemCatalog == null)
            {
                throw new ArgumentNullException(nameof(itemCatalog));
            }

            var potion = GetRequiredItem(itemCatalog, ItemIds.Potion, "Potion");
            var mnPotion = GetRequiredItem(itemCatalog, ItemIds.MnPotion, "MN Potion");
            var machineItems = itemCatalog.Items
                .OfType<SkillMachineItemAsset>()
                .Where(item => item.Skill != null
                    && item.SkillId >= SkillIdRanges.FirstMachineExclusiveId
                    && item.SkillId <= SkillIdRanges.LastMachineExclusiveId)
                .OrderBy(item => item.SkillId)
                .ToArray();
            var neutralMachines = machineItems
                .Where(item => item.Skill.AllocationType == AllocationType.Unassigned)
                .ToArray();
            var attributeMachines = machineItems
                .Where(item => item.Skill.AllocationType != AllocationType.Unassigned)
                .ToArray();
            var engravings = itemCatalog.Items
                .OfType<EngravingItemAsset>()
                .OrderBy(item => item.TargetStat)
                .ToArray();
            var equipment = itemCatalog.Items
                .OfType<EquipmentItemAsset>()
                .OrderBy(item => item.Slot)
                .ThenBy(item => item.MainAttribute)
                .ToArray();

            ValidateMachinePool("neutral", neutralMachines, requests.Count);
            ValidateMachinePool("attribute", attributeMachines, requests.Count);
            ValidateEngravings(engravings);
            ValidateEquipment(equipment, requests.Count);

            var random = new Random(CreateDistributionSeed(requests));
            var assignedItems = requests.ToDictionary(
                request => request.CityGroupId,
                _ => new List<ItemAsset>());

            DistributeRandomCopies(
                potion,
                PotionTotalCopies,
                requests,
                assignedItems,
                random);
            DistributeRandomCopies(
                mnPotion,
                MnPotionTotalCopies,
                requests,
                assignedItems,
                random);
            DistributeEvenly(neutralMachines, requests, assignedItems, random);
            DistributeEvenly(attributeMachines, requests, assignedItems, random);
            foreach (var engraving in engravings)
            {
                DistributeRandomCopies(
                    engraving,
                    EngravingCopiesPerStat,
                    requests,
                    assignedItems,
                    random);
            }
            DistributeEquipment(
                equipment,
                requests,
                assignedItems,
                random);

            var result = new Dictionary<string, IReadOnlyList<CityStockEntry>>();
            foreach (var request in requests)
            {
                result.Add(
                    request.CityGroupId,
                    CreateStockEntries(
                        request,
                        assignedItems[request.CityGroupId]));
            }

            return result;
        }

        public static int GetMinimumPrice(int basePrice)
        {
            return checked(((basePrice * MinimumPricePercent) + 99) / 100);
        }

        public static int GetMaximumPrice(int basePrice)
        {
            return checked((basePrice * MaximumPricePercent) / 100);
        }

        private static void ValidateRequests(IReadOnlyList<CityStockRequest> requests)
        {
            if (requests == null || requests.Count == 0)
            {
                throw new ArgumentException("At least one City stock request is required.", nameof(requests));
            }

            if (requests.Any(request => request == null)
                || requests.Select(request => request.CityGroupId).Distinct().Count()
                    != requests.Count)
            {
                throw new ArgumentException(
                    "City stock requests must be non-null and have unique Group IDs.",
                    nameof(requests));
            }
        }

        private static ItemAsset GetRequiredItem(
            ItemCatalog itemCatalog,
            int itemId,
            string label)
        {
            var item = itemCatalog.Get(itemId);
            if (item == null || item.BasePrice <= 0)
            {
                throw new InvalidOperationException(
                    $"City stock generation requires {label} with a positive BasePrice.");
            }

            return item;
        }

        private static void ValidateMachinePool(
            string label,
            IReadOnlyCollection<SkillMachineItemAsset> machines,
            int cityCount)
        {
            var requiredCount = checked(cityCount * MachineCopiesPerPoolPerCity);
            if (machines.Count < requiredCount)
            {
                throw new InvalidOperationException(
                    $"The machine-exclusive {label} Skill pool requires at least "
                    + $"{requiredCount} entries for {cityCount} Cities, but contains "
                    + $"{machines.Count} entries.");
            }
        }

        private static void ValidateEngravings(
            IReadOnlyCollection<EngravingItemAsset> engravings)
        {
            var expectedCount = (int)PachimonStatType.Count;
            if (engravings.Count != expectedCount
                || engravings.Select(item => item.TargetStat).Distinct().Count()
                    != expectedCount)
            {
                throw new InvalidOperationException(
                    "City stock generation requires one Engraving Item for every Pachimon Stat.");
            }
        }

        private static void ValidateEquipment(
            IReadOnlyCollection<EquipmentItemAsset> equipment,
            int cityCount)
        {
            var expectedDefinitions = Enum.GetValues(typeof(EquipmentSlot)).Length
                * Enum.GetValues(typeof(PachimonAttribute)).Length;
            if (equipment.Count != expectedDefinitions
                || equipment
                    .Select(item => (item.Slot, item.MainAttribute))
                    .Distinct()
                    .Count() != expectedDefinitions
                || equipment.Count * EquipmentCopiesPerDefinition
                    != cityCount * EquipmentPerCity)
            {
                throw new InvalidOperationException(
                    "City stock requires two copies of every Slot/Attribute Equipment definition.");
            }
        }

        private static int CreateDistributionSeed(
            IEnumerable<CityStockRequest> requests)
        {
            unchecked
            {
                var seed = 0x43495459;
                foreach (var request in requests)
                {
                    seed = (seed * 397) ^ request.ShopSeed;
                }

                return seed;
            }
        }

        private static void DistributeRandomCopies(
            ItemAsset item,
            int totalCopies,
            IReadOnlyList<CityStockRequest> requests,
            IReadOnlyDictionary<string, List<ItemAsset>> assignedItems,
            Random random)
        {
            if (totalCopies < requests.Count)
            {
                throw new InvalidOperationException(
                    $"Item {item.ItemId} requires at least one copy per City.");
            }

            // Keep every category represented, then let the remaining stock vary by Run.
            foreach (var request in requests)
            {
                assignedItems[request.CityGroupId].Add(item);
            }

            for (var copyIndex = requests.Count; copyIndex < totalCopies; copyIndex++)
            {
                var request = requests[random.Next(requests.Count)];
                assignedItems[request.CityGroupId].Add(item);
            }
        }

        private static void DistributeEvenly(
            IReadOnlyCollection<SkillMachineItemAsset> machines,
            IReadOnlyList<CityStockRequest> requests,
            IReadOnlyDictionary<string, List<ItemAsset>> assignedItems,
            Random random)
        {
            var shuffled = machines.Cast<ItemAsset>().ToList();
            Shuffle(shuffled, random);
            var distributedCount = checked(
                requests.Count * MachineCopiesPerPoolPerCity);
            for (var index = 0; index < distributedCount; index++)
            {
                var request = requests[index % requests.Count];
                assignedItems[request.CityGroupId].Add(shuffled[index]);
            }
        }

        private static void DistributeEquipment(
            IEnumerable<EquipmentItemAsset> equipment,
            IReadOnlyList<CityStockRequest> requests,
            IReadOnlyDictionary<string, List<ItemAsset>> assignedItems,
            Random random)
        {
            var deck = equipment
                .SelectMany(item => Enumerable.Repeat<ItemAsset>(
                    item,
                    EquipmentCopiesPerDefinition))
                .ToList();
            Shuffle(deck, random);
            for (var index = 0; index < deck.Count; index++)
            {
                assignedItems[requests[index % requests.Count].CityGroupId]
                    .Add(deck[index]);
            }
        }

        private static IReadOnlyList<CityStockEntry> CreateStockEntries(
            CityStockRequest request,
            IEnumerable<ItemAsset> items)
        {
            var random = new Random(request.ShopSeed);
            var effectRandom = new Random(unchecked(request.ShopSeed ^ 0x45464645));
            var drafts = items.Select(item =>
                {
                    var minimumPrice = GetMinimumPrice(item.BasePrice);
                    var maximumPrice = GetMaximumPrice(item.BasePrice);
                    return new StockDraft(
                        item,
                        minimumPrice,
                        maximumPrice,
                        random.Next(minimumPrice, maximumPrice + 1));
                })
                .ToList();

            BalanceTotalPrice(drafts, random);
            AssignPrimaryEffectValues(drafts, effectRandom);
            Shuffle(drafts, random);
            return drafts
                .Select((draft, index) => new CityStockEntry(
                    $"{request.CityGroupId}_stock_{index + 1:D3}",
                    new GeneratedItemData(
                        draft.Item.ItemId,
                        draft.PrimaryEffectValue,
                        draft.StatChanges,
                        draft.EquipmentSlot),
                    draft.BasePrice,
                    draft.Price))
                .ToArray();
        }

        private static void AssignPrimaryEffectValues(
            IReadOnlyCollection<StockDraft> drafts,
            Random random)
        {
            foreach (var group in drafts
                         .Where(draft => draft.Item is HealingItemAsset)
                         .GroupBy(draft => draft.Item.ItemId))
            {
                var healingItem = (HealingItemAsset)group.First().Item;
                var values = CreateBalancedEffectValues(
                    group.Count(),
                    healingItem.RecoveryPercent,
                    random);
                var orderedDrafts = group
                    .OrderBy(draft => draft.Price)
                    .ToArray();
                Array.Sort(values);
                for (var index = 0; index < orderedDrafts.Length; index++)
                {
                    orderedDrafts[index].PrimaryEffectValue = values[index];
                }
            }


            var equipmentDrafts = drafts
                .Where(draft => draft.Item is EquipmentItemAsset)
                .OrderBy(draft => draft.Price)
                .ToArray();
            var equipmentBase = checked(
                StatUnitValue.Get(PachimonStatType.Fire) * 3);
            var equipmentValues = Enumerable.Range(0, equipmentDrafts.Length)
                .Select(_ => random.Next(
                    checked(equipmentBase * MinimumEquipmentEffectPercent / 100),
                    checked(equipmentBase * MaximumEquipmentEffectPercent / 100) + 1))
                .OrderBy(value => value)
                .ToArray();
            for (var index = 0; index < equipmentDrafts.Length; index++)
            {
                var draft = equipmentDrafts[index];
                var equipment = (EquipmentItemAsset)draft.Item;
                var mainStat = PachimonStatTypeUtility.FromAttribute(
                    equipment.MainAttribute);
                var rankedValue = equipmentValues[index];
                var mainValue = equipment.Slot == EquipmentSlot.Head
                    ? checked(rankedValue * 2)
                    : rankedValue;
                var additionalAttributes = Enum
                    .GetValues(typeof(PachimonAttribute))
                    .Cast<PachimonAttribute>()
                    .Where(attribute => attribute != equipment.MainAttribute)
                    .ToArray();
                var additionalStat = PachimonStatTypeUtility.FromAttribute(
                    additionalAttributes[random.Next(additionalAttributes.Length)]);
                var changes = new List<GeneratedStatChange>
                {
                    new(mainStat, mainValue),
                    new(additionalStat, Math.Max(1, mainValue / 3)),
                };
                if (equipment.Slot == EquipmentSlot.Body)
                {
                    changes.Add(new GeneratedStatChange(
                        PachimonStatType.Haste,
                        checked(StatUnitValue.Get(PachimonStatType.Haste) * 4)));
                }
                else if (equipment.Slot == EquipmentSlot.Feet)
                {
                    changes.Add(new GeneratedStatChange(
                        PachimonStatType.Speed,
                        checked(StatUnitValue.Get(PachimonStatType.Speed) * 4)));
                }

                draft.StatChanges = changes;
                draft.EquipmentSlot = equipment.Slot;
            }


            var engravingByStat = drafts
                .Select(draft => draft.Item)
                .OfType<EngravingItemAsset>()
                .Distinct()
                .ToDictionary(item => item.TargetStat);
            foreach (var group in drafts
                         .Where(draft => draft.Item is EngravingItemAsset)
                         .GroupBy(draft => draft.Item.ItemId))
            {
                var engraving = (EngravingItemAsset)group.First().Item;
                var values = CreateBalancedEffectValues(
                    group.Count(),
                    engraving.BaseEffectValue,
                    random);
                var orderedDrafts = group.OrderBy(draft => draft.Price).ToArray();
                Array.Sort(values);
                for (var index = 0; index < orderedDrafts.Length; index++)
                {
                    var mainValue = values[index];
                    var downsideCandidates = engravingByStat.Values
                        .Where(item => item.TargetStat != engraving.TargetStat)
                        .ToArray();
                    var downside = downsideCandidates[random.Next(
                        downsideCandidates.Length)];
                    var downsideValue = Math.Max(
                        1,
                        (int)Math.Floor(
                            downside.BaseEffectValue
                            * (decimal)mainValue
                            / engraving.BaseEffectValue
                            / 2m));
                    orderedDrafts[index].StatChanges = new[]
                    {
                        new GeneratedStatChange(engraving.TargetStat, mainValue),
                        new GeneratedStatChange(downside.TargetStat, -downsideValue),
                    };
                }
            }
        }

        private static int[] CreateBalancedEffectValues(
            int count,
            int baseValue,
            Random random)
        {
            var minimum = checked(
                ((baseValue * MinimumEffectPercent) + 99) / 100);
            var maximum = checked(
                (baseValue * MaximumEffectPercent) / 100);
            var values = Enumerable.Range(0, count)
                .Select(_ => random.Next(minimum, maximum + 1))
                .ToArray();
            var difference = checked((baseValue * count) - values.Sum());
            while (difference != 0)
            {
                var increase = difference > 0;
                var candidates = Enumerable.Range(0, values.Length)
                    .Where(index => increase
                        ? values[index] < maximum
                        : values[index] > minimum)
                    .ToArray();
                if (candidates.Length == 0)
                {
                    throw new InvalidOperationException(
                        "City Item effects could not be balanced within configured bounds.");
                }

                var targetIndex = candidates[random.Next(candidates.Length)];
                var capacity = increase
                    ? maximum - values[targetIndex]
                    : values[targetIndex] - minimum;
                var adjustment = random.Next(
                    1,
                    Math.Min(Math.Abs(difference), capacity) + 1);
                values[targetIndex] += increase ? adjustment : -adjustment;
                difference += increase ? -adjustment : adjustment;
            }

            return values;
        }

        private static void BalanceTotalPrice(IList<StockDraft> drafts, Random random)
        {
            var difference = drafts.Sum(draft => draft.BasePrice)
                - drafts.Sum(draft => draft.Price);
            while (difference != 0)
            {
                var increase = difference > 0;
                var candidates = Enumerable.Range(0, drafts.Count)
                    .Where(index => increase
                        ? drafts[index].Price < drafts[index].MaximumPrice
                        : drafts[index].Price > drafts[index].MinimumPrice)
                    .ToArray();
                if (candidates.Length == 0)
                {
                    throw new InvalidOperationException(
                        "City stock prices could not be balanced within configured bounds.");
                }

                var draft = drafts[candidates[random.Next(candidates.Length)]];
                var capacity = increase
                    ? draft.MaximumPrice - draft.Price
                    : draft.Price - draft.MinimumPrice;
                var maximumAdjustment = Math.Min(Math.Abs(difference), capacity);
                var adjustment = random.Next(1, maximumAdjustment + 1);
                draft.Price += increase ? adjustment : -adjustment;
                difference += increase ? -adjustment : adjustment;
            }
        }

        private static void Shuffle<T>(IList<T> values, Random random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var target = random.Next(index + 1);
                (values[index], values[target]) = (values[target], values[index]);
            }
        }

        private sealed class StockDraft
        {
            public StockDraft(
                ItemAsset item,
                int minimumPrice,
                int maximumPrice,
                int price)
            {
                Item = item;
                BasePrice = item.BasePrice;
                MinimumPrice = minimumPrice;
                MaximumPrice = maximumPrice;
                Price = price;
            }

            public ItemAsset Item { get; }
            public int BasePrice { get; }
            public int MinimumPrice { get; }
            public int MaximumPrice { get; }
            public int Price { get; set; }
            public int? PrimaryEffectValue { get; set; }
            public IReadOnlyList<GeneratedStatChange> StatChanges { get; set; }
            public EquipmentSlot? EquipmentSlot { get; set; }
        }
    }
}
