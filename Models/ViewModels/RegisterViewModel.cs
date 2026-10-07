using System.ComponentModel.DataAnnotations;

namespace BusTicketBookingSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required, StringLength(50, MinimumLength = 3)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        [Display(Name = "Contact Number")]
        public string ContactNo { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;
    }
}