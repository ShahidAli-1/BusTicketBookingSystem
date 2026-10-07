using BusTicketBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BusTicketBookingSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Models.Route> Routes { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<BusSeat> BusSeats { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<PaymentRequest> PaymentRequests { get; set; }
        public DbSet<BookingSeat> BookingSeats { get; set; }
        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraints
            modelBuilder.Entity<Passenger>().HasIndex(p => p.Email).IsUnique();
            modelBuilder.Entity<Admin>().HasIndex(a => a.Email).IsUnique();
            modelBuilder.Entity<Bus>().HasIndex(b => b.BusNumber).IsUnique();
            modelBuilder.Entity<Booking>().HasIndex(b => b.BookingNumber).IsUnique();

            // Prevent duplicate seat per schedule
            modelBuilder.Entity<BusSeat>()
                .HasIndex(s => new { s.ScheduleID, s.SeatNumber }).IsUnique();

            // Avoid cascade delete of historical bookings
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Schedule)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ScheduleID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.BusSeat)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.BusSeatID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Passenger)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PassengerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookingSeat>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingSeats)
                .HasForeignKey(bs => bs.BookingID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingSeat>()
                .HasOne(bs => bs.BusSeat)
                .WithMany(s => s.BookingSeats)
                .HasForeignKey(bs => bs.BusSeatID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}