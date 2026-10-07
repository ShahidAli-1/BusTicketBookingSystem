using System.ComponentModel.DataAnnotations;

namespace BusTicketBookingSystem.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        [Display(Name = "Registered Email")]
        public string Email { get; set; } = string.Empty;
    }
}