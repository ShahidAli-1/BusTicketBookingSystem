using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Models;
using BusTicketBookingSystem.Services;
using BusTicketBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace BusTicketBookingSystem.Controllers
{
    [Authorize(Roles = "Passenger")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _bookingService;
        private readonly ISeatService _seatService;

        public BookingController(
            ApplicationDbContext db,
            IBookingService bs,
            ISeatService ss)
        {
            _db = db;
            _bookingService = bs;
            _seatService = ss;
        }

        private int CurrentPassengerId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: /Booking/Search
        [HttpGet]
        public async Task<IActionResult> Search(
            string? origin,
            string? destination,
            DateTime? date)
        {
            // Expire stale reservations
            await _bookingService.ExpireStaleReservationsAsync();

            var now = DateTime.Now;
            var today = DateTime.Today;

            var query = _db.Schedules
                .Include(s => s.Bus).ThenInclude(b => b!.Route)
                .Where(s => s.Status == "Active")
                .Where(s =>
                    s.TravelDate.Date > today ||
                    (s.TravelDate.Date == today && s.DepartureTime > now.TimeOfDay));

            if (!string.IsNullOrWhiteSpace(origin))
                query = query.Where(s => s.Bus!.Route!.Origin.Contains(origin));

            if (!string.IsNullOrWhiteSpace(destination))
                query = query.Where(s => s.Bus!.Route!.Destination.Contains(destination));

            if (date.HasValue)
                query = query.Where(s => s.TravelDate.Date == date.Value.Date);

            var schedules = await query
                .OrderBy(s => s.TravelDate)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();

            ViewBag.Origin = origin;
            ViewBag.Destination = destination;
            ViewBag.Date = date?.ToString("yyyy-MM-dd");

            return View(schedules);
        }

        // GET: /Booking/Seats/5
        [HttpGet]
        public async Task<IActionResult> Seats(int id)
        {
            await _bookingService.ExpireStaleReservationsAsync();

            var schedule = await _db.Schedules
                .Include(s => s.Bus).ThenInclude(b => b!.Route)
                .FirstOrDefaultAsync(s => s.ScheduleID == id);

            if (schedule == null) return NotFound();

            var departureDateTime = schedule.TravelDate.Date + schedule.DepartureTime;
            if (departureDateTime <= DateTime.Now)
            {
                TempData["Error"] = "This schedule has already departed and cannot be booked.";
                return RedirectToAction(nameof(Search));
            }

            // Real seats from DB
            var dbSeats = await _seatService.GetSeatsForScheduleAsync(id);
            var dbSeatMap = dbSeats.ToDictionary(s => s.SeatNumber);

            // ⭐ Fixed visual grid
            const int VISUAL_SEATS = 45;
            var busCapacity = schedule.Bus?.TotalSeats ?? 0;

            var allSeats = new List<BusSeat>();

            for (int i = 1; i <= VISUAL_SEATS; i++)
            {
                if (i <= busCapacity && dbSeatMap.TryGetValue(i, out var realSeat))
                {
                    // Real seat — status from DB
                    allSeats.Add(realSeat);
                }
                else
                {
                    // Disabled placeholder (beyond bus capacity)
                    allSeats.Add(new BusSeat
                    {
                        BusSeatID = 0,
                        ScheduleID = id,
                        SeatNumber = i,
                        Status = "Disabled"
                    });
                }
            }

            var vm = new SeatSelectionViewModel
            {
                ScheduleID = schedule.ScheduleID,
                Schedule = schedule,
                Seats = allSeats,
                Origin = schedule.Bus?.Route?.Origin ?? "",
                Destination = schedule.Bus?.Route?.Destination ?? "",
                BusName = schedule.Bus?.BusName ?? "",
                BusNumber = schedule.Bus?.BusNumber ?? "",
                TravelDate = schedule.TravelDate,
                DepartureTime = schedule.DepartureTime,
                Fare = schedule.Fare,
                MaxSeatsPerBooking = 5,
                TotalSeatsInBus = busCapacity,
                VisualSeatCount = VISUAL_SEATS
            };

            return View(vm);
        }
        

        // POST: /Booking/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int scheduleId, string seatNumbers)
        {
            if (string.IsNullOrWhiteSpace(seatNumbers))
            {
                TempData["Error"] = "Please select at least one seat.";
                return RedirectToAction(nameof(Seats), new { id = scheduleId });
            }

            // Parse comma-separated seat numbers: "3,7,12"
            var seatList = seatNumbers
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                .Where(n => n > 0)
                .Distinct()
                .ToList();

            if (seatList.Count == 0)
            {
                TempData["Error"] = "Please select at least one valid seat.";
                return RedirectToAction(nameof(Seats), new { id = scheduleId });
            }

            if (seatList.Count > 5)
            {
                TempData["Error"] = "You can book a maximum of 5 seats at a time.";
                return RedirectToAction(nameof(Seats), new { id = scheduleId });
            }

            try
            {
                // Block past departures
                var schedule = await _db.Schedules.FindAsync(scheduleId);
                if (schedule == null)
                {
                    TempData["Error"] = "Schedule not found.";
                    return RedirectToAction(nameof(Search));
                }

                var departureDateTime = schedule.TravelDate.Date + schedule.DepartureTime;
                if (departureDateTime <= DateTime.Now)
                {
                    TempData["Error"] = "This schedule has already departed.";
                    return RedirectToAction(nameof(Search));
                }

                // ⭐ Create ONE booking covering all seats
                var booking = await _bookingService.CreateMultiSeatBookingAsync(
                    CurrentPassengerId, scheduleId, seatList);

                return RedirectToAction("Request", "Payment", new { bookingId = booking.BookingID });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Seats), new { id = scheduleId });
            }
        }

        // GET: /Booking/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _bookingService.GetBookingAsync(id);
            if (booking == null) return NotFound();
            if (booking.PassengerID != CurrentPassengerId) return Forbid();
            return View(booking);
        }

        // POST: /Booking/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _bookingService.GetBookingAsync(id);
            if (booking == null) return NotFound();
            if (booking.PassengerID != CurrentPassengerId) return Forbid();

            if (booking.Status == "Reserved" || booking.Status == "PendingApproval")
            {
                await _bookingService.RejectBookingAsync(id, 0, "Cancelled by passenger");
                TempData["Message"] = "Booking cancelled. Seat released.";
            }
            else
            {
                TempData["Error"] = "Only Reserved or Pending bookings can be cancelled.";
            }

            return RedirectToAction("BookingHistory", "Passenger");
        }
    }
}