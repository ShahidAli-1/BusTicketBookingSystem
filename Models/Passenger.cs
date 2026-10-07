using System.ComponentModel.DataAnnotations;

namespace BusTicketBookingSystem.Models
{
    public class Passenger
    {
        [Key]
        public int PassengerID { get; set; }

        [Required, StringLength(50, MinimumLength = 3)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, Phone]
        [Display(Name = "Contact Number")]
        public string ContactNo { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Booking>? Bookings { get; set; }
    }
}