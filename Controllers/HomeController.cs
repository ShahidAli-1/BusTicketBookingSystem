using BusTicketBookingSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Redirect logged-in users to their dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Passenger"))
                    return RedirectToAction("Dashboard", "Passenger");
            }

            // Show stats on public home page
            ViewBag.TotalBuses = await _db.Buses.CountAsync(b => b.IsActive);
            ViewBag.TotalRoutes = await _db.Routes.CountAsync(r => r.IsActive);
            ViewBag.TotalSchedules = await _db.Schedules.CountAsync(s => s.Status == "Active");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}