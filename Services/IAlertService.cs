using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Services
{
    public interface IAlertService
    {
        Task<IEnumerable<Alert>> GetActiveAlertsAsync();
        Task<IEnumerable<Alert>> GetAlertsByTypeAsync(AlertType type);
        Task<Alert> CreateAlertAsync(Alert alert);
        Task MarkAsReadAsync(int alertId);
        Task DeleteAlertAsync(int alertId);
        Task<int> GetUnreadCountAsync();
    }
}