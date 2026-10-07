using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BusTicketBookingSystem.Controllers
{
    [Authorize(Roles = "Passenger")]
    public class PassengerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _bookingService;

        public PassengerController(ApplicationDbContext db, IBookingService bs)
        {
            _db = db;
            _bookingService = bs;
        }

        private int CurrentPassengerId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: /Passenger/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            await _bookingService.ExpireStaleReservationsAsync();

            var bookings = await _bookingService.GetPassengerBookingsAsync(CurrentPassengerId);

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.ConfirmedBookings = bookings.Count(b => b.Status == "Confirmed");
            ViewBag.PendingBookings = bookings.Count(b => b.Status == "PendingApproval" || b.Status == "Reserved");
            ViewBag.TotalSpent = bookings
                .Where(b => b.Status == "Confirmed")
                .Sum(b => b.Fare);

            return View(bookings.Take(5).ToList());
        }

        // GET: /Passenger/BookingHistory
        public async Task<IActionResult> BookingHistory(
            string? search,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            await _bookingService.ExpireStaleReservationsAsync();

            var query = _db.Bookings
                .Include(b => b.Schedule!).ThenInclude(s => s.Bus!).ThenInclude(b => b.Route)
                .Include(b => b.BusSeat)
                .Include(b => b.BookingSeats!).ThenInclude(bs => bs.BusSeat)
                .Where(b => b.PassengerID == CurrentPassengerId)
                .AsQueryable();

            // Search by booking number OR route name
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.BookingNumber.Contains(search) ||
                    (b.Schedule!.Bus!.Route!.Origin + " → " + b.Schedule!.Bus!.Route!.Destination)
                        .Contains(search) ||
                    b.Schedule!.Bus!.Route!.RouteName.Contains(search));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            // Filter by travel date range
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.Schedule!.TravelDate.Date >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(b => b.Schedule!.TravelDate.Date <= toDate.Value.Date);
            }

            var bookings = await query
                .OrderByDescending(b => b.ReservedAt)
                .ToListAsync();

            // Preserve values
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            // Count stats for the top mini-summary
            var allBookings = await _bookingService.GetPassengerBookingsAsync(CurrentPassengerId);
            ViewBag.TotalCount = allBookings.Count;
            ViewBag.ConfirmedCount = allBookings.Count(b => b.Status == "Confirmed");
            ViewBag.PendingCount = allBookings.Count(b => b.Status == "PendingApproval" || b.Status == "Reserved");
            ViewBag.CancelledOrRejectedCount = allBookings.Count(b => b.Status == "Cancelled" || b.Status == "Rejected" || b.Status == "Expired");

            return View(bookings);
        }

        // GET: /Passenger/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var passenger = await _db.Passengers.FindAsync(CurrentPassengerId);
            if (passenger == null) return NotFound();
            return View(passenger);
        }

        // POST: /Passenger/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(string fullName, string contactNo, string gender)
        {
            var passenger = await _db.Passengers.FindAsync(CurrentPassengerId);
            if (passenger == null) return NotFound();

            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 3)
            {
                TempData["Error"] = "Full name must be at least 3 characters.";
                return RedirectToAction(nameof(Profile));
            }

            passenger.FullName = fullName;
            passenger.ContactNo = contactNo;
            passenger.Gender = gender;

            await _db.SaveChangesAsync();

            TempData["Message"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }
    }
}