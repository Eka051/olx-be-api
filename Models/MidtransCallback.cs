using System.Text.Json.Serialization;

namespace olx_be_api.Models
{
    public class MidtransCallbacks
    {
        [JsonPropertyName("finish")]
        public string Finish { get; set; }
    }
}
