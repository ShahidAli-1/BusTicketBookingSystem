using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BusTicketBookingSystem.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBookingService _bookingService;

        public TicketController(ApplicationDbContext db, IBookingService bs)
        {
            _db = db;
            _bookingService = bs;
        }

        // GET: /Ticket/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _bookingService.GetBookingAsync(id);
            if (booking == null) return NotFound();

            // Admin can view any; passenger can only view own
            if (User.IsInRole("Passenger"))
            {
                var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (booking.PassengerID != passengerId) return Forbid();
            }

            if (booking.Status != "Confirmed")
            {
                TempData["Error"] = "Ticket not yet confirmed.";
                return RedirectToAction("BookingHistory", "Passenger");
            }

            return View(booking);
        }

        // GET: /Ticket/Print/5
        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            var booking = await _bookingService.GetBookingAsync(id);
            if (booking == null) return NotFound();

            if (User.IsInRole("Passenger"))
            {
                var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (booking.PassengerID != passengerId) return Forbid();
            }

            if (booking.Status != "Confirmed")
            {
                TempData["Error"] = "Ticket not yet confirmed.";
                return RedirectToAction("BookingHistory", "Passenger");
            }

            return View(booking);
        }

        // GET: /Ticket/MyTickets (Passenger)
        [HttpGet]
        [Authorize(Roles = "Passenger")]
        public async Task<IActionResult> MyTickets()
        {
            var passengerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var bookings = await _bookingService.GetPassengerBookingsAsync(passengerId);
            return View(bookings.Where(b => b.Status == "Confirmed").ToList());
        }
    }
}