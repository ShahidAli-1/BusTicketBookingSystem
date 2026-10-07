using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BusController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BusController(ApplicationDbContext db) => _db = db;

        // GET: /Bus
        public async Task<IActionResult> Index(
            string? search,
            string? type,
            string? status,
            int? routeId)
        {
            var query = _db.Buses
                .Include(b => b.Route)
                .AsQueryable();

            // Search by bus number or name
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.BusNumber.Contains(search) ||
                    b.BusName.Contains(search));
            }

            // Filter by bus type
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(b => b.BusType == type);
            }

            // Filter by status
            if (status == "Active")
                query = query.Where(b => b.IsActive);
            else if (status == "Inactive")
                query = query.Where(b => !b.IsActive);

            // Filter by route
            if (routeId.HasValue && routeId.Value > 0)
            {
                query = query.Where(b => b.RouteID == routeId.Value);
            }

            var buses = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Populate filter values
            ViewBag.Search = search;
            ViewBag.Type = type;
            ViewBag.Status = status;
            ViewBag.RouteId = routeId;
            ViewBag.Routes = new SelectList(
                await _db.Routes.Where(r => r.IsActive).ToListAsync(),
                "RouteID", "RouteName", routeId);

            return View(buses);
        }

        // GET: /Bus/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadRoutesDropdownAsync();
            return View();
        }

        // POST: /Bus/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bus bus)
        {
            if (_db.Buses.Any(b => b.BusNumber == bus.BusNumber))
            {
                ModelState.AddModelError("BusNumber", "Bus number must be unique.");
            }

            if (!ModelState.IsValid)
            {
                await LoadRoutesDropdownAsync(bus.RouteID);
                return View(bus);
            }

            bus.CreatedAt = DateTime.Now;
            _db.Buses.Add(bus);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Bus registered successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Bus/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var bus = await _db.Buses.FindAsync(id);
            if (bus == null) return NotFound();

            await LoadRoutesDropdownAsync(bus.RouteID);
            return View(bus);
        }

        // POST: /Bus/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bus bus)
        {
            if (id != bus.BusID) return BadRequest();

            if (_db.Buses.Any(b => b.BusNumber == bus.BusNumber && b.BusID != bus.BusID))
            {
                ModelState.AddModelError("BusNumber", "Bus number must be unique.");
            }

            if (!ModelState.IsValid)
            {
                await LoadRoutesDropdownAsync(bus.RouteID);
                return View(bus);
            }

            var existing = await _db.Buses.FindAsync(id);
            if (existing == null) return NotFound();

            existing.BusNumber = bus.BusNumber;
            existing.BusName = bus.BusName;
            existing.BusType = bus.BusType;
            existing.TotalSeats = bus.TotalSeats;
            existing.RouteID = bus.RouteID;
            existing.IsActive = bus.IsActive;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Bus updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Bus/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var bus = await _db.Buses
                .Include(b => b.Route)
                .Include(b => b.Schedules)
                .FirstOrDefaultAsync(b => b.BusID == id);
            if (bus == null) return NotFound();
            return View(bus);
        }

        // POST: /Bus/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var bus = await _db.Buses.FindAsync(id);
            if (bus == null) return NotFound();

            bus.IsActive = !bus.IsActive;
            await _db.SaveChangesAsync();

            TempData["Message"] = $"Bus {(bus.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRoutesDropdownAsync(int? selectedId = null)
        {
            var routes = await _db.Routes
                .Where(r => r.IsActive)
                .ToListAsync();
            ViewBag.Routes = new SelectList(routes, "RouteID", "RouteName", selectedId);
        }
    }
}