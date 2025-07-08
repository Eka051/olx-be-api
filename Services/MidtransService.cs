using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using olx_be_api.Models;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace olx_be_api.Services
{

    public class MidtransService : IMidtransService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MidtransService> _logger;

        public MidtransService(HttpClient httpClient, IConfiguration configuration, ILogger<MidtransService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public MidtransConfig GetConfig()
        {
            var settings = _configuration.GetSection("Midtrans");
            return new MidtransConfig
            {
                ServerKey = settings["ServerKey"]!,
                ClientKey = settings["ClientKey"]!,
                IsProduction = bool.TryParse(settings["IsProduction"], out var isProd) && isProd
            };
        }

        public async Task<MidtransResponse> CreateSnapTransaction(MidtransRequest request)
        {
            var midtransConfig = _configuration.GetSection("Midtrans");
            var serverKey = midtransConfig["ServerKey"];
            var apiUrl = midtransConfig["ApiUrl"];

            if (string.IsNullOrEmpty(serverKey) || string.IsNullOrEmpty(apiUrl))
            {
                _logger.LogError("Konfigurasi Midtrans (ServerKey, ApiUrl) tidak lengkap.");
                return new MidtransResponse { IsSuccess = false, ErrorMessage = "Konfigurasi Midtrans tidak lengkap." };
            }

            if (!apiUrl.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("ApiUrl Midtrans terdeteksi memiliki format tidak standar: {OriginalUrl}", apiUrl);
                int httpIndex = apiUrl.IndexOf("http");
                if (httpIndex > -1)
                {
                    apiUrl = apiUrl.Substring(httpIndex);
                    _logger.LogInformation("ApiUrl Midtrans telah diperbaiki menjadi: {NewUrl}", apiUrl);
                }
            }


            var payload = new
            {
                transaction_details = new
                {
                    order_id = request.InvoiceNumber,
                    gross_amount = request.Amount
                },
                credit_card = new
                {
                    secure = true
                },
                customer_details = new
                {
                    first_name = request.CustomerDetails?.FirstName,
                    last_name = request.CustomerDetails?.LastName,
                    email = request.CustomerDetails?.Email,
                    phone = request.CustomerDetails?.Phone
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
                Formatting = Formatting.None
            });

            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(serverKey + ":"));

            try
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
                requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(requestMessage);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gagal membuat transaksi Midtrans. Status: {StatusCode}, Body: {ResponseBody}", response.StatusCode, responseBody);
                    return new MidtransResponse { IsSuccess = false, ErrorMessage = $"Error Midtrans: {responseBody}" };
                }

                var midtransResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);

                string token = midtransResponse!.token;
                string redirectUrl = midtransResponse.redirect_url;

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(redirectUrl))
                {
                    _logger.LogError("Token atau Redirect URL tidak ditemukan dalam respons Midtrans: {ResponseBody}", responseBody);
                    return new MidtransResponse { IsSuccess = false, ErrorMessage = "Token atau Redirect URL tidak valid dari Midtrans." };
                }

                return new MidtransResponse { IsSuccess = true, SnapToken = token, RedirectUrl = redirectUrl };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Terjadi kesalahan saat membuat transaksi Midtrans.");
                return new MidtransResponse { IsSuccess = false, ErrorMessage = $"Terjadi kesalahan sistem: {ex.Message}" };
            }
        }
    }
}