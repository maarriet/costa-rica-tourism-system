// ViewModels/PlaceStatisticsViewModel.cs
namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class PlaceStatisticsViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public int TotalReservations { get; set; }
        public int ActiveReservations { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double OccupancyRate { get; set; }
        public double AverageRating { get; set; }
        public int CurrentOccupancy { get; set; }
        public int? Capacity { get; set; }
    }
}