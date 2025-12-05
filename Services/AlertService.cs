using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Services
{
    public class AlertService : IAlertService
    {
        private readonly TourismContext _context;

        public AlertService(TourismContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Alert>> GetActiveAlertsAsync()
        {
            return await _context.Alerts
                .Where(a => !a.IsSent && a.AlertDate <= DateTime.Now)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alert>> GetAlertsByTypeAsync(AlertType type)
        {
            return await _context.Alerts
                .Where(a => a.Type == type && !a.IsSent)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task<Alert> CreateAlertAsync(Alert alert)
        {
            alert.CreatedDate = DateTime.Now;
            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();
            return alert;
        }

        public async Task MarkAsReadAsync(int alertId)
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            if (alert != null)
            {
                alert.IsSent = true;
                alert.SentDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAlertAsync(int alertId)
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            if (alert != null)
            {
                _context.Alerts.Remove(alert);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await _context.Alerts
                .CountAsync(a => !a.IsSent && a.AlertDate <= DateTime.Now);
        }
    }
}