using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketBookingSystem.Models
{
    public class PaymentRequest
    {
        [Key]
        public int PaymentRequestID { get; set; }

        [Required]
        public int BookingID { get; set; }

        [ForeignKey("BookingID")]
        public Booking? Booking { get; set; }

        [Required, Range(1, 1000000)]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public string? PaymentReference { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public int? ReviewedBy { get; set; }

        [ForeignKey("ReviewedBy")]
        public Admin? Admin { get; set; }

        public DateTime? ReviewedAt { get; set; }
        public string? AdminRemarks { get; set; }
    }
}