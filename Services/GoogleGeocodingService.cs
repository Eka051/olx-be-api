using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace olx_be_api.Services
{
    public class GoogleGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;

        public GoogleGeocodingService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleMaps:ApiKey"];
        }

        public async Task<LocationDetails?> GetLocationDetailsFromCoordinates(double lat, double lng)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return null;
            }

            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={_apiKey}&language=id";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var geocodingResponse = JsonSerializer.Deserialize<GoogleGeocodingResponse>(jsonResponse, options);

            if (geocodingResponse == null || geocodingResponse.Status != "OK" || !geocodingResponse.Results.Any())
            {
                return null;
            }

            var firstResult = geocodingResponse.Results.First();

            string? FindComponent(string type) =>
                firstResult.AddressComponents.FirstOrDefault(c => c.Types.Contains(type))?.LongName;

            var details = new LocationDetails
            {
                Province = FindComponent("administrative_area_level_1"),
                City = FindComponent("administrative_area_level_2"),
                District = FindComponent("administrative_area_level_3"),
                FullAddress = firstResult.FormattedAddress
            };

            return details;
        }
    }
}
