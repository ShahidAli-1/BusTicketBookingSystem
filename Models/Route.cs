using System.ComponentModel.DataAnnotations;

namespace BusTicketBookingSystem.Models
{
    public class Route
    {
        [Key]
        public int RouteID { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Route Name")]
        public string RouteName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Origin { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Destination { get; set; } = string.Empty;

        [Range(1, 10000)]
        [Display(Name = "Distance (km)")]
        public int DistanceKm { get; set; }

        [Required]
        [Display(Name = "Estimated Duration")]
        public string EstimatedDuration { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<Bus>? Buses { get; set; }
    }
}