using System.Text;
using System.Text.Json;

namespace olx_be_api.Services
{
    public class ImageKitStorageService : IStorageService
    {
        private readonly string _privateKey;
        private readonly string _publicKey;
        private readonly string _urlEndpoint;
        private readonly HttpClient _httpClient;

        public ImageKitStorageService(IConfiguration configuration, HttpClient httpClient)
        {
            _privateKey = configuration["ImageKit:PrivateKey"] ?? throw new InvalidOperationException("ImageKit:PrivateKey must be configured.");
            _publicKey = configuration["ImageKit:PublicKey"] ?? throw new InvalidOperationException("ImageKit:PublicKey must be configured.");
            _urlEndpoint = configuration["ImageKit:UrlEndpoint"] ?? throw new InvalidOperationException("ImageKit:UrlEndpoint must be configured.");
            _httpClient = httpClient;
        }

        public async Task<string> UploadAsync(IFormFile file, string bucketName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty.", nameof(file));
            }

            var fileName = $"{bucketName}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var base64File = Convert.ToBase64String(fileBytes);

            var requestData = new
            {
                file = base64File,
                fileName = fileName
            };

            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_privateKey}:"));
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");

            var response = await _httpClient.PostAsync("https://upload.imagekit.io/api/v1/files/upload", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload to ImageKit: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var uploadResult = JsonSerializer.Deserialize<ImageKitUploadResponse>(responseJson);

            return uploadResult?.url ?? throw new Exception("Upload succeeded but no URL returned");
        }

        public async Task DeleteAsync(string fileUrl, string bucketName)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return;
            }

            try
            {
                var fileId = ExtractFileIdFromUrl(fileUrl);
                if (string.IsNullOrEmpty(fileId))
                {
                    return;
                }

                var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_privateKey}:"));
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");

                var response = await _httpClient.DeleteAsync($"https://api.imagekit.io/v1/files/{fileId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to delete file from ImageKit: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file from ImageKit: {ex.Message}");
            }
        }

        private string ExtractFileIdFromUrl(string fileUrl)
        {
            var segments = fileUrl.Split('/');
            if (segments.Length > 0)
            {
                var lastSegment = segments[^1];
                return Path.GetFileNameWithoutExtension(lastSegment);
            }
            return string.Empty;
        }

        private class ImageKitUploadResponse
        {
            public string? fileId { get; set; }
            public string? name { get; set; }
            public string? url { get; set; }
            public string? thumbnailUrl { get; set; }
        }
    }
}
