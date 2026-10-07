using System.ComponentModel.DataAnnotations;

namespace BusTicketBookingSystem.Models
{
    public class Admin
    {
        [Key]
        public int AdminID { get; set; }

        [Required, StringLength(50, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public ICollection<PaymentRequest>? Reviews { get; set; }
    }
}