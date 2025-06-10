namespace olx_be_api.Services
{
    public class LocationDetails
    {
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? FullAddress { get; set; }
    }

    public interface IGeocodingService
    {
        Task<LocationDetails?> GetLocationDetailsFromCoordinates(double lat, double lng);
    }
}
