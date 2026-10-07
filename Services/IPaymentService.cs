using BusTicketBookingSystem.Models;

namespace BusTicketBookingSystem.Services
{
    public interface IPaymentService
    {
        Task<PaymentRequest> SubmitRequestAsync(int bookingId, decimal amount, string method, string? reference);
        Task<List<PaymentRequest>> GetPendingRequestsAsync();
        Task<bool> ApproveAsync(int requestId, int adminId, string? remarks);
        Task<bool> RejectAsync(int requestId, int adminId, string remarks);
        Task<PaymentRequest?> GetByIdAsync(int requestId);
    }
}