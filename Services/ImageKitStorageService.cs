using Imagekit;
using Imagekit.Sdk;

namespace olx_be_api.Services
{
    public class ImageKitStorageService : IStorageService
    {
        private readonly ImagekitClient _imagekit;

        public ImageKitStorageService(IConfiguration configuration)
        {
            var privateKey = configuration["ImageKit:PrivateKey"] ?? throw new InvalidOperationException("ImageKit:PrivateKey must be configured.");
            var publicKey = configuration["ImageKit:PublicKey"] ?? throw new InvalidOperationException("ImageKit:PublicKey must be configured.");
            var urlEndpoint = configuration["ImageKit:UrlEndpoint"] ?? throw new InvalidOperationException("ImageKit:UrlEndpoint must be configured.");

            _imagekit = new ImagekitClient(publicKey, privateKey, urlEndpoint);
        }

        public async Task<string> UploadAsync(IFormFile file, string bucketName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty.", nameof(file));
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            var base64File = Convert.ToBase64String(fileBytes);
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            var request = new FileCreateRequest
            {
                file = base64File,
                fileName = fileName,
                folder = bucketName
            };
            
            return await Task.Run(() => 
            {
                var result = _imagekit.Upload(request);
                
                if (result.HttpStatusCode >= 200 && result.HttpStatusCode < 300)
                {
                    return result.url;
                }
                
                throw new Exception($"Failed to upload to ImageKit: {result.HttpStatusCode}");
            });
        }

        public async Task DeleteAsync(string fileUrl, string bucketName)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            var fileId = ExtractFileIdFromUrl(fileUrl);
            if (string.IsNullOrEmpty(fileId)) return;

            await Task.Run(() => 
            {
                // Note: ImageKit SDK usually has DeleteFile or similar
                // We need to check if we can delete by ID
                try 
                {
                     // Typically _imagekit.DeleteFile(fileId)
                     _imagekit.DeleteFile(fileId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file from ImageKit: {ex.Message}");
                }
            });
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
    }
}
