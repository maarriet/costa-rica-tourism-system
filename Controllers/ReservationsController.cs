using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.Controllers
{
    [Authorize] // Ambos roles pueden acceder
    public class ReservationsController : Controller
    {
        private readonly TourismContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReservationsController(TourismContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reservations - Diferente vista según rol
        public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, ReservationStatus? statusFilter, int page = 1, int pageSize = 10)
        {
            var query = _context.Reservations
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .AsQueryable();

            // Si es Usuario, solo mostrar sus propias reservas
            if (User.IsInRole("Usuario"))
            {
                var user = await _userManager.GetUserAsync(User);
                query = query.Where(r => r.ClientEmail == user.Email);
            }

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

            // Retornar vista diferente según rol
            if (User.IsInRole("Usuario"))
            {
                return View("UserReservations", viewModel);
            }

            return View(viewModel); // Vista de admin
        }

        // GET: Reservations/Details/5 - Verificar permisos
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

            // Si es Usuario, verificar que sea su reserva
            if (User.IsInRole("Usuario"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (reservation.ClientEmail != user.Email)
                {
                    return Forbid(); // No puede ver reservas de otros
                }
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

        // GET: Reservations/Create - Ambos roles pueden crear
        public async Task<IActionResult> Create()
        {
            ViewBag.Places = await GetPlacesSelectList();

            ViewBag.PlacesData = await _context.Places
                .Include(p => p.Category)
                .Select(p => new {
                     id = p.Id,
                     name = p.Name,
                     price = p.Price,
                     categoryName = p.Category.Name  // Agregar esta línea
                }).ToListAsync();

            var viewModel = new ReservationViewModel { StartDate = DateTime.Today.AddDays(1) };

            // Si es Usuario, prellenar sus datos
            if (User.IsInRole("Usuario"))
            {
                var user = await _userManager.GetUserAsync(User);
                viewModel.ClientName = user.FullName;
                viewModel.ClientEmail = user.Email;
            }

            return View(viewModel);
        }

        // POST: Reservations/Create - Ambos roles pueden crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservationViewModel viewModel)
        {
            // Si es Usuario, forzar sus datos
            if (User.IsInRole("Usuario"))
            {
                var user = await _userManager.GetUserAsync(User);
                viewModel.ClientName = user.FullName;
                viewModel.ClientEmail = user.Email;
            }

            // MOSTRAR ERRORES DE VALIDACIÓN EN TEMPDATA
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();

                TempData["ValidationErrors"] = string.Join(" | ", errors);
            }

            // Validar fechas
            if (viewModel.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("StartDate", "La fecha de inicio no puede ser anterior a hoy");
                TempData["DateError"] = "Fecha de inicio inválida";
            }

            if (viewModel.EndDate.HasValue && viewModel.EndDate < viewModel.StartDate)
            {
                ModelState.AddModelError("EndDate", "La fecha de fin no puede ser anterior a la fecha de inicio");
                TempData["DateError"] = "Fecha de fin inválida";
            }

            // Check place availability
            if (viewModel.PlaceId > 0)
            {
                var place = await _context.Places.FindAsync(viewModel.PlaceId);
                if (place == null)
                {
                    ModelState.AddModelError("PlaceId", "Lugar no encontrado");
                    TempData["PlaceError"] = "Lugar no encontrado";
                }
                else if (place.Status != PlaceStatus.Available)
                {
                    ModelState.AddModelError("PlaceId", "El lugar no está disponible");
                    TempData["PlaceError"] = "Lugar no disponible";
                }
            }
            else
            {
                TempData["PlaceError"] = "Debe seleccionar un lugar";
            }

            if (ModelState.IsValid)
            {
                // tu código de guardado existente...
                TempData["SuccessMessage"] = "¡Reserva creada exitosamente!";
                return RedirectToAction(nameof(Index));
            }

            // Recargar datos
            ViewBag.Places = await GetPlacesSelectList();
            ViewBag.PlacesData = await _context.Places
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price
                }).ToListAsync();

            return View(viewModel);
        }

   

        // Resto de métodos sin cambios...

        // Método corregido para lugares disponibles
        public async Task<IActionResult> AvailablePlaces(int? categoryId, string search)
        {
            var query = _context.Places
                .Where(p => p.Status == PlaceStatus.Available)
                .Include(p => p.Category)
                .AsQueryable(); // Esto soluciona el error de conversión

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            var places = await query.ToListAsync();
            var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.Search = search;

            return View(places);
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

   
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, ReservationViewModel viewModel)
        {
            // Tu código existente de Edit
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CheckIn(int id)
        {
            // Tu código existente de CheckIn
            return Json(new { success = true, message = "Check-In realizado exitosamente" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CheckOut(int id)
        {
            // Tu código existente de CheckOut
            return Json(new { success = true, message = "Check-Out realizado exitosamente" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Confirm(int id)
        {
            // Tu código existente de Confirm
            return Json(new { success = true, message = "Reserva confirmada exitosamente" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            // Tu código existente de Cancel con validación de permisos
            return Json(new { success = true, message = "Reserva cancelada exitosamente" });
        }

        [HttpGet]
        public async Task<IActionResult> GetPlacePrice(int placeId)
        {
            // Tu código existente de GetPlacePrice
            return Json(new { success = true, price = 0, capacity = 0 });
        }
    }
}