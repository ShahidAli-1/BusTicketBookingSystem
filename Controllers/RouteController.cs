using BusTicketBookingSystem.Data;
using BusTicketBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RouteController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RouteController(ApplicationDbContext db) => _db = db;

        // GET: /Route
        public async Task<IActionResult> Index(
            string? search,
            string? origin,
            string? destination,
            string? status)
        {
            var query = _db.Routes
                .Include(r => r.Buses)
                .AsQueryable();

            // Search by name / origin / destination
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.RouteName.Contains(search) ||
                    r.Origin.Contains(search) ||
                    r.Destination.Contains(search));
            }

            // Filter by origin
            if (!string.IsNullOrWhiteSpace(origin))
            {
                query = query.Where(r => r.Origin.Contains(origin));
            }

            // Filter by destination
            if (!string.IsNullOrWhiteSpace(destination))
            {
                query = query.Where(r => r.Destination.Contains(destination));
            }

            // Filter by status
            if (status == "Active")
                query = query.Where(r => r.IsActive);
            else if (status == "Inactive")
                query = query.Where(r => !r.IsActive);

            var routes = await query
                .OrderBy(r => r.RouteName)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Origin = origin;
            ViewBag.Destination = destination;
            ViewBag.Status = status;

            return View(routes);
        }

        // GET: /Route/Create
        [HttpGet]
        public IActionResult Create() => View();

        // POST: /Route/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.Route route)
        {
            if (!ModelState.IsValid) return View(route);

            _db.Routes.Add(route);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Route created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Route/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var route = await _db.Routes.FindAsync(id);
            if (route == null) return NotFound();
            return View(route);
        }

        // POST: /Route/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.Route route)
        {
            if (id != route.RouteID) return BadRequest();
            if (!ModelState.IsValid) return View(route);

            var existing = await _db.Routes.FindAsync(id);
            if (existing == null) return NotFound();

            existing.RouteName = route.RouteName;
            existing.Origin = route.Origin;
            existing.Destination = route.Destination;
            existing.DistanceKm = route.DistanceKm;
            existing.EstimatedDuration = route.EstimatedDuration;
            existing.IsActive = route.IsActive;

            await _db.SaveChangesAsync();
            TempData["Message"] = "Route updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Route/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var route = await _db.Routes
                .Include(r => r.Buses)
                .FirstOrDefaultAsync(r => r.RouteID == id);
            if (route == null) return NotFound();
            return View(route);
        }

        // POST: /Route/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var route = await _db.Routes.FindAsync(id);
            if (route == null) return NotFound();

            route.IsActive = !route.IsActive;
            await _db.SaveChangesAsync();

            TempData["Message"] = $"Route {(route.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}