using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketBookingSystem.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [Required, StringLength(30)]
        public string BookingNumber { get; set; } = string.Empty;

        [Required]
        public int PassengerID { get; set; }
        [ForeignKey("PassengerID")]
        public Passenger? Passenger { get; set; }

        [Required]
        public int ScheduleID { get; set; }
        [ForeignKey("ScheduleID")]
        public Schedule? Schedule { get; set; }

        [Required]
        public int BusSeatID { get; set; }
        [ForeignKey("BusSeatID")]
        public BusSeat? BusSeat { get; set; }

        [Required]
        public decimal Fare { get; set; }

        [Required]
        public string Status { get; set; } = "Reserved";

        public DateTime ReservedAt { get; set; } = DateTime.Now;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? TicketNumber { get; set; }

        public ICollection<PaymentRequest>? PaymentRequests { get; set; }
        public ICollection<BookingSeat>? BookingSeats { get; set; }

       
        [NotMapped]
        public string SeatNumbersDisplay
        {
            get
            {
                if (BookingSeats != null && BookingSeats.Any())
                {
                    return string.Join(", ", BookingSeats
                        .Where(bs => bs.BusSeat != null)
                        .Select(bs => bs.BusSeat!.SeatNumber)
                        .OrderBy(n => n));
                }
               
                return BusSeat?.SeatNumber.ToString() ?? "—";
            }
        }

        [NotMapped]
        public int SeatCount
        {
            get
            {
                if (BookingSeats != null && BookingSeats.Any())
                    return BookingSeats.Count;
                return BusSeat != null ? 1 : 0;
            }
        }
    }
}