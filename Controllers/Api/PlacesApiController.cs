// Controllers/Api/PlacesApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class PlacesApiController : ControllerBase
    {
        private readonly TourismContext _context;

        public PlacesApiController(TourismContext context)
        {
            _context = context;
        }

        // GET: api/places
        [HttpGet]
        public async Task<IActionResult> GetPlaces()
        {
            try
            {
                var places = await _context.Places
                    .Include(p => p.Category)
                    .Select(p => new
                    {
                        id = p.Id,
                        code = p.Code,
                        name = p.Name,
                        description = p.Description,
                        price = p.Price,
                        capacity = p.Capacity,
                        location = p.Location,
                        status = p.Status.ToString(),
                        available = p.Status == PlaceStatus.Available,
                        category = new
                        {
                            id = p.Category.Id,
                            name = p.Category.Name
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = places.Count,
                    data = places
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        // GET: api/places/available
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailablePlaces()
        {
            try
            {
                var places = await _context.Places
                    .Include(p => p.Category)
                    .Where(p => p.Status == PlaceStatus.Available)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        price = p.Price,
                        capacity = p.Capacity,
                        location = p.Location,
                        category = p.Category.Name
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = places.Count,
                    data = places
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/places/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlace(int id)
        {
            try
            {
                var place = await _context.Places
                    .Include(p => p.Category)
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        id = p.Id,
                        code = p.Code,
                        name = p.Name,
                        description = p.Description,
                        price = p.Price,
                        capacity = p.Capacity,
                        location = p.Location,
                        status = p.Status.ToString(),
                        available = p.Status == PlaceStatus.Available,
                        category = new
                        {
                            id = p.Category.Id,
                            name = p.Category.Name
                        }
                    })
                    .FirstOrDefaultAsync();

                if (place == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Lugar no encontrado"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = place
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/places/{id}/availability
        [HttpGet("{id}/availability")]
        public async Task<IActionResult> GetPlaceAvailability(int id, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var place = await _context.Places.FindAsync(id);
                if (place == null)
                {
                    return NotFound(new { success = false, message = "Lugar no encontrado" });
                }

                // Consultar reservas existentes en el rango de fechas
                var query = _context.Reservations.Where(r => r.PlaceId == id);

                if (startDate.HasValue)
                    query = query.Where(r => r.StartDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(r => r.StartDate <= endDate.Value);

                var reservations = await query
                    .Select(r => new
                    {
                        startDate = r.StartDate,
                        endDate = r.EndDate,
                        guests = r.NumberOfPeople,
                        status = r.Status.ToString()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    placeId = id,
                    placeName = place.Name,
                    capacity = place.Capacity,
                    available = place.Status == PlaceStatus.Available,
                    reservations = reservations,
                    queryPeriod = new
                    {
                        startDate = startDate?.ToString("yyyy-MM-dd"),
                        endDate = endDate?.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}