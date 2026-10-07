using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketBookingSystem.Models
{
    public class PasswordResetCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PassengerID { get; set; }

        [ForeignKey("PassengerID")]
        public Passenger? Passenger { get; set; }

        [Required, StringLength(6)]
        public string Code { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}