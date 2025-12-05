// Controllers/Api/DashboardApiController.cs - Complete corrected version
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardApiController : ControllerBase
    {
        private readonly TourismContext _context;

        public DashboardApiController(TourismContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtener estadísticas del dashboard
        /// </summary>
        // Controllers/Api/DashboardApiController.cs - Corrected version
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                // Calculate current occupancy from active reservations (CheckedIn status)
                var currentOccupancy = await _context.Reservations
                    .Where(r => r.Status == ReservationStatus.CheckedIn)
                    .SumAsync(r => r.NumberOfPeople);

                var totalCapacity = await _context.Places
                    .Where(p => p.Capacity.HasValue && p.Status == PlaceStatus.Available) // FIXED: Available instead of Active
                    .SumAsync(p => p.Capacity.Value);

                var stats = new
                {
                    TotalPlaces = await _context.Places.CountAsync(),
                    AvailablePlaces = await _context.Places.CountAsync(p => p.Status == PlaceStatus.Available), // FIXED
                    TotalReservations = await _context.Reservations.CountAsync(),
                    TodayReservations = await _context.Reservations
                        .CountAsync(r => r.StartDate.Date == today),
                    MonthlyRevenue = await _context.Reservations
                        .Where(r => r.CreatedDate >= thisMonth && r.Status != ReservationStatus.Cancelled)
                        .SumAsync(r => r.TotalAmount),
                    CurrentOccupancy = currentOccupancy, // FIXED: Calculated from reservations
                    TotalCapacity = totalCapacity,
                    OccupancyRate = totalCapacity > 0 ? Math.Round((double)currentOccupancy / totalCapacity * 100, 1) : 0,
                    CheckedInToday = await _context.Reservations
                        .CountAsync(r => r.CheckInDate.HasValue && r.CheckInDate.Value.Date == today),
                    PendingCheckouts = await _context.Reservations
                        .CountAsync(r => r.Status == ReservationStatus.CheckedIn),
                    PendingReservations = await _context.Reservations
                        .CountAsync(r => r.Status == ReservationStatus.Pending),
                    ConfirmedReservations = await _context.Reservations
                        .CountAsync(r => r.Status == ReservationStatus.Confirmed),
                    PendingAlerts = await _context.Alerts.CountAsync(a => !a.IsSent),
                    TotalAlerts = await _context.Alerts.CountAsync()
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("popular-places")]
        public async Task<IActionResult> GetPopularPlaces([FromQuery] int limit = 5)
        {
            try
            {
                var popularPlaces = await _context.Places
                    .Include(p => p.Reservations)
                    .Include(p => p.Category)
                    .Where(p => p.Status == PlaceStatus.Available && p.Category.IsActive) // FIXED: Available
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Name,
                        p.Location,
                        CategoryName = p.Category.Name,
                        CategoryIcon = p.Category.Icon,
                        CategoryColor = p.Category.Color,
                        p.Price,
                        p.Capacity,
                        ReservationCount = p.Reservations.Count,
                        MonthlyReservations = p.Reservations.Count(r => r.CreatedDate >= DateTime.Today.AddDays(-30)),
                        TotalRevenue = p.Reservations
                            .Where(r => r.Status != ReservationStatus.Cancelled)
                            .Sum(r => r.TotalAmount),
                        MonthlyRevenue = p.Reservations
                            .Where(r => r.CreatedDate >= DateTime.Today.AddDays(-30) &&
                                       r.Status != ReservationStatus.Cancelled)
                            .Sum(r => r.TotalAmount),
                        // FIXED: Calculate CurrentOccupancy from reservations
                        CurrentOccupancy = p.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn).Sum(r => r.NumberOfPeople),
                        OccupancyRate = p.Capacity.HasValue && p.Capacity > 0
                            ? Math.Round((double)p.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn).Sum(r => r.NumberOfPeople) / p.Capacity.Value * 100, 1)
                            : (double?)null
                    })
                    .OrderByDescending(p => p.ReservationCount)
                    .Take(limit)
                    .ToListAsync();

                return Ok(new { success = true, data = popularPlaces });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            try
            {
                var systemAlerts = new List<object>();
                var today = DateTime.Today;

                // Database alerts
                var dbAlerts = await _context.Alerts
                    .Include(a => a.Reservation)
                    .ThenInclude(r => r.Place)
                    .Where(a => !a.IsSent)
                    .OrderByDescending(a => a.CreatedDate)
                    .Take(10)
                    .Select(a => new
                    {
                        Type = "database_alert",
                        Title = a.Title,
                        Message = a.Message,
                        AlertType = a.Type.ToString(),
                        AlertTypeText = GetAlertTypeText(a.Type),
                        ReservationCode = a.Reservation.ReservationCode,
                        ClientName = a.Reservation.ClientName,
                        PlaceName = a.Reservation.Place.Name,
                        AlertDate = a.AlertDate,
                        CreatedDate = a.CreatedDate,
                        Severity = GetAlertSeverity(a.Type),
                        Icon = GetAlertIcon(a.Type)
                    })
                    .ToListAsync();

                systemAlerts.AddRange(dbAlerts);

                
                var highOccupancyPlaces = await _context.Places
                    .Include(p => p.Reservations)
                    .Where(p => p.Status == PlaceStatus.Available && p.Capacity.HasValue && p.Capacity > 0) // FIXED: Available
                    .Select(p => new
                    {
                        Place = p,
                        CurrentOccupancy = p.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn).Sum(r => r.NumberOfPeople), // FIXED
                        OccupancyRate = (double)p.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn).Sum(r => r.NumberOfPeople) / p.Capacity.Value
                    })
                    .Where(x => x.OccupancyRate >= 0.9)
                    .Select(x => new
                    {
                        Type = "high_occupancy",
                        Title = "Alta Ocupación",
                        Message = $"{x.Place.Name} está al {Math.Round(x.OccupancyRate * 100, 1)}% de capacidad",
                        PlaceId = x.Place.Id,
                        PlaceName = x.Place.Name,
                        PlaceCode = x.Place.Code,
                        Severity = "warning",
                        Icon = "fas fa-exclamation-triangle",
                        CreatedDate = DateTime.Now
                    })
                    .ToListAsync();

                systemAlerts.AddRange(highOccupancyPlaces);

                

                return Ok(new { success = true, data = systemAlerts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("category-performance")]
        public async Task<IActionResult> GetCategoryPerformance()
        {
            try
            {
                var thisMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                var categoryPerformance = await _context.Categories
                    .Include(c => c.Places)
                    .ThenInclude(p => p.Reservations)
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Icon,
                        c.Color,
                        TotalPlaces = c.Places.Count,
                        AvailablePlaces = c.Places.Count(p => p.Status == PlaceStatus.Available), 
                        TotalCapacity = c.Places.Where(p => p.Capacity.HasValue).Sum(p => p.Capacity.Value),
 
                        CurrentOccupancy = c.Places.SelectMany(p => p.Reservations)
                            .Where(r => r.Status == ReservationStatus.CheckedIn)
                            .Sum(r => r.NumberOfPeople),
                        MonthlyReservations = c.Places.SelectMany(p => p.Reservations)
                            .Count(r => r.CreatedDate >= thisMonth),
                        MonthlyRevenue = c.Places.SelectMany(p => p.Reservations)
                            .Where(r => r.CreatedDate >= thisMonth && r.Status != ReservationStatus.Cancelled)
                            .Sum(r => r.TotalAmount),
                        TotalReservations = c.Places.SelectMany(p => p.Reservations).Count(),
                        AveragePrice = c.Places.Any() ? c.Places.Average(p => p.Price) : 0
                    })
                    .Where(c => c.TotalPlaces > 0)
                    .OrderByDescending(c => c.MonthlyRevenue)
                    .ToListAsync();

                var performanceWithRates = categoryPerformance.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Icon,
                    c.Color,
                    c.TotalPlaces,
                    c.AvailablePlaces,
                    c.TotalCapacity,
                    c.CurrentOccupancy,
                    c.MonthlyReservations,
                    MonthlyRevenue = Math.Round(c.MonthlyRevenue, 2),
                    c.TotalReservations,
                    AveragePrice = Math.Round(c.AveragePrice, 2),
                    OccupancyRate = c.TotalCapacity > 0 ? Math.Round((double)c.CurrentOccupancy / c.TotalCapacity * 100, 1) : 0,
                    RevenuePerPlace = c.AvailablePlaces > 0 ? Math.Round(c.MonthlyRevenue / c.AvailablePlaces, 2) : 0
                });

                return Ok(new { success = true, data = performanceWithRates });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        // Métodos auxiliares
        private static string GetStatusText(ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => "Pendiente",
                ReservationStatus.Confirmed => "Confirmada",
                ReservationStatus.CheckedIn => "Check-In",
                ReservationStatus.CheckedOut => "Check-Out",
                ReservationStatus.Completed => "Completada",
                ReservationStatus.Cancelled => "Cancelada",
                _ => "Desconocido"
            };
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

        private static string GetAlertSeverity(AlertType type)
        {
            return type switch
            {
                AlertType.ReservationReminder => "info",
                AlertType.CheckInReminder => "warning",
                AlertType.CheckOutReminder => "warning",
                AlertType.PaymentReminder => "error",
                AlertType.CancellationNotice => "error",
                _ => "info"
            };
        }

        private static string GetAlertIcon(AlertType type)
        {
            return type switch
            {
                AlertType.ReservationReminder => "fas fa-calendar-check",
                AlertType.CheckInReminder => "fas fa-sign-in-alt",
                AlertType.CheckOutReminder => "fas fa-sign-out-alt",
                AlertType.PaymentReminder => "fas fa-credit-card",
                AlertType.CancellationNotice => "fas fa-times-circle",
                _ => "fas fa-bell"
            };
        }
    }
}