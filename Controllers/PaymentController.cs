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
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPaymentService _paymentService;
        private readonly IBookingService _bookingService;

        public PaymentController(ApplicationDbContext db, IPaymentService ps, IBookingService bs)
        {
            _db = db;
            _paymentService = ps;
            _bookingService = bs;
        }

        private int CurrentPassengerId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: /Payment/Request/5
        [HttpGet]
        [Authorize(Roles = "Passenger")]
        public async Task<IActionResult> Request(int bookingId)
        {
            var booking = await _bookingService.GetBookingAsync(bookingId);
            if (booking == null) return NotFound();
            if (booking.PassengerID != CurrentPassengerId) return Forbid();

            if (booking.Status != "Reserved")
            {
                TempData["Error"] = "Only reserved bookings can submit payment.";
                return RedirectToAction("Details", "Booking", new { id = bookingId });
            }

            var vm = new PaymentRequestViewModel
            {
                BookingId = booking.BookingID,
                BookingNumber = booking.BookingNumber,
                Amount = booking.Fare
            };

            return View(vm);
        }

        // POST: /Payment/Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Passenger")]
        public async Task<IActionResult> Request(PaymentRequestViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var booking = await _bookingService.GetBookingAsync(vm.BookingId);
            if (booking == null) return NotFound();
            if (booking.PassengerID != CurrentPassengerId) return Forbid();

            try
            {
                await _paymentService.SubmitRequestAsync(
                    vm.BookingId, vm.Amount, vm.PaymentMethod, vm.PaymentReference);

                TempData["Message"] = "Payment request submitted. Awaiting admin approval.";
                return RedirectToAction("Details", "Booking", new { id = vm.BookingId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
        }

        // GET: /Payment/Pending (Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Pending()
        {
            var requests = await _paymentService.GetPendingRequestsAsync();
            return View(requests);
        }

        // GET: /Payment/Review/5 (Admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Review(int id)
        {
            var request = await _paymentService.GetByIdAsync(id);
            if (request == null) return NotFound();
            return View(request);
        }

        // POST: /Payment/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, string? remarks)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var ok = await _paymentService.ApproveAsync(id, adminId, remarks);
            if (!ok)
            {
                TempData["Error"] = "Failed to approve. Request may have been already processed.";
            }
            else
            {
                TempData["Message"] = "Payment approved. Ticket confirmed.";
            }

            return RedirectToAction(nameof(Pending));
        }

        // POST: /Payment/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id, string remarks)
        {
            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["Error"] = "Remarks are required for rejection.";
                return RedirectToAction(nameof(Review), new { id });
            }

            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _paymentService.RejectAsync(id, adminId, remarks);

            TempData[ok ? "Message" : "Error"] =
                ok ? "Payment rejected. Seat released." : "Failed to reject request.";

            return RedirectToAction(nameof(Pending));
        }

        // GET: /Payment/MyRequests (Passenger)
        [HttpGet]
        [Authorize(Roles = "Passenger")]
        public async Task<IActionResult> MyRequests()
        {
            var requests = await _db.PaymentRequests
                .Include(p => p.Booking!).ThenInclude(b => b.Schedule!).ThenInclude(s => s.Bus!).ThenInclude(b => b.Route)
                .Where(p => p.Booking!.PassengerID == CurrentPassengerId)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();

            return View(requests);
        }
    }
}