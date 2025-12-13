// ViewModels/ReportsViewModel.cs
// ViewModels/ReportsViewModel.cs
using System.Globalization;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class ReportsViewModel
    {
        public List<PlaceVisitReport> MostVisitedPlaces { get; set; } = new();
        public List<MonthlyReservationReport> ReservationsByMonth { get; set; } = new();
        public List<CategoryOccupancyReport> OccupancyByCategory { get; set; } = new();
    }

    public class PlaceVisitReport
    {
        public string PlaceName { get; set; } = "";
        public int TotalReservations { get; set; }
        public int TotalGuests { get; set; }
        public decimal TotalRevenue { get; set; }
        public string Category { get; set; } = "";
    }

    public class MonthlyReservationReport
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalReservations { get; set; }
        public decimal TotalRevenue { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy", new CultureInfo("es-ES"));
    }

    public class CategoryOccupancyReport
    {
        public string CategoryName { get; set; } = "";
        public int TotalReservations { get; set; }
        public int TotalGuests { get; set; }
        public double? AverageStay { get; set; }
        public decimal TotalRevenue { get; set; }
        public string AverageStayText => AverageStay.HasValue ? $"{AverageStay.Value:F1} días" : "N/A";
    }
}