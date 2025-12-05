using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;


namespace Sistema_GuiaLocal_Turismo.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly TourismContext _context;

        public ReservationsController(TourismContext context)
        {
            _context = context;
        }

        // GET: Reservations
        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, ReservationStatus? statusFilter, int page = 1, int pageSize = 10)
        {
            var query = _context.Reservations
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // Apply filters
            if (dateFrom.HasValue)
            {
                query = query.Where(r => r.StartDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(r => r.StartDate <= dateTo.Value);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(r => r.Status == statusFilter.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var reservations = await query
                .OrderByDescending(r => r.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReservationViewModel
                {
                    Id = r.Id,
                    ReservationCode = r.ReservationCode,
                    PlaceId = r.PlaceId,
                    PlaceName = r.Place.Name,
                    CategoryName = r.Place.Category.Name,
                    ClientName = r.ClientName,
                    ClientEmail = r.ClientEmail,
                    ClientPhone = r.ClientPhone,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    NumberOfPeople = r.NumberOfPeople,
                    TotalAmount = r.TotalAmount,
                    Status = r.Status,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    Notes = r.Notes,
                    PlacePrice = r.Place.Price,
                    DaysUntilReservation = (int)(r.StartDate - DateTime.Now).TotalDays
                })
                .ToListAsync();

            var viewModel = new ReservationListViewModel
            {
                Reservations = reservations,
                DateFrom = dateFrom,
                DateTo = dateTo,
                StatusFilter = statusFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        // GET: Reservations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            var viewModel = new ReservationViewModel
            {
                Id = reservation.Id,
                ReservationCode = reservation.ReservationCode,
                PlaceId = reservation.PlaceId,
                PlaceName = reservation.Place.Name,
                CategoryName = reservation.Place.Category.Name,
                ClientName = reservation.ClientName,
                ClientEmail = reservation.ClientEmail,
                ClientPhone = reservation.ClientPhone,
                StartDate = reservation.StartDate,
                EndDate = reservation.EndDate,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                NumberOfPeople = reservation.NumberOfPeople,
                TotalAmount = reservation.TotalAmount,
                Status = reservation.Status,
                CheckInDate = reservation.CheckInDate,
                CheckOutDate = reservation.CheckOutDate,
                Notes = reservation.Notes,
                PlacePrice = reservation.Place.Price,
                DaysUntilReservation = (int)(reservation.StartDate - DateTime.Now).TotalDays
            };

            return View(viewModel);
        }

        // GET: Reservations/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Places = await GetPlacesSelectList();
            return View(new ReservationViewModel { StartDate = DateTime.Today.AddDays(1) });
        }

        // POST: Reservations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Validate dates
                if (viewModel.StartDate < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "La fecha de inicio no puede ser anterior a hoy");
                }

                if (viewModel.EndDate.HasValue && viewModel.EndDate < viewModel.StartDate)
                {
                    ModelState.AddModelError("EndDate", "La fecha de fin no puede ser anterior a la fecha de inicio");
                }

                // Check place availability
                var place = await _context.Places.FindAsync(viewModel.PlaceId);
                if (place == null)
                {
                    ModelState.AddModelError("PlaceId", "Lugar no encontrado");
                }
                else if (place.Status != PlaceStatus.Available)
                {
                    ModelState.AddModelError("PlaceId", "El lugar no está disponible");
                }

                if (ModelState.IsValid)
                {
                    // Generate reservation code
                    var reservationCode = await GenerateReservationCode();

                    // Calculate total amount
                    var totalAmount = CalculateTotalAmount(place.Price, viewModel.NumberOfPeople, viewModel.StartDate, viewModel.EndDate);

                    var reservation = new Reservation
                    {
                        ReservationCode = reservationCode,
                        PlaceId = viewModel.PlaceId,
                        ClientName = viewModel.ClientName,
                        ClientEmail = viewModel.ClientEmail,
                        ClientPhone = viewModel.ClientPhone,
                        StartDate = viewModel.StartDate,
                        EndDate = viewModel.EndDate,
                        StartTime = viewModel.StartTime,
                        EndTime = viewModel.EndTime,
                        NumberOfPeople = viewModel.NumberOfPeople,
                        TotalAmount = totalAmount,
                        Status = ReservationStatus.Pending,
                        Notes = viewModel.Notes,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    _context.Add(reservation);
                    await _context.SaveChangesAsync();

                    // Create reminder alert
                    await CreateReservationAlert(reservation);

                    TempData["SuccessMessage"] = $"Reserva {reservationCode} creada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Places = await GetPlacesSelectList();
            return View(viewModel);
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }

            var viewModel = new ReservationViewModel
            {
                Id = reservation.Id,
                ReservationCode = reservation.ReservationCode,
                PlaceId = reservation.PlaceId,
                ClientName = reservation.ClientName,
                ClientEmail = reservation.ClientEmail,
                ClientPhone = reservation.ClientPhone,
                StartDate = reservation.StartDate,
                EndDate = reservation.EndDate,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                NumberOfPeople = reservation.NumberOfPeople,
                TotalAmount = reservation.TotalAmount,
                Status = reservation.Status,
                Notes = reservation.Notes
            };

            ViewBag.Places = await GetPlacesSelectList();
            return View(viewModel);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReservationViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var reservation = await _context.Reservations.FindAsync(id);
                    if (reservation == null)
                    {
                        return NotFound();
                    }

                    // Validate dates
                    if (viewModel.StartDate < DateTime.Today && reservation.Status == ReservationStatus.Pending)
                    {
                        ModelState.AddModelError("StartDate", "La fecha de inicio no puede ser anterior a hoy");
                    }

                    if (viewModel.EndDate.HasValue && viewModel.EndDate < viewModel.StartDate)
                    {
                        ModelState.AddModelError("EndDate", "La fecha de fin no puede ser anterior a la fecha de inicio");
                    }

                    if (ModelState.IsValid)
                    {
                        // Recalculate total if needed
                        var place = await _context.Places.FindAsync(viewModel.PlaceId);
                        var totalAmount = CalculateTotalAmount(place.Price, viewModel.NumberOfPeople, viewModel.StartDate, viewModel.EndDate);

                        reservation.PlaceId = viewModel.PlaceId;
                        reservation.ClientName = viewModel.ClientName;
                        reservation.ClientEmail = viewModel.ClientEmail;
                        reservation.ClientPhone = viewModel.ClientPhone;
                        reservation.StartDate = viewModel.StartDate;
                        reservation.EndDate = viewModel.EndDate;
                        reservation.StartTime = viewModel.StartTime;
                        reservation.EndTime = viewModel.EndTime;
                        reservation.NumberOfPeople = viewModel.NumberOfPeople;
                        reservation.TotalAmount = totalAmount;
                        reservation.Status = viewModel.Status;
                        reservation.Notes = viewModel.Notes;
                        reservation.UpdatedDate = DateTime.Now;

                        _context.Update(reservation);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Reserva actualizada exitosamente";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Places = await GetPlacesSelectList();
            return View(viewModel);
        }

        // POST: Reservations/CheckIn/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return Json(new { success = false, message = "Reserva no encontrada" });
            }

            if (reservation.Status != ReservationStatus.Confirmed)
            {
                return Json(new { success = false, message = "La reserva debe estar confirmada para hacer Check-In" });
            }

            reservation.Status = ReservationStatus.CheckedIn;
            reservation.CheckInDate = DateTime.Now;
            reservation.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Check-In realizado exitosamente" });
        }

        // POST: Reservations/CheckOut/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return Json(new { success = false, message = "Reserva no encontrada" });
            }

            if (reservation.Status != ReservationStatus.CheckedIn)
            {
                return Json(new { success = false, message = "La reserva debe tener Check-In para hacer Check-Out" });
            }

            reservation.Status = ReservationStatus.CheckedOut;
            reservation.CheckOutDate = DateTime.Now;
            reservation.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Check-Out realizado exitosamente" });
        }

        // POST: Reservations/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return Json(new { success = false, message = "Reserva no encontrada" });
            }

            if (reservation.Status != ReservationStatus.Pending)
            {
                return Json(new { success = false, message = "Solo se pueden confirmar reservas pendientes" });
            }

            reservation.Status = ReservationStatus.Confirmed;
            reservation.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Reserva confirmada exitosamente" });
        }

        // POST: Reservations/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return Json(new { success = false, message = "Reserva no encontrada" });
            }

            if (reservation.Status == ReservationStatus.CheckedIn || reservation.Status == ReservationStatus.CheckedOut)
            {
                return Json(new { success = false, message = "No se puede cancelar una reserva que ya tiene Check-In/Out" });
            }

            reservation.Status = ReservationStatus.Cancelled;
            reservation.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Reserva cancelada exitosamente" });
        }

        // GET: API method to get place price
        [HttpGet]
        public async Task<IActionResult> GetPlacePrice(int placeId)
        {
            var place = await _context.Places.FindAsync(placeId);
            if (place == null)
            {
                return Json(new { success = false });
            }

            return Json(new { success = true, price = place.Price, capacity = place.Capacity });
        }

        private async Task<string> GenerateReservationCode()
        {
            string code;
            do
            {
                code = "RES" + DateTime.Now.ToString("yyyyMMdd") + new Random().Next(1000, 9999);
            }
            while (await _context.Reservations.AnyAsync(r => r.ReservationCode == code));

            return code;
        }

        private decimal CalculateTotalAmount(decimal placePrice, int numberOfPeople, DateTime startDate, DateTime? endDate)
        {
            var days = 1;
            if (endDate.HasValue)
            {
                days = Math.Max(1, (int)(endDate.Value - startDate).TotalDays);
            }

            return placePrice * numberOfPeople * days;
        }

        private async Task CreateReservationAlert(Reservation reservation)
        {
            var alertDate = reservation.StartDate.AddDays(-3);
            if (alertDate > DateTime.Now)
            {
                var alert = new Alert
                {
                    ReservationId = reservation.Id,
                    Type = AlertType.ReservationReminder,
                    Title = "Recordatorio de Reserva",
                    Message = $"Su reserva {reservation.ReservationCode} está programada para el {reservation.StartDate:dd/MM/yyyy}",
                    AlertDate = alertDate,
                    CreatedDate = DateTime.Now
                };

                _context.Alerts.Add(alert);
                await _context.SaveChangesAsync();
            }
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }

        private async Task<SelectList> GetPlacesSelectList()
        {
            var places = await _context.Places
                .Include(p => p.Category)
                .Where(p => p.Status == PlaceStatus.Available)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, DisplayName = $"{p.Name} - {p.Category.Name} (${p.Price})" })
                .ToListAsync();

            return new SelectList(places, "Id", "DisplayName");
        }
    }
}
