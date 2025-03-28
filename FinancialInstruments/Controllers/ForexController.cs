using FinancialInstrumentsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinancialInstrumentsApi.Controllers
{
    // Controllers/ForexController.cs
    [ApiController]
    [Route("api/forex")]
    public class ForexController : ControllerBase
    {
        private readonly IPriceCache _priceCache;
        private readonly ITiingoService _tiingoService;

        public ForexController(
            IPriceCache priceCache,
            ITiingoService tiingoService)
        {
            _priceCache = priceCache;
            _tiingoService = tiingoService;
        }



        /// <summary>
        /// Returns list of currency pairs with available prices
        /// </summary>
        /// <returns></returns>
        [HttpGet("pairs")]
        public IActionResult GetAvailablePairs()
        {
            return Ok(_priceCache.GetAvailablePairs());
        }

        /// <summary>
        /// Returns current price for specified currency pair
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        [HttpGet("{pair}/price")]
        public async Task<IActionResult> GetPrice(string pair)
        {
            // Ensure subscribtion to this pair
            try
            {
                await _tiingoService.SubscribeToPair([pair]);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to subscribe to pair: {ex.Message}");
            }

            var price = _priceCache.GetLatestPrice(pair);

            if (price == null)
            {
                return NotFound($"No price data available for {pair}");
            }
            return Ok(price);
        }
    }
}
