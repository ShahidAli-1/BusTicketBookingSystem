using BusTicketBookingSystem.Models;
using BusTicketBookingSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var pwd = services.GetRequiredService<IPasswordService>();

            // Admin
            if (!await db.Admins.AnyAsync())
            {
                db.Admins.Add(new Admin
                {
                    FullName = "System Admin",
                    Email = "admin@bus.com",
                    PasswordHash = pwd.HashPassword("Admin@123")
                });
                await db.SaveChangesAsync();
            }

            // Routes
            if (!await db.Routes.AnyAsync())
            {
                db.Routes.AddRange(
                    new Models.Route { RouteName = "Multan-Lahore", Origin = "Multan", Destination = "Lahore", DistanceKm = 350, EstimatedDuration = "5 hours" },
                    new Models.Route { RouteName = "Multan-Islamabad", Origin = "Multan", Destination = "Islamabad", DistanceKm = 550, EstimatedDuration = "8 hours" },
                    new Models.Route { RouteName = "Lahore-Karachi", Origin = "Lahore", Destination = "Karachi", DistanceKm = 1200, EstimatedDuration = "18 hours" }
                );
                await db.SaveChangesAsync();
            }

            // Buses
            if (!await db.Buses.AnyAsync())
            {
                var route1 = await db.Routes.FirstAsync(r => r.RouteName == "Multan-Lahore");
                var route2 = await db.Routes.FirstAsync(r => r.RouteName == "Multan-Islamabad");

                db.Buses.AddRange(
                    new Bus { BusNumber = "MLT-101", BusName = "Express 01", BusType = "Executive", TotalSeats = 40, RouteID = route1.RouteID },
                    new Bus { BusNumber = "MLT-102", BusName = "Express 02", BusType = "Business", TotalSeats = 30, RouteID = route2.RouteID }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}