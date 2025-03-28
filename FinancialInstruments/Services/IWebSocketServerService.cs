using FinancialInstrumentsApi.Models;
using System;
using System.Threading.Tasks;

namespace FinancialInstrumentsApi.Services
{
    public interface IWebSocketServerService
    {
        void Start(string location);
        Task BroadcastAsync(ForexPrice forexPrice);
        Task SendToClientAsync(Guid connectionId, string message);
    }
}

