using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Trnkt.Models;

namespace Trnkt.Services
{
    public class NftService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.opensea.io/api/v2";

        public NftService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = Environment.GetEnvironmentVariable("OPENSEA_API_KEY");

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("OPENSEA_API_KEY environment variable is not set.");
            }
        }

        public async Task<List<Nft>> GetBatchFromCollectionAsync(string collectionSlug)
        {
            var requestUri = $"{BaseUrl}/collection/{collectionSlug}/nfts";
            var options = new HttpRequestMessage(HttpMethod.Get, requestUri);
            options.Headers.Add("accept", "application/json");
            options.Headers.Add("X-API-KEY", _apiKey);

            try
            {
                var response = await _httpClient.SendAsync(options);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsStringAsync();
                var nftResponse = JsonSerializer.Deserialize<NftBatchResponse>(data);
                return nftResponse?.Nfts ?? new List<Nft>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<Nft>();
            }
        }

        public async Task<Nft> GetNftAsync(string id, string chain, string address)
        {
            var requestUri = $"{BaseUrl}/chain/{chain}/contract/{address}/nfts/{id}";
            var options = new HttpRequestMessage(HttpMethod.Get, requestUri);
            options.Headers.Add("accept", "application/json");
            options.Headers.Add("X-API-KEY", _apiKey);

            try
            {
                var response = await _httpClient.SendAsync(options);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsStringAsync();
                var nft = JsonSerializer.Deserialize<Nft>(data);
                return nft;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}
