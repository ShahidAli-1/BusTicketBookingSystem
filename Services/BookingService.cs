using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISeatService _seatService;

        public BookingService(ApplicationDbContext db, ISeatService seatService)
        {
            _db = db;
            _seatService = seatService;
        }

        
        public async Task<Booking> CreateBookingAsync(int passengerId, int scheduleId, int seatNumber)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var reserved = await _seatService.ReserveSeatAsync(scheduleId, seatNumber);
                if (!reserved)
                    throw new InvalidOperationException("Seat is no longer available.");

                var seat = await _db.BusSeats
                    .FirstAsync(s => s.ScheduleID == scheduleId && s.SeatNumber == seatNumber);

                var schedule = await _db.Schedules.FindAsync(scheduleId)
                    ?? throw new InvalidOperationException("Schedule not found.");

                var booking = new Booking
                {
                    BookingNumber = $"BK-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}",
                    PassengerID = passengerId,
                    ScheduleID = scheduleId,
                    BusSeatID = seat.BusSeatID,
                    Fare = schedule.Fare,
                    Status = "Reserved",
                    ReservedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMinutes(15)
                };

                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();

                // ⭐ Also create a BookingSeat row for consistency
                _db.BookingSeats.Add(new BookingSeat
                {
                    BookingID = booking.BookingID,
                    BusSeatID = seat.BusSeatID,
                    Fare = schedule.Fare
                });

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return booking;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

       
        public async Task<Booking> CreateMultiSeatBookingAsync(
            int passengerId,
            int scheduleId,
            List<int> seatNumbers)
        {
            if (seatNumbers == null || seatNumbers.Count == 0)
                throw new InvalidOperationException("No seats selected.");

            if (seatNumbers.Count > 5)
                throw new InvalidOperationException("Maximum 5 seats per booking.");

            // Remove duplicates just in case
            seatNumbers = seatNumbers.Distinct().ToList();

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var schedule = await _db.Schedules.FindAsync(scheduleId)
                    ?? throw new InvalidOperationException("Schedule not found.");

                // Load all requested seats in one query
                var seats = await _db.BusSeats
                    .Where(s => s.ScheduleID == scheduleId && seatNumbers.Contains(s.SeatNumber))
                    .ToListAsync();

                if (seats.Count != seatNumbers.Count)
                    throw new InvalidOperationException("One or more selected seats do not exist.");

                // Verify all are available
                var unavailable = seats.Where(s => s.Status != "Available").ToList();
                if (unavailable.Any())
                {
                    throw new InvalidOperationException(
                        $"Seat(s) {string.Join(", ", unavailable.Select(s => s.SeatNumber))} " +
                        $"are no longer available.");
                }

                // Reserve all seats
                foreach (var seat in seats)
                {
                    seat.Status = "Reserved";
                }

                // Create ONE booking with total fare
                var totalFare = schedule.Fare * seats.Count;

                var booking = new Booking
                {
                    BookingNumber = $"BK-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(100, 999)}",
                    PassengerID = passengerId,
                    ScheduleID = scheduleId,
                    BusSeatID = seats.OrderBy(s => s.SeatNumber).First().BusSeatID, // first seat (legacy)
                    Fare = totalFare,
                    Status = "Reserved",
                    ReservedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMinutes(15)
                };

                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync(); // need BookingID for join rows

                // Create BookingSeat rows (one per selected seat)
                foreach (var seat in seats)
                {
                    _db.BookingSeats.Add(new BookingSeat
                    {
                        BookingID = booking.BookingID,
                        BusSeatID = seat.BusSeatID,
                        Fare = schedule.Fare
                    });
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return booking;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        
        public async Task<Booking?> GetBookingAsync(int bookingId)
        {
            return await _db.Bookings
                .Include(b => b.Schedule!).ThenInclude(s => s.Bus!).ThenInclude(b => b.Route)
                .Include(b => b.BusSeat)
                .Include(b => b.BookingSeats!).ThenInclude(bs => bs.BusSeat)
                .Include(b => b.Passenger)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);
        }

        public async Task<List<Booking>> GetPassengerBookingsAsync(int passengerId)
        {
            return await _db.Bookings
                .Include(b => b.Schedule!).ThenInclude(s => s.Bus!).ThenInclude(b => b.Route)
                .Include(b => b.BusSeat)
                .Include(b => b.BookingSeats!).ThenInclude(bs => bs.BusSeat)
                .Where(b => b.PassengerID == passengerId)
                .OrderByDescending(b => b.ReservedAt)
                .ToListAsync();
        }

       
        public async Task<bool> ConfirmBookingAsync(int bookingId, int adminId)
        {
            var booking = await _db.Bookings
                .Include(b => b.BusSeat)
                .Include(b => b.BookingSeats!).ThenInclude(bs => bs.BusSeat)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null || booking.Status == "Confirmed")
                return false;

            booking.Status = "Confirmed";
            booking.ConfirmedAt = DateTime.Now;
            booking.TicketNumber = $"TKT-{DateTime.Now:yyyy}-{bookingId:D6}";

            // Mark all seats as Booked
            if (booking.BookingSeats != null && booking.BookingSeats.Any())
            {
                foreach (var bs in booking.BookingSeats)
                {
                    if (bs.BusSeat != null) bs.BusSeat.Status = "Booked";
                }
            }
            // Fallback for legacy single-seat bookings
            else if (booking.BusSeat != null)
            {
                booking.BusSeat.Status = "Booked";
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectBookingAsync(int bookingId, int adminId, string remarks)
        {
            var booking = await _db.Bookings
                .Include(b => b.BusSeat)
                .Include(b => b.BookingSeats!).ThenInclude(bs => bs.BusSeat)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);

            if (booking == null) return false;

            booking.Status = "Rejected";

            // Release all seats
            if (booking.BookingSeats != null && booking.BookingSeats.Any())
            {
                foreach (var bs in booking.BookingSeats)
                {
                    if (bs.BusSeat != null) bs.BusSeat.Status = "Available";
                }
            }
            else if (booking.BusSeat != null)
            {
                booking.BusSeat.Status = "Available";
            }

            await _db.SaveChangesAsync();
            return true;
        }

        
        public async Task ExpireStaleReservationsAsync()
        {
            var now = DateTime.Now;

            // 1) Expire stale reservations
            var stale = await _db.Bookings
                .Include(b => b.BusSeat)
                .Include(b => b.BookingSeats!).ThenInclude(bs => bs.BusSeat)
                .Where(b => b.Status == "Reserved" && b.ExpiresAt < now)
                .ToListAsync();

            foreach (var b in stale)
            {
                b.Status = "Expired";

                if (b.BookingSeats != null && b.BookingSeats.Any())
                {
                    foreach (var bs in b.BookingSeats)
                    {
                        if (bs.BusSeat != null) bs.BusSeat.Status = "Available";
                    }
                }
                else if (b.BusSeat != null)
                {
                    b.BusSeat.Status = "Available";
                }
            }

            // 2) Auto-complete departed schedules
            var activeSchedules = await _db.Schedules
                .Where(s => s.Status == "Active")
                .ToListAsync();

            foreach (var s in activeSchedules)
            {
                var departureDateTime = s.TravelDate.Date + s.DepartureTime;
                if (departureDateTime <= now)
                {
                    s.Status = "Completed";
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}