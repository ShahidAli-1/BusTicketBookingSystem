using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Services
{
    public class SeatService : ISeatService
    {
        private readonly ApplicationDbContext _db;

        public SeatService(ApplicationDbContext db)
        {
            _db = db;
        }

       
        public async Task<List<BusSeat>> GetSeatsForScheduleAsync(int scheduleId)
        {
            return await _db.BusSeats
                .Where(s => s.ScheduleID == scheduleId)
                .OrderBy(s => s.SeatNumber)
                .ToListAsync();
        }

        
        public async Task InitializeSeatsAsync(int scheduleId, int totalSeats)
        {
            var existing = await _db.BusSeats
                .CountAsync(s => s.ScheduleID == scheduleId);

            if (existing > 0) return;

            for (int i = 1; i <= totalSeats; i++)
            {
                _db.BusSeats.Add(new BusSeat
                {
                    ScheduleID = scheduleId,
                    SeatNumber = i,
                    Status = "Available"
                });
            }

            await _db.SaveChangesAsync();
        }

     
        public async Task<bool> ReserveSeatAsync(int scheduleId, int seatNumber)
        {
            var seat = await _db.BusSeats
                .FirstOrDefaultAsync(s =>
                    s.ScheduleID == scheduleId &&
                    s.SeatNumber == seatNumber);

            if (seat == null || seat.Status != "Available")
                return false;

            seat.Status = "Reserved";
            await _db.SaveChangesAsync();
            return true;
        }

       
        public async Task ReleaseSeatAsync(int busSeatId)
        {
            var seat = await _db.BusSeats.FindAsync(busSeatId);
            if (seat != null)
            {
                seat.Status = "Available";
                await _db.SaveChangesAsync();
            }
        }
    }
}