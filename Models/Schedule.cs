using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketBookingSystem.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleID { get; set; }

        [Required]
        public int BusID { get; set; }

        [ForeignKey("BusID")]
        public Bus? Bus { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Travel Date")]
        public DateTime TravelDate { get; set; }

        [Required]
        [Display(Name = "Departure Time")]
        public TimeSpan DepartureTime { get; set; }

        [Required]
        [Display(Name = "Arrival Time")]
        public TimeSpan ArrivalTime { get; set; }

        [Required, Range(1, 100000)]
        public decimal Fare { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        public ICollection<BusSeat>? Seats { get; set; }
        public ICollection<Booking>? Bookings { get; set; }
    }
}