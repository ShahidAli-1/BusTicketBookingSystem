using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _bookingService;

        public PaymentService(ApplicationDbContext db, IBookingService bookingService)
        {
            _db = db;
            _bookingService = bookingService;
        }

        public async Task<PaymentRequest> SubmitRequestAsync(int bookingId, decimal amount, string method, string? reference)
        {
            var booking = await _db.Bookings.FindAsync(bookingId)
                ?? throw new InvalidOperationException("Booking not found.");

            if (booking.Status != "Reserved")
                throw new InvalidOperationException("Only reserved bookings can submit payment.");

            var request = new PaymentRequest
            {
                BookingID = bookingId,
                Amount = amount,
                PaymentMethod = method,
                PaymentReference = reference,
                Status = "Pending",
                SubmittedAt = DateTime.Now
            };

            booking.Status = "PendingApproval";

            _db.PaymentRequests.Add(request);
            await _db.SaveChangesAsync();
            return request;
        }

        public async Task<List<PaymentRequest>> GetPendingRequestsAsync()
        {
            return await _db.PaymentRequests
                .Include(p => p.Booking!).ThenInclude(b => b.Passenger)
                .Include(p => p.Booking!).ThenInclude(b => b.Schedule!).ThenInclude(s => s.Bus!).ThenInclude(b => b.Route)
                .Include(p => p.Booking!).ThenInclude(b => b.BusSeat)
                .Where(p => p.Status == "Pending")
                .OrderBy(p => p.SubmittedAt)
                .ToListAsync();
        }

        public async Task<PaymentRequest?> GetByIdAsync(int requestId)
        {
            return await _db.PaymentRequests
                .Include(p => p.Booking!).ThenInclude(b => b.Passenger)
                .Include(p => p.Booking!).ThenInclude(b => b.Schedule!).ThenInclude(s => s.Bus!).ThenInclude(b => b.Route)
                .Include(p => p.Booking!).ThenInclude(b => b.BusSeat)
                .FirstOrDefaultAsync(p => p.PaymentRequestID == requestId);
        }

        public async Task<bool> ApproveAsync(int requestId, int adminId, string? remarks)
        {
            var req = await _db.PaymentRequests.FindAsync(requestId);
            if (req == null || req.Status != "Pending") return false;

            req.Status = "Approved";
            req.ReviewedBy = adminId;
            req.ReviewedAt = DateTime.Now;
            req.AdminRemarks = remarks;

            var ok = await _bookingService.ConfirmBookingAsync(req.BookingID, adminId);
            if (!ok) return false;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int requestId, int adminId, string remarks)
        {
            var req = await _db.PaymentRequests.FindAsync(requestId);
            if (req == null || req.Status != "Pending") return false;

            req.Status = "Rejected";
            req.ReviewedBy = adminId;
            req.ReviewedAt = DateTime.Now;
            req.AdminRemarks = remarks;

            await _bookingService.RejectBookingAsync(req.BookingID, adminId, remarks);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}