using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _bookingService;

        public AdminController(ApplicationDbContext db, IBookingService bs)
        {
            _db = db;
            _bookingService = bs;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            
            await _bookingService.ExpireStaleReservationsAsync();

            ViewBag.TotalBuses = await _db.Buses.CountAsync(b => b.IsActive);
            ViewBag.TotalRoutes = await _db.Routes.CountAsync(r => r.IsActive);
            ViewBag.TotalPassengers = await _db.Passengers.CountAsync(p => p.IsActive);
            ViewBag.TotalSchedules = await _db.Schedules.CountAsync(s => s.Status == "Active");
            ViewBag.CompletedSchedules = await _db.Schedules.CountAsync(s => s.Status == "Completed");
            ViewBag.PendingRequests = await _db.PaymentRequests.CountAsync(p => p.Status == "Pending");
            ViewBag.ConfirmedBookings = await _db.Bookings.CountAsync(b => b.Status == "Confirmed");
            ViewBag.TotalRevenue = await _db.Bookings
                .Where(b => b.Status == "Confirmed")
                .SumAsync(b => (decimal?)b.Fare) ?? 0m;

            var recentBookings = await _db.Bookings
                .Include(b => b.Passenger)
                .Include(b => b.Schedule!).ThenInclude(s => s.Bus!).ThenInclude(b => b.Route)
                .OrderByDescending(b => b.ReservedAt)
                .Take(10)
                .ToListAsync();

            return View(recentBookings);
        }

        // GET: /Admin/Passengers
        public async Task<IActionResult> Passengers(
            string? search,
            string? gender,
            string? status)
        {
            var query = _db.Passengers.AsQueryable();

            // Search by name / email / contact
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.FullName.Contains(search) ||
                    p.Email.Contains(search) ||
                    p.ContactNo.Contains(search));
            }

            // Filter by gender
            if (!string.IsNullOrWhiteSpace(gender))
            {
                query = query.Where(p => p.Gender == gender);
            }

            // Filter by status
            if (status == "Active")
                query = query.Where(p => p.IsActive);
            else if (status == "Inactive")
                query = query.Where(p => !p.IsActive);

            var passengers = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Preserve filter values
            ViewBag.Search = search;
            ViewBag.Gender = gender;
            ViewBag.Status = status;

            return View(passengers);
        }

        // POST: /Admin/TogglePassenger/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePassenger(int id)
        {
            var passenger = await _db.Passengers.FindAsync(id);
            if (passenger == null) return NotFound();

            passenger.IsActive = !passenger.IsActive;
            await _db.SaveChangesAsync();

            TempData["Message"] = $"Passenger {(passenger.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Passengers));
        }

        // GET: /Admin/Reports
        public async Task<IActionResult> Reports()
        {
            await _bookingService.ExpireStaleReservationsAsync();

            ViewBag.TotalBookings = await _db.Bookings.CountAsync();
            ViewBag.ConfirmedBookings = await _db.Bookings.CountAsync(b => b.Status == "Confirmed");
            ViewBag.CancelledBookings = await _db.Bookings.CountAsync(b => b.Status == "Cancelled");
            ViewBag.RejectedBookings = await _db.Bookings.CountAsync(b => b.Status == "Rejected");
            ViewBag.TotalRevenue = await _db.Bookings
                .Where(b => b.Status == "Confirmed")
                .SumAsync(b => (decimal?)b.Fare) ?? 0m;

            ViewBag.ActiveSchedules = await _db.Schedules.CountAsync(s => s.Status == "Active");
            ViewBag.CompletedSchedules = await _db.Schedules.CountAsync(s => s.Status == "Completed");

            var bookingsByStatus = await _db.Bookings
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.BookingsByStatus = bookingsByStatus;

            return View();
        }

        // POST: /Admin/RunMaintenance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunMaintenance()
        {
            await _bookingService.ExpireStaleReservationsAsync();
            TempData["Message"] = "Maintenance run: stale reservations expired, departed schedules completed.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}