using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

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
        return await FetchDataAsync(baseUrl, endpoint, apiKey);
    }

    [HttpGet("fetchNfts/{collectionSlug}")]
    public async Task<IActionResult> GetNftBatchAsync(string collectionSlug)
    {
        _logger.LogInformation("Fetching NFT batch for collection: {collectionSlug}", collectionSlug);
        var endpoint = $"/collection/{collectionSlug}/nfts";
        return await FetchDataAsync(baseUrl, endpoint, apiKey);
    }

    private async Task<IActionResult> FetchDataAsync(string baseUrl, string endpoint, string apiKey)
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
                // Return JSON content
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
