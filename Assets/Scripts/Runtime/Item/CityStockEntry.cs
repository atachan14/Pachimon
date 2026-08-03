using System;

namespace Pachimon.Items
{
    public sealed class CityStockEntry
    {
        public CityStockEntry(
            string stockId,
            int itemId,
            int basePrice,
            int price)
        {
            if (string.IsNullOrWhiteSpace(stockId))
            {
                throw new ArgumentException("Stock ID is required.", nameof(stockId));
            }

            if (itemId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            if (basePrice <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(basePrice));
            }

            if (price <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(price));
            }

            StockId = stockId;
            ItemId = itemId;
            BasePrice = basePrice;
            Price = price;
        }

        public string StockId { get; }
        public int ItemId { get; }
        public int BasePrice { get; }
        public int Price { get; }
        public bool IsPurchased { get; private set; }

        public bool TryMarkPurchased()
        {
            if (IsPurchased)
            {
                return false;
            }

            IsPurchased = true;
            return true;
        }
    }
}
