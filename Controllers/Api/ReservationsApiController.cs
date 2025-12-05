// Controllers/Api/ReservationsApiController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;

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

        /// <summary>
        /// Obtener todas las reservas con filtros
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetReservations(
            [FromQuery] int? placeId = null,
            [FromQuery] ReservationStatus? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? clientEmail = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Reservations
                    .Include(r => r.Place)
                    .ThenInclude(p => p.Category)
                    .AsQueryable();

                // Aplicar filtros
                if (placeId.HasValue)
                    query = query.Where(r => r.PlaceId == placeId.Value);

                if (status.HasValue)
                    query = query.Where(r => r.Status == status.Value);

                if (fromDate.HasValue)
                    query = query.Where(r => r.StartDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(r => r.StartDate <= toDate.Value);

                if (!string.IsNullOrEmpty(clientEmail))
                    query = query.Where(r => r.ClientEmail.Contains(clientEmail));

                var totalCount = await query.CountAsync();
                var reservations = await query
                    .OrderByDescending(r => r.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        r.ReservationCode,
                        r.PlaceId,
                        PlaceName = r.Place.Name,
                        r.ClientName,
                        r.ClientEmail,
                        r.ClientPhone,
                        r.StartDate,
                        r.EndDate,
                        r.StartTime,
                        r.EndTime,
                        r.NumberOfPeople,
                        r.TotalAmount,
                        r.Status,
                        r.CheckInDate,
                        r.CheckOutDate,
                        r.Notes,
                        r.CreatedDate,
                        r.UpdatedDate,
                        StatusText = GetStatusText(r.Status),
                        DaysUntilReservation = (r.StartDate.Date - DateTime.Today).Days
                    })
                    .ToListAsync();

                var result = new
                {
                    reservations,
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
        /// Obtener una reserva específica por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservation(int id)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Place)
                    .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                    return NotFound(new { success = false, message = $"Reserva con ID {id} no encontrada" });

                var result = new
                {
                    reservation.Id,
                    reservation.ReservationCode,
                    reservation.PlaceId,
                    PlaceName = reservation.Place.Name,
                    CategoryName = reservation.Place.Category.Name,
                    reservation.ClientName,
                    reservation.ClientEmail,
                    reservation.ClientPhone,
                    reservation.StartDate,
                    reservation.EndDate,
                    reservation.StartTime,
                    reservation.EndTime,
                    reservation.NumberOfPeople,
                    reservation.TotalAmount,
                    reservation.Status,
                    StatusText = GetStatusText(reservation.Status),
                    reservation.CheckInDate,
                    reservation.CheckOutDate,
                    reservation.Notes,
                    reservation.CreatedDate,
                    reservation.UpdatedDate
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Crear una nueva reserva
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReservation([FromBody] ReservationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

                // Verificar si el lugar existe y está disponible
                var place = await _context.Places.FindAsync(model.PlaceId);
                if (place == null)
                    return BadRequest(new { success = false, message = "ID de lugar inválido" });

                if (place.Status != PlaceStatus.Available)
                    return BadRequest(new { success = false, message = "El lugar no está disponible para reservas" });

                // Verificar disponibilidad para la fecha
                var existingReservations = await _context.Reservations
                    .Where(r => r.PlaceId == model.PlaceId &&
                               r.StartDate.Date == model.StartDate.Date &&
                               (r.Status == ReservationStatus.Confirmed ||
                                r.Status == ReservationStatus.CheckedIn))
                    .SumAsync(r => r.NumberOfPeople);

                if (existingReservations + model.NumberOfPeople > place.Capacity)
                    return BadRequest(new { success = false, message = "No hay suficiente capacidad disponible para esta fecha" });

                // Generar código de reserva único
                var reservationCode = await GenerateReservationCode();

                var reservation = new Reservation
                {
                    ReservationCode = reservationCode,
                    PlaceId = model.PlaceId,
                    ClientName = model.ClientName,
                    ClientEmail = model.ClientEmail,
                    ClientPhone = model.ClientPhone,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    NumberOfPeople = model.NumberOfPeople,
                    TotalAmount = model.NumberOfPeople * place.Price,
                    Status = ReservationStatus.Confirmed,
                    Notes = model.Notes,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id },
                    new { success = true, message = "Reserva creada exitosamente", data = reservation });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Actualizar el estado de una reserva
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                    return NotFound(new { success = false, message = $"Reserva con ID {id} no encontrada" });

                reservation.Status = request.Status;
                reservation.UpdatedDate = DateTime.Now;

                if (request.Status == ReservationStatus.CheckedIn)
                    reservation.CheckInDate = DateTime.Now;
                else if (request.Status == ReservationStatus.CheckedOut)
                    reservation.CheckOutDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Estado de reserva actualizado exitosamente", data = reservation });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Check-in de una reserva
        /// </summary>
        [HttpPost("{id}/checkin")]
        public async Task<IActionResult> CheckIn(int id)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Place)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                    return NotFound(new { success = false, message = $"Reserva con ID {id} no encontrada" });

                if (reservation.Status != ReservationStatus.Confirmed)
                    return BadRequest(new { success = false, message = "Solo las reservas confirmadas pueden hacer check-in" });

                if (reservation.StartDate.Date != DateTime.Today)
                    return BadRequest(new { success = false, message = "Solo se puede hacer check-in en la fecha de la reserva" });

                // Optional: Check capacity before check-in
                if (reservation.Place.Capacity.HasValue)
                {
                    var currentOccupancy = await _context.Reservations
                        .Where(r => r.PlaceId == reservation.PlaceId && r.Status == ReservationStatus.CheckedIn)
                        .SumAsync(r => r.NumberOfPeople);

                    if (currentOccupancy + reservation.NumberOfPeople > reservation.Place.Capacity.Value)
                    {
                        return BadRequest(new { success = false, message = "No hay suficiente capacidad disponible" });
                    }
                }

                reservation.Status = ReservationStatus.CheckedIn;
                reservation.CheckInDate = DateTime.Now;
                reservation.UpdatedDate = DateTime.Now;

                // REMOVED: Don't update CurrentOccupancy since the property doesn't exist
                // reservation.Place.CurrentOccupancy += reservation.NumberOfPeople;

                await _context.SaveChangesAsync();

                // Calculate current occupancy for response
                var newOccupancy = await _context.Reservations
                    .Where(r => r.PlaceId == reservation.PlaceId && r.Status == ReservationStatus.CheckedIn)
                    .SumAsync(r => r.NumberOfPeople);

                return Ok(new
                {
                    success = true,
                    message = "Check-in exitoso",
                    data = reservation,
                    currentOccupancy = newOccupancy,
                    availableSpaces = reservation.Place.Capacity.HasValue
                        ? Math.Max(0, reservation.Place.Capacity.Value - newOccupancy)
                        : (int?)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        /// <summary>
        /// Check-out de una reserva
        /// </summary>
        [HttpPost("{id}/checkout")]
        public async Task<IActionResult> CheckOut(int id)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Place)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (reservation == null)
                    return NotFound(new { success = false, message = $"Reserva con ID {id} no encontrada" });

                if (reservation.Status != ReservationStatus.CheckedIn)
                    return BadRequest(new { success = false, message = "Solo las reservas con check-in pueden hacer check-out" });

                reservation.Status = ReservationStatus.CheckedOut;
                reservation.CheckOutDate = DateTime.Now;
                reservation.UpdatedDate = DateTime.Now;


                await _context.SaveChangesAsync();

    
                var currentOccupancy = await _context.Reservations
                    .Where(r => r.PlaceId == reservation.PlaceId && r.Status == ReservationStatus.CheckedIn)
                    .SumAsync(r => r.NumberOfPeople);

                return Ok(new
                {
                    success = true,
                    message = "Check-out exitoso",
                    data = reservation,
                    currentOccupancy = currentOccupancy,
                    availableSpaces = reservation.Place.Capacity.HasValue
                        ? Math.Max(0, reservation.Place.Capacity.Value - currentOccupancy)
                        : (int?)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cancelar una reserva
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelReservation(int id, [FromBody] CancelReservationRequest request)
        {
            try
            {
                var reservation = await _context.Reservations.FindAsync(id);
                if (reservation == null)
                    return NotFound(new { success = false, message = $"Reserva con ID {id} no encontrada" });

                if (reservation.Status == ReservationStatus.Cancelled)
                    return BadRequest(new { success = false, message = "La reserva ya está cancelada" });

                if (reservation.Status == ReservationStatus.CheckedIn || reservation.Status == ReservationStatus.CheckedOut)
                    return BadRequest(new { success = false, message = "No se puede cancelar una reserva que ya ha iniciado" });

                reservation.Status = ReservationStatus.Cancelled;
                reservation.Notes = string.IsNullOrEmpty(reservation.Notes)
                    ? $"Cancelada: {request.Reason}"
                    : $"{reservation.Notes}\nCancelada: {request.Reason}";
                reservation.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Reserva cancelada exitosamente", data = reservation });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Buscar reservas por código
        /// </summary>
        [HttpGet("search/{code}")]
        public async Task<IActionResult> SearchByCode(string code)
        {
            try
            {
                var reservation = await _context.Reservations
                    .Include(r => r.Place)
                    .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(r => r.ReservationCode == code);

                if (reservation == null)
                    return NotFound(new { success = false, message = $"No se encontró reserva con código {code}" });

                var result = new
                {
                    reservation.Id,
                    reservation.ReservationCode,
                    reservation.PlaceId,
                    PlaceName = reservation.Place.Name,
                    CategoryName = reservation.Place.Category.Name,
                    reservation.ClientName,
                    reservation.ClientEmail,
                    reservation.ClientPhone,
                    reservation.StartDate,
                    reservation.EndDate,
                    reservation.StartTime,
                    reservation.EndTime,
                    reservation.NumberOfPeople,
                    reservation.TotalAmount,
                    reservation.Status,
                    StatusText = GetStatusText(reservation.Status),
                    reservation.CheckInDate,
                    reservation.CheckOutDate,
                    reservation.Notes,
                    reservation.CreatedDate
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Métodos auxiliares
        private async Task<string> GenerateReservationCode()
        {
            string code;
            bool exists;
            do
            {
                code = $"RES{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";
                exists = await _context.Reservations.AnyAsync(r => r.ReservationCode == code);
            } while (exists);

            return code;
        }

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
    }

    // Clases auxiliares para requests
    public class UpdateStatusRequest
    {
        public ReservationStatus Status { get; set; }
    }

    public class CancelReservationRequest
    {
        public string Reason { get; set; } = "Cancelada por el usuario";
    }
}