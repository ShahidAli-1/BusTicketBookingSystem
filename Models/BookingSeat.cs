using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketBookingSystem.Models
{
    public class BookingSeat
    {
        [Key]
        public int BookingSeatID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [ForeignKey("BookingID")]
        public Booking? Booking { get; set; }

        [Required]
        public int BusSeatID { get; set; }

        [ForeignKey("BusSeatID")]
        public BusSeat? BusSeat { get; set; }

        public decimal Fare { get; set; }
    }
}