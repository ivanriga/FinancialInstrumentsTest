using FinancialInstrumentsApi.Models;
using System.Collections.Concurrent;

namespace FinancialInstrumentsApi.Services
{
    public interface IPriceCache
    {
        void UpdatePrice(ForexPrice price);
        ForexPrice GetLatestPrice(string ticker);
        IEnumerable<string> GetAvailablePairs();
    }

}
