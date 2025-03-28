using FinancialInstrumentsApi.Models;
using Fleck;
using IBApi;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FinancialInstrumentsApi.Services
{
    public class WebSocketServerService: IWebSocketServerService
    {
        private readonly ConcurrentDictionary<Guid, IWebSocketConnection> _clients = new();
        private WebSocketServer _server;
        private readonly ILogger<WebSocketServerService> _logger;


        private ConcurrentDictionary<string, List<IWebSocketConnection>>  subscribedClients = new();


        public WebSocketServerService(ILogger<WebSocketServerService> logger)
        {
            _logger = logger;
        }

        public void Start(string location)
        {
            _server = new WebSocketServer(location);
            _server.Start(ws =>
            {
                ws.OnOpen = () => _clients.TryAdd(ws.ConnectionInfo.Id, ws);
                ws.OnClose = () => _clients.TryRemove(ws.ConnectionInfo.Id, out _);
                ws.OnMessage = message => ProcessMessage(ws, message);
            });
        }

        private void ProcessMessage(IWebSocketConnection ws, string message)
        {
            try
            {
                var json = JsonDocument.Parse(message);
                var method = json.RootElement.GetProperty("method").GetString();

                if (method == "SUBSCRIBE")
                {
                    var streamNames = json.RootElement.GetProperty("params")
                        .EnumerateArray()
                        .Select(x => x.GetString())
                        .ToList();

                    var id = json.RootElement.GetProperty("id").GetInt32();

                    foreach (var stream in streamNames)
                    {
                        if (  stream == null  || stream=="")
                        {
                            ws.Send("Invalid params");
                            _logger.LogError($"Client {ws.ConnectionInfo.Id} has invalid params");
                            return;
                        }
                        if (!subscribedClients.ContainsKey(stream))
                        {
                            subscribedClients[stream] = new List<IWebSocketConnection>();
                        }

                        if (!subscribedClients[stream].Contains(ws))
                        {
                            subscribedClients[stream].Add(ws);
                        }

                        _logger.LogInformation($"Client {ws.ConnectionInfo.Id} subscribed to {stream}");
                    }

                    // Send subscription confirmation
                    ws.Send(JsonSerializer.Serialize(new
                    {
                        result = "SUBSCRIBED",
                        id
                    }));
                }
                else if (method == "UNSUBSCRIBE")
                {
                    foreach (var clients in subscribedClients)
                    {
                        if (clients.Value.Contains(ws))
                        {
                            clients.Value.Remove(ws);
                            _logger.LogInformation($"Client {ws.ConnectionInfo.Id} unsubscribed from {clients.Key}");
                        }
                    }
                    ws.Send("UNSUBSCRIBED");
                 }
                else
                {
                    ws.Send("Unknown method");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
                ws.Send("Invalid message format");
            }
        }

        public async Task BroadcastAsync(ForexPrice forexPrice)
        {
            //simulation of multiple subscribers: imax = 1000;
            int imax = 1;
            for (int i = 0; i < imax; i++)
            {

                foreach (var client in _clients.Values.Where(c => c.IsAvailable))
                {
                    if (subscribedClients.ContainsKey(forexPrice.Ticker) && subscribedClients[forexPrice.Ticker].Contains(client))
                    {
                        string message = JsonSerializer.Serialize(forexPrice);
                        await client.Send(message);
                        //  _logger.LogDebug($"ClientId:{client.ConnectionInfo.Id} Ticker:{forexPrice.Ticker} {forexPrice.MidPrice}");
                    }
                }
            }

        }

        public async Task SendToClientAsync(Guid connectionId, string message)
        {
            if (_clients.TryGetValue(connectionId, out var client) && client.IsAvailable)
            {
                await client.Send(message);
            }
        }
    }
}
