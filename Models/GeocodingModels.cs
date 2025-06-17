using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace olx_be_api.Models
{
    public class GoogleGeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingResult> Results { get; set; } = new List<GeocodingResult>();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class GeocodingResult
    {
        [JsonPropertyName("address_components")]
        public List<AddressComponent> AddressComponents { get; set; } = new List<AddressComponent>();

        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; set; } = string.Empty;
    }

    public class AddressComponent
    {
        [JsonPropertyName("long_name")]
        public string LongName { get; set; } = string.Empty;

        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = new List<string>();
    }
}
