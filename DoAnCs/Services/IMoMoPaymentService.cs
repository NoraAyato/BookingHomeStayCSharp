using DoAnCs.Models.Momo;

namespace DoAnCs.Services
{
    public interface IMoMoPaymentService
    {
        Task<MoMoPaymentResponse> CreatePaymentAsync(string orderId, long amount, string orderInfo);
    }
}
