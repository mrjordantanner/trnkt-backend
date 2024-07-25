using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Trnkt.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NftController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NftController> _logger;
        private readonly string baseUrl;
        private readonly string apiKey;

        public NftController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<NftController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            baseUrl = "https://api.opensea.io/api/v2";
            apiKey = _configuration["OPENSEA_API_KEY"];
        }

        [HttpGet("fetchNft/{chain}/{address}/{id}")]
        public async Task<IActionResult> GetNftAsync(string chain, string address, string id)
        {
            _logger.LogInformation("Fetching NFT with chain: {chain}, address: {address}, id: {id}", chain, address, id);
            var endpoint = $"/chain/{chain}/contract/{address}/nfts/{id}";
            return await FetchDataAsync(endpoint);
        }

        [HttpGet("fetchNfts/{collectionSlug}")]
        public async Task<IActionResult> GetNftBatchAsync(
            string collectionSlug, 
            [FromQuery] int limit = 50, 
            [FromQuery] string next = null)
        {
            _logger.LogInformation("Fetching NFT batch for collection: {collectionSlug}", collectionSlug);
            var endpoint = $"/collection/{collectionSlug}/nfts";
            
            if (limit <= 0 || limit > 200) limit = 50;
            endpoint += $"?limit={limit}";

            if (!string.IsNullOrEmpty(next)) {
                endpoint += $"&next={next}";
            }
            return await FetchDataAsync(endpoint);
        }

        [HttpGet("fetchCollections")]
        public async Task<IActionResult> GetCollectionBatchAsync(
            [FromQuery] string chain = "base", 
            [FromQuery] int limit = 50, 
            [FromQuery] string next = null)
        {
            var endpoint = $"/collections";//?chain={chain}";
            
            // if (limit <= 0 || limit > 200) {
            //     limit = 50;
            // }
            // endpoint += $"&limit={limit}";

            // if (!string.IsNullOrEmpty(next)) {
            //     endpoint += $"&next={next}";
            // }

            _logger.LogInformation("Fetching {limit} Collections on Blockchain {chain}...", limit, chain);
            return await FetchDataAsync(endpoint);
        }

        [HttpGet("fetchCollection/{collectionSlug}")]
        public async Task<IActionResult> GetCollectionAsync(
            string collectionSlug)
        {
            var endpoint = $"/collections/{collectionSlug}`";

            _logger.LogInformation("Fetching Collection {collectionSlug}...", collectionSlug);
            return await FetchDataAsync(endpoint);
        }

        private async Task<IActionResult> FetchDataAsync(string endpoint)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}{endpoint}");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("X-API-KEY", apiKey);

            try
            {
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Successfully fetched data from {endpoint}", endpoint);
                    return Content(data, "application/json");
                }
                else
                {
                    _logger.LogWarning("Failed to fetch data from {endpoint}. Status Code: {statusCode}, Reason: {reason}", endpoint, response.StatusCode, response.ReasonPhrase);
                    return StatusCode((int)response.StatusCode, response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching data from {endpoint}", endpoint);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}