// Controllers/Api/CategoriesApiController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly TourismContext _context;

        public CategoriesApiController(TourismContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtener todas las categorías
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Places)
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.Icon,
                        c.Color,
                        c.IsActive,
                        c.CreatedDate,
                        PlaceCount = c.Places.Count,
                        ActivePlaceCount = c.Places.Count(p => p.Status == PlaceStatus.Available),
                        TotalCapacity = c.Places.Where(p => p.Capacity.HasValue).Sum(p => p.Capacity.Value),
                        TotalRevenue = c.Places.SelectMany(p => p.Reservations)
                            .Where(r => r.Status != ReservationStatus.Cancelled)
                            .Sum(r => r.TotalAmount)
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Ok(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener todas las categorías (incluyendo inactivas)
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Places)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        c.Icon,
                        c.Color,
                        c.IsActive,
                        c.CreatedDate,
                        PlaceCount = c.Places.Count,
                        ActivePlaceCount = c.Places.Count(p => p.Status == PlaceStatus.Available)
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Ok(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener una categoría específica por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Places)
                    .ThenInclude(p => p.Reservations)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound(new { success = false, message = "Categoría no encontrada" });

                var result = new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.Icon,
                    category.Color,
                    category.IsActive,
                    category.CreatedDate,
                    PlaceCount = category.Places.Count,
                    ActivePlaceCount = category.Places.Count(p => p.Status == PlaceStatus.Available),
                    Places = category.Places.Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Name,
                        p.Location,
                        p.Price,
                        p.Capacity,
                        p.Status,
                        StatusText = GetPlaceStatusText(p.Status),
                        ReservationCount = p.Reservations.Count,
                        TotalRevenue = p.Reservations
                            .Where(r => r.Status != ReservationStatus.Cancelled)
                            .Sum(r => r.TotalAmount)
                    }).OrderBy(p => p.Name)
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Crear una nueva categoría
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

                // Verificar si el nombre ya existe
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());

                if (existingCategory != null)
                    return BadRequest(new { success = false, message = "Ya existe una categoría con este nombre" });

                category.CreatedDate = DateTime.Now;
                category.IsActive = true;

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id },
                    new { success = true, message = "Categoría creada exitosamente", data = category });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar una categoría existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound(new { success = false, message = "Categoría no encontrada" });

                // Verificar si el nuevo nombre ya existe (excluyendo la categoría actual)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower() && c.Id != id);

                if (existingCategory != null)
                    return BadRequest(new { success = false, message = "Ya existe una categoría con este nombre" });

                category.Name = model.Name;
                category.Description = model.Description;
                category.Icon = model.Icon;
                category.Color = model.Color;
                category.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Categoría actualizada exitosamente", data = category });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar una categoría
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Places)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound(new { success = false, message = "Categoría no encontrada" });

                if (category.Places.Any())
                    return BadRequest(new { success = false, message = "No se puede eliminar una categoría que tiene lugares asignados" });

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Categoría eliminada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Activar/Desactivar una categoría
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<IActionResult> ToggleCategoryStatus(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound(new { success = false, message = "Categoría no encontrada" });

                category.IsActive = !category.IsActive;
                await _context.SaveChangesAsync();

                var statusText = category.IsActive ? "activada" : "desactivada";
                return Ok(new
                {
                    success = true,
                    message = $"Categoría {statusText} exitosamente",
                    data = new { category.Id, category.IsActive }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener estadísticas de una categoría
        /// </summary>
        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetCategoryStats(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Places)
                    .ThenInclude(p => p.Reservations)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return NotFound(new { success = false, message = "Categoría no encontrada" });

                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var stats = new
                {
                    CategoryName = category.Name,
                    TotalPlaces = category.Places.Count,
                    AvailablePlaces = category.Places.Count(p => p.Status == PlaceStatus.Available),
                    TotalCapacity = category.Places.Where(p => p.Capacity.HasValue).Sum(p => p.Capacity.Value),
                    CurrentOccupancy = category.Places.SelectMany(p => p.Reservations)
                        .Count(r => r.Status == ReservationStatus.CheckedIn),
                    TotalReservations = category.Places.SelectMany(p => p.Reservations).Count(),
                    TodayReservations = category.Places.SelectMany(p => p.Reservations)
                        .Count(r => r.StartDate.Date == today),
                    MonthlyReservations = category.Places.SelectMany(p => p.Reservations)
                        .Count(r => r.CreatedDate >= thisMonth),
                    TotalRevenue = category.Places.SelectMany(p => p.Reservations)
                        .Where(r => r.Status != ReservationStatus.Cancelled)
                        .Sum(r => r.TotalAmount),
                    MonthlyRevenue = category.Places.SelectMany(p => p.Reservations)
                        .Where(r => r.CreatedDate >= thisMonth && r.Status != ReservationStatus.Cancelled)
                        .Sum(r => r.TotalAmount),
                    AveragePrice = category.Places.Any() ? category.Places.Average(p => p.Price) : 0,
                    OccupancyRate = category.Places.Where(p => p.Capacity.HasValue).Sum(p => p.Capacity.Value) > 0
                        ? Math.Round((double)category.Places.SelectMany(p => p.Reservations).Count(r => r.Status == ReservationStatus.CheckedIn) /
                                    category.Places.Where(p => p.Capacity.HasValue).Sum(p => p.Capacity.Value) * 100, 1)
                        : 0
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Método auxiliar
        private static string GetPlaceStatusText(PlaceStatus status)
        {
            return status switch
            {
                PlaceStatus.Available => "Disponible",
                PlaceStatus.Occupied => "Ocupado",
                PlaceStatus.Maintenance => "Mantenimiento",
                PlaceStatus.Inactive => "Inactivo",
                _ => "Desconocido"
            };
        }
    }
}