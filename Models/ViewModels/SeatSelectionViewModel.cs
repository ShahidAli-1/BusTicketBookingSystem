using BusTicketBookingSystem.Models;

namespace BusTicketBookingSystem.ViewModels
{
    public class SeatSelectionViewModel
    {
        public int ScheduleID { get; set; }
        public Schedule? Schedule { get; set; }
        public List<BusSeat> Seats { get; set; } = new();

        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string BusName { get; set; } = string.Empty;
        public string BusNumber { get; set; } = string.Empty;
        public DateTime TravelDate { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public decimal Fare { get; set; }

        public int MaxSeatsPerBooking { get; set; } = 5;

        // ⭐ Real seat count for this bus (e.g. 30)
        public int TotalSeatsInBus { get; set; }

        // ⭐ Always render this many cells (e.g. 45)
        public int VisualSeatCount { get; set; } = 45;
    }
}