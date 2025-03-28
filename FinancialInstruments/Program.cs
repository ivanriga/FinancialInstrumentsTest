using FinancialInstrumentsApi.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders(); // Optional: Clears default logging providers
builder.Logging.AddConsole(); // Add Console Logging

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Register services
builder.Services.AddHttpClient();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPriceCache, PriceCache>();
builder.Services.AddSingleton<ITiingoService, TiingoService>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("MarketDataPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});
builder.Services.AddSingleton<IWebSocketServerService, WebSocketServerService>();

var app = builder.Build();
// Register in Program.cs
app.UseMiddleware<ExceptionMiddleware>();
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
// Start WebSocket connection
var tiingoService = app.Services.GetRequiredService<ITiingoService>();
var webSocketService = app.Services.GetRequiredService<IWebSocketServerService>();
webSocketService.Start("ws://0.0.0.0:5001");
var cancellationTokenSource = new CancellationTokenSource();

try
{
    await tiingoService.ConnectAsync(cancellationTokenSource.Token);

    // Subscribe to some default pairs
    await tiingoService.SubscribeToPair(["eurusd", "usdjpy", "btcusd", "audusd"]);

}
catch (Exception ex)
{
    Console.WriteLine($"Failed to connect to Tiingo: {ex.Message}");
}



app.Run();
