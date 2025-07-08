namespace olx_be_api.Models
{
    public class MidtransRequest
    {
        public string TransactionDetails { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public MidtransCallbacks Callbacks { get; set; }
        public CustomerDetails CustomerDetails { get; set; }
        public List<ItemDetails> ItemDetails { get; set; }
    }

    public class CustomerDetails
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class ItemDetails
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class SnapResponse
    {
        public string Token { get; set; }
        public string RedirectUrl { get; set; }
    }

    public class MidtransResponse
    {
        public bool IsSuccess { get; set; }
        public string SnapToken { get; set; }
        public string RedirectUrl { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MidtransConfig
    {
        public string ServerKey { get; set; }
        public string ClientKey { get; set; }
        public bool IsProduction { get; set; }
    }

    public class MidtransNotification
    {
        public string status_code { get; set; }
        public string status_message { get; set; }
        public string transaction_id { get; set; }
        public string order_id { get; set; }
        public string gross_amount { get; set; }
        public string payment_type { get; set; }
        public string transaction_time { get; set; }
        public string transaction_status { get; set; }
        public string fraud_status { get; set; }
        public string signature_key { get; set; }
    }
}
