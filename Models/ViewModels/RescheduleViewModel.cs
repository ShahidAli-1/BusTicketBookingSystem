using System.ComponentModel.DataAnnotations;

namespace BusTicketBookingSystem.ViewModels
{
    public class RescheduleViewModel
    {
        // ============ SOURCE (read-only display) ============
        public int SourceScheduleID { get; set; }
        public string OldBusNumber { get; set; } = string.Empty;
        public string OldBusName { get; set; } = string.Empty;
        public string OldRouteName { get; set; } = string.Empty;
        public DateTime OldTravelDate { get; set; }
        public TimeSpan OldDepartureTime { get; set; }
        public TimeSpan OldArrivalTime { get; set; }
        public decimal OldFare { get; set; }

        // ============ NEW SCHEDULE (fully editable) ============

        [Required]
        [Display(Name = "Bus")]
        public int NewBusID { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Travel Date")]
        public DateTime NewTravelDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [Display(Name = "Departure Time")]
        public TimeSpan NewDepartureTime { get; set; } = new TimeSpan(9, 0, 0);

        [Required]
        [Display(Name = "Arrival Time")]
        public TimeSpan NewArrivalTime { get; set; } = new TimeSpan(13, 0, 0);

        [Required, Range(1, 1000000)]
        [Display(Name = "Fare (Rs.)")]
        public decimal NewFare { get; set; }

        // ============ DROPDOWN DATA ============
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> BusOptions { get; set; } = new();
    }
}