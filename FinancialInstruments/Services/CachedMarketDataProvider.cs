using FinancialInstrumentsApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FinancialInstrumentsApi.Services
{

    namespace FinancialInstrumentsApi.Services
    {
        public class CachedMarketDataProvider : IMarketDataProvider
        {
            private readonly IMarketDataProvider _innerProvider;
            private readonly IMemoryCache _cache;
            private readonly TimeSpan _cacheDuration;

            public CachedMarketDataProvider(
                IMarketDataProvider innerProvider,
                IMemoryCache cache,
                IConfiguration config)
            {
                _innerProvider = innerProvider;
                _cache = cache;
                _cacheDuration = config.GetValue<TimeSpan>("Cache:Duration");
            }

            public async Task<decimal> GetPriceAsync(string symbol)
            {
                var cacheKey = $"Price_{symbol}";

                if (_cache.TryGetValue<decimal>(cacheKey, out var cachedPrice))
                {
                    return cachedPrice;
                }

                var price = await _innerProvider.GetPriceAsync(symbol);
                _cache.Set(cacheKey, price, _cacheDuration);

                return price;
            }
        }

        // Register in Program.cs

    }
}
