using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.Services
{
    public interface IReportService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
        Task<byte[]> GenerateReservationReportAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<byte[]> GenerateRevenueReportAsync(DateTime? dateFrom, DateTime? dateTo);
        Task<byte[]> GeneratePlaceOccupancyReportAsync();
    }
}