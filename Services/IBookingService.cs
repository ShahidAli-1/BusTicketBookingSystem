using BusTicketBookingSystem.Models;

namespace BusTicketBookingSystem.Services
{
    public interface IBookingService
    {
        // Single-seat (kept for compatibility)
        Task<Booking> CreateBookingAsync(int passengerId, int scheduleId, int seatNumber);

        // ⭐ Multi-seat
        Task<Booking> CreateMultiSeatBookingAsync(int passengerId, int scheduleId, List<int> seatNumbers);

        Task<Booking?> GetBookingAsync(int bookingId);
        Task<List<Booking>> GetPassengerBookingsAsync(int passengerId);
        Task<bool> ConfirmBookingAsync(int bookingId, int adminId);
        Task<bool> RejectBookingAsync(int bookingId, int adminId, string remarks);
        Task ExpireStaleReservationsAsync();
    }
}