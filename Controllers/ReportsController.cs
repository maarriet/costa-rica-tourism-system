// Controllers/ReportsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.ViewModels;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ReportsController : Controller
    {
        private readonly TourismContext _context;

        public ReportsController(TourismContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new ReportsViewModel
            {
                // Lugares más visitados
                MostVisitedPlaces = await GetMostVisitedPlaces(),

                // Reservas por mes
                ReservationsByMonth = await GetReservationsByMonth(),

                // Ocupación por categoría
                OccupancyByCategory = await GetOccupancyByCategory()
            };

            return View(model);
        }

        private async Task<List<PlaceVisitReport>> GetMostVisitedPlaces()
        {
            return await _context.Reservations
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .Where(r => r.Place != null)
                .GroupBy(r => new { r.Place.Id, r.Place.Name, CategoryName = r.Place.Category.Name })
                .Select(g => new PlaceVisitReport
                {
                    PlaceName = g.Key.Name,
                    TotalReservations = g.Count(),
                    TotalGuests = g.Sum(r => r.NumberOfPeople),
                    TotalRevenue = g.Sum(r => r.TotalAmount),
                    Category = g.Key.CategoryName
                })
                .OrderByDescending(x => x.TotalReservations)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<MonthlyReservationReport>> GetReservationsByMonth()
        {
            return await _context.Reservations
                .GroupBy(r => new { r.StartDate.Year, r.StartDate.Month })
                .Select(g => new MonthlyReservationReport
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalReservations = g.Count(),
                    TotalRevenue = g.Sum(r => r.TotalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();
        }

        private async Task<List<CategoryOccupancyReport>> GetOccupancyByCategory()
        {
            // Obtener datos básicos por categoría
            var categoryStats = await _context.Reservations
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .Where(r => r.Place != null && r.Place.Category != null)
                .GroupBy(r => r.Place.Category.Name)
                .Select(g => new CategoryOccupancyReport
                {
                    CategoryName = g.Key,
                    TotalReservations = g.Count(),
                    TotalGuests = g.Sum(r => r.NumberOfPeople),
                    TotalRevenue = g.Sum(r => r.TotalAmount),
                    AverageStay = null // Lo calcularemos después
                })
                .ToListAsync();

            // Calcular promedio de estadía en memoria (más simple)
            foreach (var category in categoryStats)
            {
                var reservationsWithEndDate = await _context.Reservations
                    .Include(r => r.Place)
                    .ThenInclude(p => p.Category)
                    .Where(r => r.Place.Category.Name == category.CategoryName && r.EndDate.HasValue)
                    .Select(r => new { r.StartDate, r.EndDate })
                    .ToListAsync();

                if (reservationsWithEndDate.Any())
                {
                    category.AverageStay = reservationsWithEndDate
                        .Average(r => (r.EndDate.Value - r.StartDate).TotalDays);
                }
            }

            return categoryStats;
        }

        [HttpGet]
        public async Task<IActionResult> ExportReservations(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Reservations.Include(r => r.Place).AsQueryable();

            if (startDate.HasValue)
                query = query.Where(r => r.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.StartDate <= endDate.Value);

            var reservations = await query.OrderByDescending(r => r.StartDate).ToListAsync();

            var csv = GenerateCSV(reservations);
            var fileName = $"reservas_{DateTime.Now:yyyyMMdd}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        private string GenerateCSV(List<Reservation> reservations)
        {
            var csv = "Código,Cliente,Email,Lugar,Fecha Inicio,Fecha Fin,Huéspedes,Total,Estado\n";

            foreach (var r in reservations)
            {
                csv += $"{r.ReservationCode},{r.ClientName},{r.ClientEmail},{r.Place?.Name ?? "N/A"}," +
                       $"{r.StartDate:yyyy-MM-dd},{r.EndDate?.ToString("yyyy-MM-dd") ?? "N/A"}," +
                       $"{r.NumberOfPeople},{r.TotalAmount},{r.Status}\n";
            }

            return csv;
        }
    }
}