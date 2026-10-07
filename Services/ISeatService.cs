using BusTicketBookingSystem.Models;

namespace BusTicketBookingSystem.Services
{
    public interface ISeatService
    {
        Task<List<BusSeat>> GetSeatsForScheduleAsync(int scheduleId);
        Task<bool> ReserveSeatAsync(int scheduleId, int seatNumber);
        Task ReleaseSeatAsync(int busSeatId);
        Task InitializeSeatsAsync(int scheduleId, int totalSeats);
    }
}