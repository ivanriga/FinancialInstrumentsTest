using FinancialInstrumentsApi.Models;

namespace FinancialInstrumentsApi.Services
{

    public interface IMarketDataProvider
    {
        /// <summary>
        /// Gets the current market price for a financial instrument
        /// </summary>
        /// <param name="symbol">Instrument symbol (e.g., EURUSD, BTCUSD)</param>
        /// <returns>Current market price</returns>
        Task<decimal> GetPriceAsync(string symbol);

    }


}

