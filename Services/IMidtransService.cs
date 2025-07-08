using olx_be_api.Models;

namespace olx_be_api.Services
{
    public interface IMidtransService
    {
        Task<MidtransResponse> CreateSnapTransaction(MidtransRequest request);
        MidtransConfig GetConfig();
    }
}
