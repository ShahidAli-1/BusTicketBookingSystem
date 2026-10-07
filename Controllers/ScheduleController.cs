using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Models;
using BusTicketBookingSystem.Services;
using BusTicketBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISeatService _seatService;
        private readonly IBookingService _bookingService;

        public ScheduleController(
            ApplicationDbContext db,
            ISeatService seatService,
            IBookingService bookingService)
        {
            _db = db;
            _seatService = seatService;
            _bookingService = bookingService;
        }

        // GET: /Schedule
        public async Task<IActionResult> Index(
            string? search,
            int? busId,
            int? routeId,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            await _bookingService.ExpireStaleReservationsAsync();

            // ⭐ DEFAULT: show only Active if no status filter is provided
            // But respect "All" (empty string) when explicitly chosen
            var effectiveStatus = string.IsNullOrWhiteSpace(status) ? "Active" : status;

            var query = _db.Schedules
                .Include(s => s.Bus).ThenInclude(b => b!.Route)
                .AsQueryable();

            // Search by bus number / name / route name
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.Bus!.BusNumber.Contains(search) ||
                    s.Bus!.BusName.Contains(search) ||
                    s.Bus!.Route!.RouteName.Contains(search));
            }

            if (busId.HasValue && busId.Value > 0)
                query = query.Where(s => s.BusID == busId.Value);

            if (routeId.HasValue && routeId.Value > 0)
                query = query.Where(s => s.Bus!.RouteID == routeId.Value);

            // ⭐ Handle "All" explicitly → no status filter
            if (effectiveStatus != "All" && !string.IsNullOrWhiteSpace(effectiveStatus))
            {
                query = query.Where(s => s.Status == effectiveStatus);
            }

            if (fromDate.HasValue)
                query = query.Where(s => s.TravelDate.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(s => s.TravelDate.Date <= toDate.Value.Date);

            var schedules = await query
                .OrderByDescending(s => s.TravelDate)
                .ThenBy(s => s.DepartureTime)
                .ToListAsync();

            // ⭐ Counts for tab badges
            ViewBag.ActiveCount = await _db.Schedules.CountAsync(s => s.Status == "Active");
            ViewBag.CompletedCount = await _db.Schedules.CountAsync(s => s.Status == "Completed");
            ViewBag.CancelledCount = await _db.Schedules.CountAsync(s => s.Status == "Cancelled");
            ViewBag.AllCount = await _db.Schedules.CountAsync();

            // Dropdowns
            ViewBag.Buses = new SelectList(
                await _db.Buses.Where(b => b.IsActive)
                    .Select(b => new { b.BusID, Display = b.BusNumber + " - " + b.BusName })
                    .ToListAsync(),
                "BusID", "Display", busId);

            ViewBag.Routes = new SelectList(
                await _db.Routes.Where(r => r.IsActive).ToListAsync(),
                "RouteID", "RouteName", routeId);

            // Preserve
            ViewBag.Search = search;
            ViewBag.BusId = busId;
            ViewBag.RouteId = routeId;
            ViewBag.Status = effectiveStatus;    // ⭐ use effective
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Now = DateTime.Now;

            return View(schedules);
        }

        // GET: /Schedule/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadBusesDropdownAsync();
            return View();
        }

        // POST: /Schedule/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Schedule schedule)
        {
            if (schedule.ArrivalTime <= schedule.DepartureTime)
            {
                ModelState.AddModelError("ArrivalTime", "Arrival time must be after departure time.");
            }

            if (schedule.TravelDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("TravelDate", "Travel date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await LoadBusesDropdownAsync(schedule.BusID);
                return View(schedule);
            }

            _db.Schedules.Add(schedule);
            await _db.SaveChangesAsync();

            // Auto-generate seats for this schedule
            var bus = await _db.Buses.FindAsync(schedule.BusID);
            if (bus != null)
            {
                await _seatService.InitializeSeatsAsync(schedule.ScheduleID, bus.TotalSeats);
            }

            TempData["Message"] = $"Schedule created with {bus?.TotalSeats} seats initialized.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Schedule/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _db.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            await LoadBusesDropdownAsync(schedule.BusID);
            return View(schedule);
        }

        // POST: /Schedule/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Schedule schedule)
        {
            if (id != schedule.ScheduleID) return BadRequest();

            if (schedule.ArrivalTime <= schedule.DepartureTime)
                ModelState.AddModelError("ArrivalTime", "Arrival time must be after departure time.");

            if (!ModelState.IsValid)
            {
                await LoadBusesDropdownAsync(schedule.BusID);
                return View(schedule);
            }

            var existing = await _db.Schedules.FindAsync(id);
            if (existing == null) return NotFound();

            existing.BusID = schedule.BusID;
            existing.TravelDate = schedule.TravelDate;
            existing.DepartureTime = schedule.DepartureTime;
            existing.ArrivalTime = schedule.ArrivalTime;
            existing.Fare = schedule.Fare;
            existing.Status = schedule.Status;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Schedule updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Schedule/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var schedule = await _db.Schedules
                .Include(s => s.Bus).ThenInclude(b => b!.Route)
                .Include(s => s.Seats)
                .FirstOrDefaultAsync(s => s.ScheduleID == id);
            if (schedule == null) return NotFound();

            ViewBag.BookedSeats = schedule.Seats?.Count(s => s.Status == "Booked") ?? 0;
            ViewBag.ReservedSeats = schedule.Seats?.Count(s => s.Status == "Reserved") ?? 0;
            ViewBag.AvailableSeats = schedule.Seats?.Count(s => s.Status == "Available") ?? 0;

            // ⭐ Departure flag
            var departureDateTime = schedule.TravelDate.Date + schedule.DepartureTime;
            ViewBag.IsDeparted = departureDateTime <= DateTime.Now;
            ViewBag.DepartureDateTime = departureDateTime;

            return View(schedule);
        }

        // POST: /Schedule/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var schedule = await _db.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            schedule.Status = "Cancelled";
            await _db.SaveChangesAsync();

            TempData["Message"] = "Schedule cancelled.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadBusesDropdownAsync(int? selectedId = null)
        {
            var buses = await _db.Buses
                .Include(b => b.Route)
                .Where(b => b.IsActive)
                .ToListAsync();

            ViewBag.Buses = new SelectList(
                buses.Select(b => new
                {
                    b.BusID,
                    Display = $"{b.BusNumber} - {b.BusName} ({b.Route?.RouteName})"
                }),
                "BusID", "Display", selectedId);
        }

        // GET: /Schedule/Reschedule/5
        [HttpGet]
        public async Task<IActionResult> Reschedule(int id)
        {
            var source = await _db.Schedules
                .Include(s => s.Bus).ThenInclude(b => b!.Route)
                .FirstOrDefaultAsync(s => s.ScheduleID == id);

            if (source == null) return NotFound();

            if (source.Status != "Completed" && source.Status != "Cancelled")
            {
                TempData["Error"] = "Only Completed or Cancelled schedules can be rescheduled.";
                return RedirectToAction(nameof(Index));
            }

            // ⭐ Detect if this schedule has any active bookings
            var hasBookings = await _db.Bookings
                .AnyAsync(b => b.ScheduleID == source.ScheduleID &&
                              (b.Status == "Confirmed" || b.Status == "PendingApproval"));

            ViewBag.HasBookings = hasBookings;

            var vm = new RescheduleViewModel
            {
                SourceScheduleID = source.ScheduleID,
                OldBusNumber = source.Bus?.BusNumber ?? "",
                OldBusName = source.Bus?.BusName ?? "",
                OldRouteName = source.Bus?.Route?.RouteName ?? "",
                OldTravelDate = source.TravelDate,
                OldDepartureTime = source.DepartureTime,
                OldArrivalTime = source.ArrivalTime,
                OldFare = source.Fare,

                NewBusID = source.BusID,
                NewTravelDate = DateTime.Today.AddDays(1),
                NewDepartureTime = source.DepartureTime,
                NewArrivalTime = source.ArrivalTime,
                NewFare = source.Fare,

                BusOptions = await GetBusSelectListAsync(source.BusID)
            };

            return View(vm);
        }


        // POST: /Schedule/Reschedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(RescheduleViewModel model)
        {
            // ---- Validate times ----
            if (model.NewArrivalTime <= model.NewDepartureTime)
                ModelState.AddModelError("NewArrivalTime", "Arrival time must be after departure time.");

            if (model.NewTravelDate.Date < DateTime.Today)
                ModelState.AddModelError("NewTravelDate", "Travel date cannot be in the past.");

            // ---- Validate bus ----
            var newBus = await _db.Buses
                .Include(b => b.Route)
                .FirstOrDefaultAsync(b => b.BusID == model.NewBusID);

            if (newBus == null)
            {
                ModelState.AddModelError("NewBusID", "Selected bus not found.");
            }
            else if (newBus.Route == null || !newBus.Route.IsActive)
            {
                ModelState.AddModelError("NewBusID",
                    "The selected bus does not have an active route assigned.");
            }

            // ---- Load source schedule ----
            var source = await _db.Schedules
                .Include(s => s.Bus).ThenInclude(b => b!.Route)
                .FirstOrDefaultAsync(s => s.ScheduleID == model.SourceScheduleID);

            if (source == null) return NotFound();

            if (source.Status != "Completed" && source.Status != "Cancelled")
            {
                TempData["Error"] = "Only Completed or Cancelled schedules can be rescheduled.";
                return RedirectToAction(nameof(Index));
            }

            // ---- Detect bookings on original ----
            var hasBookings = await _db.Bookings
                .AnyAsync(b => b.ScheduleID == source.ScheduleID &&
                              (b.Status == "Confirmed" || b.Status == "PendingApproval"));

            ViewBag.HasBookings = hasBookings;

            // ---- Overlap check (against new bus's active schedules) ----
            if (newBus != null)
            {
                var newDeparture = model.NewTravelDate.Date + model.NewDepartureTime;
                var newArrival = model.NewTravelDate.Date + model.NewArrivalTime;

                var conflicting = await _db.Schedules
                    .Where(s => s.BusID == model.NewBusID
                             && s.Status == "Active"
                             && s.ScheduleID != source.ScheduleID)   // exclude self (for replace case)
                    .ToListAsync();

                foreach (var s in conflicting)
                {
                    var dep = s.TravelDate.Date + s.DepartureTime;
                    var arr = s.TravelDate.Date + s.ArrivalTime;

                    if (newDeparture < arr && newArrival > dep)
                    {
                        ModelState.AddModelError("",
                            $"Bus {newBus.BusNumber} already has an active schedule " +
                            $"from {dep:dd MMM HH:mm} to {arr:dd MMM HH:mm}. " +
                            $"Please choose a different time or bus.");
                        break;
                    }
                }
            }

            // ---- Invalid → reload view ----
            if (!ModelState.IsValid)
            {
                model.BusOptions = await GetBusSelectListAsync(model.NewBusID);
                model.OldBusNumber = source.Bus?.BusNumber ?? "";
                model.OldBusName = source.Bus?.BusName ?? "";
                model.OldRouteName = source.Bus?.Route?.RouteName ?? "";
                model.OldTravelDate = source.TravelDate;
                model.OldDepartureTime = source.DepartureTime;
                model.OldArrivalTime = source.ArrivalTime;
                model.OldFare = source.Fare;
                return View(model);
            }

            // ⭐ HYBRID LOGIC

            if (!hasBookings)
            {
                // ✅ CASE A: No bookings → REPLACE IN PLACE
                var busChanged = source.BusID != model.NewBusID;

                // Update schedule fields
                source.BusID = model.NewBusID;
                source.TravelDate = model.NewTravelDate.Date;
                source.DepartureTime = model.NewDepartureTime;
                source.ArrivalTime = model.NewArrivalTime;
                source.Fare = model.NewFare;
                source.Status = "Active";

                await _db.SaveChangesAsync();

                // If bus changed → reinitialize seats
                if (busChanged)
                {
                    var oldSeats = await _db.BusSeats
                        .Where(s => s.ScheduleID == source.ScheduleID)
                        .ToListAsync();

                    // Safety: don't delete if any seat has a booking (shouldn't happen if no bookings)
                    if (oldSeats.Any(s => s.Status != "Available"))
                    {
                        TempData["Error"] =
                            "Cannot change bus: some seats still have bookings. " +
                            "Please create a new schedule instead.";
                        return RedirectToAction(nameof(Index));
                    }

                    _db.BusSeats.RemoveRange(oldSeats);
                    await _db.SaveChangesAsync();

                    var totalSeats = newBus?.TotalSeats ?? 0;
                    if (totalSeats > 0)
                    {
                        await _seatService.InitializeSeatsAsync(source.ScheduleID, totalSeats);
                    }
                }

                TempData["Message"] =
                    $"✅ Schedule #{source.ScheduleID} rescheduled in place to " +
                    $"{source.TravelDate:dd MMM yyyy} at {source.DepartureTime}. " +
                    $"Bus: {newBus?.BusNumber}.";
            }
            else
            {
                // ⚠️ CASE B: Has bookings → KEEP ORIGINAL + CREATE NEW
                var newSchedule = new Schedule
                {
                    BusID = model.NewBusID,
                    TravelDate = model.NewTravelDate.Date,
                    DepartureTime = model.NewDepartureTime,
                    ArrivalTime = model.NewArrivalTime,
                    Fare = model.NewFare,
                    Status = "Active"
                };

                _db.Schedules.Add(newSchedule);
                await _db.SaveChangesAsync();

                // Initialize seats for the new schedule
                var totalSeats = newBus?.TotalSeats ?? 0;
                if (totalSeats > 0)
                {
                    await _seatService.InitializeSeatsAsync(newSchedule.ScheduleID, totalSeats);
                }

                TempData["Message"] =
                    $"✅ Original had bookings → preserved as history. " +
                    $"New Schedule #{newSchedule.ScheduleID} created for " +
                    $"{newSchedule.TravelDate:dd MMM yyyy} at {newSchedule.DepartureTime} " +
                    $"(Bus: {newBus?.BusNumber}).";
            }

            return RedirectToAction(nameof(Index));
        }


        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetBusSelectListAsync(int? selectedId = null)
        {
            var buses = await _db.Buses
                .Include(b => b.Route)
                .Where(b => b.IsActive)
                .OrderBy(b => b.BusNumber)
                .ToListAsync();

            return buses.Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = b.BusID.ToString(),
                Text = $"{b.BusNumber} — {b.BusName} ({b.Route?.RouteName ?? "No route"}) — {b.TotalSeats} seats",
                Selected = selectedId.HasValue && b.BusID == selectedId.Value
            }).ToList();
        }
    }
}