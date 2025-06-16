namespace olx_be_api.Services
{
    public interface IStorageService
    {
        Task<string> UploadAsync(IFormFile file, string bucketName);
        Task DeleteAsync(string fileName, string bucketName);
    }
}
