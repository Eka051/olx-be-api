using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Supabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace olx_be_api.Services
{
    public class SupabaseStorageService : IStorageService
    {
        private readonly Client _supabaseClient;

        public SupabaseStorageService(IConfiguration configuration)
        {
            var supabaseUrl = configuration["Supabase:Url"];
            var supabaseKey = configuration["Supabase:ServiceRoleKey"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                throw new InvalidOperationException("Supabase Url and ServiceRoleKey must be configured.");
            }

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            _supabaseClient = new Client(supabaseUrl, supabaseKey, options);
        }

        public async Task<string> UploadAsync(IFormFile file, string bucketName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty.", nameof(file));
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            await _supabaseClient.Storage
                .From(bucketName)
                .Upload(memoryStream.ToArray(), fileName);

            var publicUrl = _supabaseClient.Storage
                .From(bucketName)
                .GetPublicUrl(fileName);

            return publicUrl;
        }

        public async Task DeleteAsync(string fileUrl, string bucketName)
        {
            if (string.IsNullOrEmpty(fileUrl) || string.IsNullOrEmpty(bucketName))
            {
                return;
            }

            try
            {
                var uri = new Uri(fileUrl);
                var fileName = Path.GetFileName(uri.LocalPath);

                if (string.IsNullOrEmpty(fileName))
                {
                    return;
                }

                await _supabaseClient.Storage
                    .From(bucketName)
                    .Remove(new List<string> { fileName });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file from storage: {ex.Message}");
            }
        }
    }
}
