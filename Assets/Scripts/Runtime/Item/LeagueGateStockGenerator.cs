using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Items
{
    public sealed class LeagueGateStockGenerator
    {
        public const int RecoveryCopiesPerItem = 5;
        public const int EngravingCopiesPerStat = 3;
        public const int MinimumRecoveryPercent = 70;
        public const int MaximumRecoveryPercent = 100;

        private static readonly int[] RecoveryItemIds =
        {
            ItemIds.SuperPotion,
            ItemIds.SuperMnPotion,
            ItemIds.SuperRecovery,
            ItemIds.MaxRevive,
        };

        public IReadOnlyList<CityStockEntry> Generate(
            int shopSeed,
            ItemCatalog itemCatalog)
        {
            if (itemCatalog == null)
                throw new ArgumentNullException(nameof(itemCatalog));

            var random = new Random(unchecked(shopSeed ^ 0x4C454147));
            var entries = new List<CityStockEntry>();
            foreach (var itemId in RecoveryItemIds)
            {
                var item = GetRequiredItem<HealingItemAsset>(itemCatalog, itemId);
                var effects = Enumerable.Range(0, RecoveryCopiesPerItem)
                    .Select(_ => random.Next(
                        MinimumRecoveryPercent,
                        MaximumRecoveryPercent + 1))
                    .OrderBy(value => value)
                    .ToArray();
                var prices = CreateRankedPrices(
                    item.BasePrice,
                    RecoveryCopiesPerItem,
                    random);
                for (var index = 0; index < RecoveryCopiesPerItem; index++)
                {
                    entries.Add(CreateEntry(
                        entries.Count,
                        item,
                        prices[index],
                        effects[index]));
                }
            }

            var engravings = itemCatalog.Items
                .OfType<EngravingItemAsset>()
                .Where(item => PachimonStatTypeUtility.IsGeneratedStat(
                    item.TargetStat))
                .OrderBy(item => item.TargetStat)
                .ToArray();
            var expectedStats = Enumerable.Range(0, (int)PachimonStatType.Count)
                .Select(index => (PachimonStatType)index)
                .Where(PachimonStatTypeUtility.IsGeneratedStat)
                .ToArray();
            if (engravings.Length != expectedStats.Length
                || expectedStats.Any(stat => engravings.All(
                    engraving => engraving.TargetStat != stat)))
            {
                throw new InvalidOperationException(
                    "League Gate requires one Engraving Item for every generated Stat.");
            }

            foreach (var engraving in engravings)
            {
                var effects = CreateRankedValues(
                    checked(
                        engraving.BaseEffectValue
                        * CityStockGenerator.EngravingMainEffectUnits),
                    EngravingCopiesPerStat,
                    random);
                var prices = CreateRankedPrices(
                    engraving.BasePrice,
                    EngravingCopiesPerStat,
                    random);
                for (var index = 0; index < EngravingCopiesPerStat; index++)
                {
                    var downside = engravings[random.Next(engravings.Length - 1)];
                    if (downside.TargetStat == engraving.TargetStat)
                        downside = engravings[^1];
                    var mainBaseValue = checked(
                        engraving.BaseEffectValue
                        * CityStockGenerator.EngravingMainEffectUnits);
                    var downsideValue = Math.Max(
                        1,
                        (int)Math.Floor(
                            downside.BaseEffectValue
                            * (decimal)effects[index]
                            / mainBaseValue
                            * CityStockGenerator.EngravingDownsideEffectUnits));
                    entries.Add(CreateEntry(
                        entries.Count,
                        engraving,
                        prices[index],
                        null,
                        new[]
                        {
                            new GeneratedStatChange(
                                engraving.TargetStat,
                                effects[index]),
                            new GeneratedStatChange(
                                downside.TargetStat,
                                -downsideValue),
                        }));
                }
            }

            return entries;
        }

        private static T GetRequiredItem<T>(ItemCatalog catalog, int itemId)
            where T : ItemAsset
        {
            if (catalog.Get(itemId) is not T item || item.BasePrice <= 0)
            {
                throw new InvalidOperationException(
                    $"League Gate requires Item {itemId} ({typeof(T).Name}).");
            }
            return item;
        }

        private static CityStockEntry CreateEntry(
            int index,
            ItemAsset item,
            int price,
            int? primaryEffectValue = null,
            IReadOnlyList<GeneratedStatChange> statChanges = null)
        {
            return new CityStockEntry(
                $"league_gate_stock_{index + 1:D3}",
                new GeneratedItemData(
                    item.ItemId,
                    primaryEffectValue,
                    statChanges),
                item.BasePrice,
                price);
        }

        private static int[] CreateRankedPrices(
            int basePrice,
            int count,
            Random random)
        {
            var minimum = CityStockGenerator.GetMinimumPrice(basePrice);
            var maximum = CityStockGenerator.GetMaximumPrice(basePrice);
            return Enumerable.Range(0, count)
                .Select(_ => random.Next(minimum, maximum + 1))
                .OrderBy(value => value)
                .ToArray();
        }

        private static int[] CreateRankedValues(
            int baseValue,
            int count,
            Random random)
        {
            var minimum = checked(
                (baseValue * CityStockGenerator.MinimumEffectPercent + 99) / 100);
            var maximum = checked(
                baseValue * CityStockGenerator.MaximumEffectPercent / 100);
            return Enumerable.Range(0, count)
                .Select(_ => random.Next(minimum, maximum + 1))
                .OrderBy(value => value)
                .ToArray();
        }
    }
}
