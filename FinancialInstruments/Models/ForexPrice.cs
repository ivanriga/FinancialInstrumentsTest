using System.Text.Json.Serialization;

namespace FinancialInstrumentsApi.Models
{
    public class ForexPrice
    {
        [JsonPropertyName("ticker")]
        public required string Ticker { get; set; }
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }
        [JsonPropertyName("bidSize")]
        public decimal? BidSize { get; set; }
        [JsonPropertyName("bidPrice")]
        public decimal? BidPrice { get; set; }
        [JsonPropertyName("midPrice")]
        public decimal? MidPrice { get; set; }
        [JsonPropertyName("askSize")]
        public decimal? AskSize { get; set; }
        [JsonPropertyName("askPrice")]
        public decimal? AskPrice { get; set; }
    }
}
