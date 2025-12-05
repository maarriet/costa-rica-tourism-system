// Controllers/Api/PlacesApiController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlacesApiController : ControllerBase
    {
        private readonly TourismContext _context;

        public PlacesApiController(TourismContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtener todos los lugares con filtros opcionales
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPlaces(
            [FromQuery] int? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] PlaceStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Places
                    .Include(p => p.Category)
                    .Include(p => p.Reservations)
                    .AsQueryable();

                // Aplicar filtros
                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(p => p.Name.Contains(search) ||
                                           p.Description.Contains(search) ||
                                           p.Location.Contains(search));

                if (status.HasValue)
                    query = query.Where(p => p.Status == status.Value);

                var totalCount = await query.CountAsync();
                var places = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Name,
                        p.Description,
                        p.CategoryId,
                        CategoryName = p.Category.Name,
                        p.Price,
                        p.Capacity,
                        p.Location,
                        p.Status,
                        StatusText = GetStatusText(p.Status),
                        p.CreatedDate,
                        p.UpdatedDate,
                        ReservationCount = p.Reservations.Count,
                        ActiveReservations = p.Reservations.Count(r =>
                            r.Status == ReservationStatus.Confirmed ||
                            r.Status == ReservationStatus.CheckedIn),
                        MonthlyRevenue = p.Reservations
                            .Where(r => r.CreatedDate >= DateTime.Today.AddDays(-30) &&
                                       r.Status != ReservationStatus.Cancelled)
                            .Sum(r => r.TotalAmount)
                    })
                    .ToListAsync();

                var result = new
                {
                    places,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
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
        /// Obtener un lugar específico por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlace(int id)
        {
            try
            {
                var place = await _context.Places
                    .Include(p => p.Category)
                    .Include(p => p.Reservations)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (place == null)
                    return NotFound(new { success = false, message = $"Lugar con ID {id} no encontrado" });

                var result = new
                {
                    place.Id,
                    place.Code,
                    place.Name,
                    place.Description,
                    place.CategoryId,
                    CategoryName = place.Category.Name,
                    place.Price,
                    place.Capacity,
                    place.Location,
                    place.Status,
                    StatusText = GetStatusText(place.Status),
                    place.CreatedDate,
                    place.UpdatedDate,
                    ReservationCount = place.Reservations.Count,
                    ActiveReservations = place.Reservations.Count(r =>
                        r.Status == ReservationStatus.Confirmed ||
                        r.Status == ReservationStatus.CheckedIn),
                    TotalRevenue = place.Reservations
                        .Where(r => r.Status != ReservationStatus.Cancelled)
                        .Sum(r => r.TotalAmount),
                    RecentReservations = place.Reservations
                        .OrderByDescending(r => r.CreatedDate)
                        .Take(5)
                        .Select(r => new
                        {
                            r.Id,
                            r.ReservationCode,
                            r.ClientName,
                            r.StartDate,
                            r.Status,
                            r.NumberOfPeople,
                            r.TotalAmount
                        })
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Crear un nuevo lugar
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePlace([FromBody] PlaceViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

                // Verificar si la categoría existe
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == model.CategoryId);

                if (!categoryExists)
                    return BadRequest(new { success = false, message = "ID de categoría inválido" });

                // Verificar si el código ya existe
                var codeExists = await _context.Places
                    .AnyAsync(p => p.Code == model.Code);

                if (codeExists)
                    return BadRequest(new { success = false, message = "El código ya existe" });

                var place = new Place
                {
                    Code = model.Code,
                    Name = model.Name,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    Price = model.Price,
                    Capacity = model.Capacity,
                    Location = model.Location,
                    Status = PlaceStatus.Available,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _context.Places.Add(place);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPlace), new { id = place.Id },
                    new { success = true, message = "Lugar creado exitosamente", data = place });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar un lugar existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlace(int id, [FromBody] PlaceViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

                var place = await _context.Places.FindAsync(id);
                if (place == null)
                    return NotFound(new { success = false, message = $"Lugar con ID {id} no encontrado" });

                // Verificar si la categoría existe
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == model.CategoryId);

                if (!categoryExists)
                    return BadRequest(new { success = false, message = "ID de categoría inválido" });

                // Verificar si el código ya existe (excluyendo el lugar actual)
                var codeExists = await _context.Places
                    .AnyAsync(p => p.Code == model.Code && p.Id != id);

                if (codeExists)
                    return BadRequest(new { success = false, message = "El código ya existe" });

                place.Code = model.Code;
                place.Name = model.Name;
                place.Description = model.Description;
                place.CategoryId = model.CategoryId;
                place.Price = model.Price;
                place.Capacity = model.Capacity;
                place.Location = model.Location;
                place.Status = model.Status;
                place.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Lugar actualizado exitosamente", data = place });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Eliminar un lugar
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlace(int id)
        {
            try
            {
                var place = await _context.Places
                    .Include(p => p.Reservations)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (place == null)
                    return NotFound(new { success = false, message = $"Lugar con ID {id} no encontrado" });

                // Verificar si el lugar tiene reservas activas
                var hasActiveReservations = place.Reservations
                    .Any(r => r.Status == ReservationStatus.Confirmed ||
                             r.Status == ReservationStatus.CheckedIn);

                if (hasActiveReservations)
                    return BadRequest(new { success = false, message = "No se puede eliminar un lugar con reservas activas" });

                _context.Places.Remove(place);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Lugar eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtener disponibilidad de un lugar para una fecha específica
        /// </summary>
        [HttpGet("{id}/availability")]
        public async Task<IActionResult> GetPlaceAvailability(int id, [FromQuery] DateTime? date = null)
        {
            try
            {
                var place = await _context.Places.FindAsync(id);
                if (place == null)
                    return NotFound(new { success = false, message = $"Lugar con ID {id} no encontrado" });

                var checkDate = date ?? DateTime.Today;

                var reservationsForDate = await _context.Reservations
                    .Where(r => r.PlaceId == id &&
                               r.StartDate.Date == checkDate.Date &&
                               (r.Status == ReservationStatus.Confirmed ||
                                r.Status == ReservationStatus.CheckedIn))
                    .SumAsync(r => r.NumberOfPeople);

                var availability = new
                {
                    placeId = id,
                    placeName = place.Name,
                    placeCode = place.Code,
                    date = checkDate.ToString("yyyy-MM-dd"),
                    capacity = place.Capacity,
                    reservedSpots = reservationsForDate,
                    availableSpots = place.Capacity.HasValue ? place.Capacity.Value - reservationsForDate : (int?)null,
                    isAvailable = place.Status == PlaceStatus.Available &&
                                 (place.Capacity == null || reservationsForDate < place.Capacity),
                    occupancyPercentage = place.Capacity.HasValue && place.Capacity > 0
                        ? Math.Round((double)reservationsForDate / place.Capacity.Value * 100, 2)
                        : (double?)null,
                    status = place.Status,
                    statusText = GetStatusText(place.Status)
                };

                return Ok(new { success = true, data = availability });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Buscar lugares por código
        /// </summary>
        [HttpGet("search/{code}")]
        public async Task<IActionResult> SearchByCode(string code)
        {
            try
            {
                var place = await _context.Places
                    .Include(p => p.Category)
                    .Include(p => p.Reservations)
                    .FirstOrDefaultAsync(p => p.Code == code);

                if (place == null)
                    return NotFound(new { success = false, message = $"No se encontró lugar con código {code}" });

                var result = new
                {
                    place.Id,
                    place.Code,
                    place.Name,
                    place.Description,
                    place.CategoryId,
                    CategoryName = place.Category.Name,
                    place.Price,
                    place.Capacity,
                    place.Location,
                    place.Status,
                    StatusText = GetStatusText(place.Status),
                    place.CreatedDate,
                    ReservationCount = place.Reservations.Count,
                    ActiveReservations = place.Reservations.Count(r =>
                        r.Status == ReservationStatus.Confirmed ||
                        r.Status == ReservationStatus.CheckedIn)
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cambiar estado de un lugar
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdatePlaceStatus(int id, [FromBody] UpdatePlaceStatusRequest request)
        {
            try
            {
                var place = await _context.Places.FindAsync(id);
                if (place == null)
                    return NotFound(new { success = false, message = $"Lugar con ID {id} no encontrado" });

                place.Status = request.Status;
                place.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Estado del lugar actualizado exitosamente",
                    data = new
                    {
                        place.Id,
                        place.Status,
                        StatusText = GetStatusText(place.Status)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Método auxiliar
        private static string GetStatusText(PlaceStatus status)
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

    // Clase auxiliar para actualizar estado
    public class UpdatePlaceStatusRequest
    {
        public PlaceStatus Status { get; set; }
    }
}