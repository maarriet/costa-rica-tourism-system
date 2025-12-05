namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new DashboardStats();
        public List<RecentAlert> RecentAlerts { get; set; } = new List<RecentAlert>();
        public List<CategoryStats> CategoryStats { get; set; } = new List<CategoryStats>();
        public List<PopularPlace> PopularPlaces { get; set; } = new List<PopularPlace>();
    }

    public class DashboardStats
    {
        public int TotalPlaces { get; set; }
        public int ActiveReservations { get; set; }
        public int PendingCheckIns { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int PlacesGrowth { get; set; }
        public int ReservationsGrowth { get; set; }
        public decimal RevenueGrowth { get; set; }
    }

    public class RecentAlert
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Icon { get; set; }
        public string CssClass { get; set; }
    }

    public class CategoryStats
    {
        public string CategoryName { get; set; }
        public int PlaceCount { get; set; }
        public int ReservationCount { get; set; }
        public decimal Revenue { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
    }

    public class PopularPlace
    {
        public string Name { get; set; }
        public int ReservationCount { get; set; }
        public decimal Percentage { get; set; }
        public int Rank { get; set; }
    }

}
