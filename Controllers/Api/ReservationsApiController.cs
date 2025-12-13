// Controllers/Api/ReservationsApiController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsApiController : ControllerBase
    {
        private readonly TourismContext _context;

        public ReservationsApiController(TourismContext context)
        {
            _context = context;
        }

        // GET: api/reservations
        [HttpGet]
        public async Task<IActionResult> GetReservations([FromQuery] int? placeId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var query = _context.Reservations
                    .Include(r => r.Place)
                    .AsQueryable();

                if (placeId.HasValue)
                    query = query.Where(r => r.PlaceId == placeId.Value);

                if (startDate.HasValue)
                    query = query.Where(r => r.StartDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(r => r.StartDate <= endDate.Value);

                var reservations = await query
                    .Select(r => new
                    {
                        id = r.Id,
                        reservationCode = r.ReservationCode,
                        clientName = r.ClientName,
                        clientEmail = r.ClientEmail,
                        place = new
                        {
                            id = r.Place.Id,
                            name = r.Place.Name
                        },
                        startDate = r.StartDate,
                        endDate = r.EndDate,
                        numberOfPeople = r.NumberOfPeople,
                        totalAmount = r.TotalAmount,
                        status = r.Status.ToString(),
                        createdDate = r.CreatedDate
                    })
                    .OrderByDescending(r => r.createdDate)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = reservations.Count,
                    data = reservations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/reservations
        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
        {
            try
            {
                // Validar que el lugar existe y está disponible
                var place = await _context.Places.FindAsync(request.PlaceId);
                if (place == null)
                {
                    return BadRequest(new { success = false, message = "Lugar no encontrado" });
                }

                if (place.Status != PlaceStatus.Available)
                {
                    return BadRequest(new { success = false, message = "Lugar no disponible" });
                }

                // Crear la reserva
                var reservation = new Reservation
                {
                    ReservationCode = GenerateReservationCode(),
                    PlaceId = request.PlaceId,
                    ClientName = request.ClientName,
                    ClientEmail = request.ClientEmail,
                    ClientPhone = request.ClientPhone,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    NumberOfPeople = request.NumberOfPeople,
                    TotalAmount = request.TotalAmount,
                    Status = ReservationStatus.Pending,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, new
                {
                    success = true,
                    message = "Reserva creada exitosamente",
                    data = new
                    {
                        id = reservation.Id,
                        reservationCode = reservation.ReservationCode,
                        status = reservation.Status.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/reservations/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservation(int id)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Place)
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        id = r.Id,
                        reservationCode = r.ReservationCode,
                        clientName = r.ClientName,
                        clientEmail = r.ClientEmail,
                        clientPhone = r.ClientPhone,
                        place = new
                        {
                            id = r.Place.Id,
                            name = r.Place.Name,
                            location = r.Place.Location
                        },
                        startDate = r.StartDate,
                        endDate = r.EndDate,
                        numberOfPeople = r.NumberOfPeople,
                        totalAmount = r.TotalAmount,
                        status = r.Status.ToString(),
                        createdDate = r.CreatedDate
                    })
                    .FirstOrDefaultAsync();

                if (reservation == null)
                {
                    return NotFound(new { success = false, message = "Reserva no encontrada" });
                }

                return Ok(new
                {
                    success = true,
                    data = reservation
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private string GenerateReservationCode()
        {
            return $"RES{DateTime.Now:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
        }
    }

    // Modelo para crear reservas via API
    public class CreateReservationRequest
    {
        public int PlaceId { get; set; }
        public string ClientName { get; set; } = "";
        public string ClientEmail { get; set; } = "";
        public string? ClientPhone { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int NumberOfPeople { get; set; }
        public decimal TotalAmount { get; set; }
    }
}