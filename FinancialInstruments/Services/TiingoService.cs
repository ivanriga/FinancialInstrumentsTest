using FinancialInstrumentsApi.Models;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using NJson = Newtonsoft.Json;
using Fleck;
using System.Collections.Concurrent;

namespace FinancialInstrumentsApi.Services
{
    public class TiingoService : ITiingoService, IDisposable
    {

        private readonly IPriceCache _priceCache;
        private readonly IWebSocketServerService _webSocketServerService;
        private readonly ILogger<TiingoService> _logger;
        private ClientWebSocket _webSocket;
        private readonly string _apiKey;
        private readonly string _url;
        private readonly ConcurrentDictionary<string, decimal> _lastPrices = new();
        public TiingoService(
            IConfiguration config,
            IPriceCache priceCache,
            IWebSocketServerService webSocketServerService,
            ILogger<TiingoService> logger)
        {
            _priceCache = priceCache;
            _webSocketServerService = webSocketServerService;
            _logger = logger;
            _apiKey = config["ApiKey"] ?? throw new ArgumentNullException("ApiKey cannot be null");
            _url = config["Tiingo:WebSocketUrl"] ?? throw new ArgumentNullException("WebSocketUrl cannot be null");
            _webSocket = new ClientWebSocket();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var uri = new Uri(_url);

            await _webSocket.ConnectAsync(uri, cancellationToken);

            _ = Task.Run(() => ReceiveMessagesAsync(cancellationToken), cancellationToken);

            _logger.LogInformation("Connected to Tiingo WebSocket API");
        }

        public async Task SubscribeToPair(string[] pairs)
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var subscriptionMessage = new
            {
                eventName = "subscribe",
                authorization = _apiKey,
                eventData = new
                {
                    thresholdLevel = 5,
                    tickers = pairs
                }
            };

            var json = JsonSerializer.Serialize(subscriptionMessage);
            var bytes = Encoding.UTF8.GetBytes(json);

            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested &&
                   _webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogDebug("Received message: {Message}", message);
                        ProcessMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning("Server initiated connection closure");
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            string.Empty,
                            cancellationToken);
                        await HandleDisconnection(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Receive operation canceled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving message");
                    await HandleDisconnection(cancellationToken);
                }
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var tiingoMessage = NJson.JsonConvert.DeserializeObject<dynamic>(message);

                if (tiingoMessage != null && tiingoMessage?.messageType == "A" && tiingoMessage?.data != null)
                {
                    var price = tiingoMessage?.data;
                    if (price == null || price[1] == null)
                    {
                        return;
                    }
                    ForexPrice forexPrice = new()
                    {
                        Ticker = price[1],
                        Timestamp = DateTime.Parse(price[2].ToString()),
                        BidSize = price[3],
                        BidPrice = price[4],
                        MidPrice = price[5],
                        AskSize = price[6],
                        AskPrice = price[7]
                    };

                    _priceCache.UpdatePrice(forexPrice);
                    _logger.LogDebug("Updated price for {Ticker}: {Bid}/{Ask}",
                        forexPrice.Ticker, forexPrice.BidPrice, forexPrice.AskPrice);
                    if (NeedToUpdatePrice(forexPrice))
                    {
                        //Broadcast only changed prices
                        _webSocketServerService.BroadcastAsync(forexPrice);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing WebSocket message");
            }
        }

        private async Task HandleDisconnection(CancellationToken cancellationToken)
        {
            try
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client closing",
                        CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnection");
            }
            finally
            {
                await ReconnectAsync(cancellationToken);
            }

        }

        private async Task ReconnectAsync(CancellationToken cancellationToken)
        {
            // Attempt to reconnect
            _logger.LogWarning("Attempting to reconnect...");
            await Task.Delay(5000, cancellationToken);
            await ConnectAsync(cancellationToken);
        }

        private bool NeedToUpdatePrice(ForexPrice forexPrice)
        {
            if (forexPrice.MidPrice == null)
            {
                return false;
            }
            if (_lastPrices.ContainsKey(forexPrice.Ticker))
            {
                var lastPrice = _lastPrices[forexPrice.Ticker];
                if (forexPrice.MidPrice != lastPrice)
                {
                    _lastPrices[forexPrice.Ticker] = (decimal)forexPrice.MidPrice;
                    return true;
                }
            }
            else
            {
                _lastPrices[forexPrice.Ticker] = (decimal)forexPrice.MidPrice;
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
        }
    }
}
