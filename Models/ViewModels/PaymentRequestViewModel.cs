using System.ComponentModel.DataAnnotations;

namespace BusTicketBookingSystem.ViewModels
{
    public class PaymentRequestViewModel
    {
        public int BookingId { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Display(Name = "Payment Reference")]
        public string? PaymentReference { get; set; }
    }
}