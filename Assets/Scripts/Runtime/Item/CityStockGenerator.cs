using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Items
{
    public sealed class CityStockGenerator
    {
        public const int MinimumPricePercent = 70;
        public const int MaximumPricePercent = 130;
        public const int SampleCopiesPerItem = 10;

        public IReadOnlyList<CityStockEntry> Generate(
            string cityGroupId,
            int shopSeed,
            ItemCatalog itemCatalog)
        {
            if (string.IsNullOrWhiteSpace(cityGroupId))
            {
                throw new ArgumentException("City Group ID is required.", nameof(cityGroupId));
            }

            if (itemCatalog == null)
            {
                throw new ArgumentNullException(nameof(itemCatalog));
            }

            var sampleItems = new[]
            {
                itemCatalog.Get(ItemIds.Potion),
                itemCatalog.Get(ItemIds.Stone),
            };
            if (sampleItems.Any(item => item == null || item.BasePrice <= 0))
            {
                throw new InvalidOperationException(
                    "City stock generation requires Potion and Stone with positive BasePrice.");
            }

            var random = new Random(shopSeed);
            var drafts = new List<StockDraft>(sampleItems.Length * SampleCopiesPerItem);
            foreach (var item in sampleItems)
            {
                var minimumPrice = GetMinimumPrice(item.BasePrice);
                var maximumPrice = GetMaximumPrice(item.BasePrice);
                for (var copyIndex = 0; copyIndex < SampleCopiesPerItem; copyIndex++)
                {
                    drafts.Add(new StockDraft(
                        item.ItemId,
                        item.BasePrice,
                        minimumPrice,
                        maximumPrice,
                        random.Next(minimumPrice, maximumPrice + 1)));
                }
            }

            BalanceTotalPrice(drafts, random);
            Shuffle(drafts, random);
            return drafts
                .Select((draft, index) => new CityStockEntry(
                    $"{cityGroupId}_stock_{index + 1:D3}",
                    draft.ItemId,
                    draft.BasePrice,
                    draft.Price))
                .ToArray();
        }

        public static int GetMinimumPrice(int basePrice)
        {
            return checked(((basePrice * MinimumPricePercent) + 99) / 100);
        }

        public static int GetMaximumPrice(int basePrice)
        {
            return checked((basePrice * MaximumPricePercent) / 100);
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
                int itemId,
                int basePrice,
                int minimumPrice,
                int maximumPrice,
                int price)
            {
                ItemId = itemId;
                BasePrice = basePrice;
                MinimumPrice = minimumPrice;
                MaximumPrice = maximumPrice;
                Price = price;
            }

            public int ItemId { get; }
            public int BasePrice { get; }
            public int MinimumPrice { get; }
            public int MaximumPrice { get; }
            public int Price { get; set; }
        }
    }
}
