using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusTicketBookingSystem.Models
{
    public class Bus
    {
        [Key]
        public int BusID { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Bus Number")]
        public string BusNumber { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Bus Name")]
        public string BusName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Bus Type")]
        public string BusType { get; set; } = "Standard";

        [Range(1, 100)]
        [Display(Name = "Total Seats")]
        public int TotalSeats { get; set; }

        [Required]
        [Display(Name = "Route")]
        public int RouteID { get; set; }

        [ForeignKey("RouteID")]
        public Route? Route { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Schedule>? Schedules { get; set; }
    }
}