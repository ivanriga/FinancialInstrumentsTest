namespace FinancialInstrumentsApi.Services
{

    /// <summary>
    /// Tiingo WebSocket Service
    /// </summary>
    public interface ITiingoService
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SubscribeToPair(string[] pairs);
    }
}
