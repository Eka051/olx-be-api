using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace olx_be_api.Services
{
    public class DokuService : IDokuService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DokuService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<DokuPaymentResponse> CreatePayment(DokuPaymentRequest request)
        {
            var dokuConfig = _configuration.GetSection("DokuSettings");
            var clientId = dokuConfig["ClientId"];
            var secretKey = dokuConfig["SecretKey"];
            var apiUrl = dokuConfig["ApiUrl"] + "/checkout/v1/payment";
            var callbackUrl = dokuConfig["CallbackUrl"];

            var requestId = Guid.NewGuid().ToString();
            var requestTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

            var payload = new
            {
                order = new
                {
                    amount = request.Amount,
                    invoice_number = request.InvoiceNumber,
                    currency = "IDR",
                    callback_url = callbackUrl,
                    line_items = request.LineItems.Select(item => new
                    {
                        name = item.Name,
                        price = item.Price,
                        quantity = item.Quantity
                    }).ToList()
                },
                payment = new
                {
                    payment_due_date = 60
                },
                customer = new
                {
                    name = request.CustomerName,
                    email = request.CustomerEmail
                }
            };

            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            var requestJson = JsonSerializer.Serialize(payload, options);

            var digest = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(requestJson)));
            var signatureComponent = $"Client-Id:{clientId}\nRequest-Id:{requestId}\nRequest-Timestamp:{requestTimestamp}\nRequest-Target:/checkout/v1/payment\nDigest:{digest}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey!));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureComponent)));

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            httpRequest.Headers.Add("Client-Id", clientId);
            httpRequest.Headers.Add("Request-Id", requestId);
            httpRequest.Headers.Add("Request-Timestamp", requestTimestamp);
            httpRequest.Headers.Add("Signature", $"HMACSHA256={signature}");
            httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new DokuPaymentResponse { IsSuccess = false, ErrorMessage = $"Gagal membuat pembayaran: {responseBody}" };
            }

            var dokuResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var paymentUrl = dokuResponse.GetProperty("response").GetProperty("payment").GetProperty("url").GetString();

            return new DokuPaymentResponse { IsSuccess = true, PaymentUrl = paymentUrl! };
        }
    }
}