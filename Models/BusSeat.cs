using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketBookingSystem.Models
{
    public class BusSeat
    {
        [Key]
        public int BusSeatID { get; set; }

        [Required]
        public int ScheduleID { get; set; }

        [ForeignKey("ScheduleID")]
        public Schedule? Schedule { get; set; }

        [Required]
        public int SeatNumber { get; set; }

        [Required]
        public string Status { get; set; } = "Available"; // Available, Reserved, Booked

        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<BookingSeat>? BookingSeats { get; set; }

    }
}