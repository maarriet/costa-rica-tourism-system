// Services/ReservationAlertService.cs
using Sistema_GuiaLocal_Turismo.Data;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Services
{
    public class ReservationAlertService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReservationAlertService> _logger;

        public ReservationAlertService(IServiceProvider serviceProvider, ILogger<ReservationAlertService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckUpcomingReservations();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Revisar cada 24 horas
            }
        }

        private async Task CheckUpcomingReservations()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TourismContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var threeDaysFromNow = DateTime.Now.AddDays(3).Date;

            // ✅ CORRECTO - Usar StartDate en lugar de CheckInDate
            var upcomingReservations = await context.Reservations
                .Include(r => r.Place)
                .Where(r => r.StartDate.Date == threeDaysFromNow && !r.AlertSent)
                .ToListAsync();

            foreach (var reservation in upcomingReservations)
            {
                await SendReservationAlert(reservation, emailService);
                reservation.AlertSent = true;
            }

            if (upcomingReservations.Any())
            {
                await context.SaveChangesAsync();
            }

            _logger.LogInformation($"Procesadas {upcomingReservations.Count} alertas de reservas");
        }

        private async Task SendReservationAlert(Reservation reservation, IEmailService emailService)
        {
            var subject = "🇨🇷 Recordatorio: Tu reserva en Costa Rica es en 3 días";
            var htmlContent = GenerateEmailTemplate(reservation);

            try
            {
                await emailService.SendEmailAsync(reservation.ClientEmail, subject, htmlContent);
                _logger.LogInformation($"Alerta enviada para reserva {reservation.ReservationCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enviando alerta para reserva {reservation.ReservationCode}");
            }
        }

        private string GenerateEmailTemplate(Reservation reservation)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #00695c;'>🇨🇷 ¡Tu aventura en Costa Rica está cerca!</h2>
                        
                        <p>Hola {reservation.ClientName},</p>
                        
                        <p>Te recordamos que tu reserva está programada para <strong>dentro de 3 días</strong>.</p>
                        
                        <div style='background: #f8f9fa; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                            <h3 style='color: #00695c; margin-top: 0;'>Detalles de tu Reserva</h3>
                            <p><strong>Código:</strong> {reservation.ReservationCode}</p>
                            <p><strong>Lugar:</strong> {reservation.Place?.Name ?? ""}</p>
                            <p><strong>Fecha de Inicio:</strong> {reservation.StartDate:dd/MM/yyyy}</p>
                            {(reservation.EndDate.HasValue ? $"<p><strong>Fecha de Fin:</strong> {reservation.EndDate.Value:dd/MM/yyyy}</p>" : "")}
                            <p><strong>Huéspedes:</strong> {reservation.NumberOfPeople}</p>
                            <p><strong>Total:</strong> ₡{reservation.TotalAmount:N0}</p>
                        </div>
                        
                        <p>¡Esperamos que disfrutes tu experiencia! 🌴</p>
                        
                        <p style='color: #666; font-size: 12px;'>
                            Sistema de Turismo Costa Rica<br>
                            Este es un mensaje automático, no responder.
                        </p>
                    </div>
                </body>
                </html>";
        }
    }
}