using System.Threading.Tasks;

namespace olx_be_api.Services
{
    public class DokuService : IDokuService
    {
        public async Task<DokuPaymentResponse> CreatePayment(DokuPaymentRequest request)
        {
            await Task.Delay(500);
            return new DokuPaymentResponse
            {
                IsSuccess = true,
                PaymentUrl = $"https://checkout.doku.com/simulasi/{request.InvoiceNumber}"
            };

            // Simulasi Gagal:
             return new DokuPaymentResponse { IsSuccess = false, ErrorMessage = "Gagal terhubung ke Doku" };
        }
    }
}