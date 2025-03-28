using FinancialInstrumentsApi.Models;
using System.Collections.Concurrent;

namespace FinancialInstrumentsApi.Services
{
    public class PriceCache : IPriceCache
    {
        private readonly ConcurrentDictionary<string, ForexPrice> _prices = new();

        public void UpdatePrice(ForexPrice price)
        {
            _prices.AddOrUpdate(price.Ticker, price, (_, _) => price);
        }

        public ForexPrice GetLatestPrice(string ticker)
        {
            if (_prices.TryGetValue(ticker, out var price))
            {
                return price;
            }
            return null;
        }

        public IEnumerable<string> GetAvailablePairs()
        {
            return _prices.Keys.ToList();
        }
    }
}
