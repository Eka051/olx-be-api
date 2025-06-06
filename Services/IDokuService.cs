namespace olx_be_api.Services
{
    public class DokuPaymentRequest
    {
        public int InvoiceNumber { get; set; }
        public int Amount { get; set; }
        public string ProductName { get; set; } = null!;
    }

    public class DokuPaymentResponse
    {
        public bool IsSuccess { get; set; }
        public string PaymentUrl { get; set; } = null!;
        public string? ErrorMessage { get; set; }
    }

    public interface IDokuService
    {
        Task<DokuPaymentResponse> CreatePayment(DokuPaymentRequest request);
    }
}
