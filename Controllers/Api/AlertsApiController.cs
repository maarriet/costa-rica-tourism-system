// Controllers/Api/AlertsApiController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsApiController : ControllerBase
    {
        private readonly TourismContext _context;

        public AlertsApiController(TourismContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtener todas las alertas
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAlerts(
            [FromQuery] AlertType? type = null,
            [FromQuery] bool? isSent = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Alerts
                    .Include(a => a.Reservation)
                    .ThenInclude(r => r.Place)
                    .AsQueryable();

                if (type.HasValue)
                    query = query.Where(a => a.Type == type.Value);

                if (isSent.HasValue)
                    query = query.Where(a => a.IsSent == isSent.Value);

                var totalCount = await query.CountAsync();
                var alerts = await query
                    .OrderByDescending(a => a.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        a.Id,
                        a.ReservationId,
                        ReservationCode = a.Reservation.ReservationCode,
                        ClientName = a.Reservation.ClientName,
                        PlaceName = a.Reservation.Place.Name,
                        a.Type,
                        TypeText = GetAlertTypeText(a.Type),
                        a.Title,
                        a.Message,
                        a.AlertDate,
                        a.IsSent,
                        a.SentDate,
                        a.CreatedDate
                    })
                    .ToListAsync();

                var result = new
                {
                    alerts,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    },
                    summary = new
                    {
                        pendingCount = await _context.Alerts.CountAsync(a => !a.IsSent),
                        sentCount = await _context.Alerts.CountAsync(a => a.IsSent),
                        todayCount = await _context.Alerts.CountAsync(a => a.CreatedDate.Date == DateTime.Today)
                    }
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Marcar alerta como enviada
        /// </summary>
        [HttpPatch("{id}/mark-sent")]
        public async Task<IActionResult> MarkAsSent(int id)
        {
            try
            {
                var alert = await _context.Alerts.FindAsync(id);
                if (alert == null)
                    return NotFound(new { success = false, message = "Alerta no encontrada" });

                alert.IsSent = true;
                alert.SentDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Alerta marcada como enviada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Crear nueva alerta
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAlert([FromBody] CreateAlertRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

                // Verificar que la reserva existe
                var reservationExists = await _context.Reservations.AnyAsync(r => r.Id == request.ReservationId);
                if (!reservationExists)
                    return BadRequest(new { success = false, message = "Reserva no encontrada" });

                var alert = new Alert
                {
                    ReservationId = request.ReservationId,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    AlertDate = request.AlertDate,
                    CreatedDate = DateTime.Now
                };

                _context.Alerts.Add(alert);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Alerta creada exitosamente", data = alert });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar alerta
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlert(int id)
        {
            try
            {
                var alert = await _context.Alerts.FindAsync(id);
                if (alert == null)
                    return NotFound(new { success = false, message = "Alerta no encontrada" });

                _context.Alerts.Remove(alert);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Alerta eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private static string GetAlertTypeText(AlertType type)
        {
            return type switch
            {
                AlertType.ReservationReminder => "Recordatorio de Reserva",
                AlertType.CheckInReminder => "Recordatorio de Check-In",
                AlertType.CheckOutReminder => "Recordatorio de Check-Out",
                AlertType.PaymentReminder => "Recordatorio de Pago",
                AlertType.CancellationNotice => "Aviso de Cancelación",
                _ => "Desconocido"
            };
        }
    }

    public class CreateAlertRequest
    {
        public int ReservationId { get; set; }
        public AlertType Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime AlertDate { get; set; }
    }
}