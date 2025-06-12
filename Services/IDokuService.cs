namespace olx_be_api.Services
{
    public class DokuLineItem
    {
        public string Name { get; set; } = null!;
        public int Quantity { get; set; }
        public int Price { get; set; }
    }
    public class DokuPaymentRequest
    {
        public string InvoiceNumber { get; set; } = null!;
        public int Amount { get; set; }
        public string ProductName { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public List<DokuLineItem> LineItems { get; set; } = new List<DokuLineItem>();
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
