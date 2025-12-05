// ViewModels/PlaceAvailabilityViewModel.cs
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class PlaceAvailabilityViewModel
    {
        public int PlaceId { get; set; }
        public string PlaceName { get; set; }
        public string PlaceCode { get; set; }
        public DateTime Date { get; set; }
        public int? Capacity { get; set; }
        public int ReservedSpots { get; set; }
        public int? AvailableSpots { get; set; }
        public bool IsAvailable { get; set; }
        public double? OccupancyPercentage { get; set; }
        public PlaceStatus Status { get; set; }
        public string StatusText { get; set; }
    }
}