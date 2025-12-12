using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.Controllers
{
    [Authorize(Roles = "Usuario")]
    public class HomeController : Controller
    {
        private readonly TourismContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(TourismContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var viewModel = new UserDashboardViewModel
            {
                User = user,
                AvailablePlaces = await _context.Places
                    .Where(p => p.Status == PlaceStatus.Available)
                    .Include(p => p.Category)
                    .Take(6)
                    .ToListAsync(),
                UserReservations = await _context.Reservations
                    .Where(r => r.ClientEmail == user.Email) // Usar ClientEmail en lugar de ClientName
                    .Include(r => r.Place)
                    .OrderByDescending(r => r.CreatedDate) // Usar CreatedDate en lugar de CreatedAt
                    .Take(5)
                    .ToListAsync(),
                Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Places(int? categoryId, string search)
        {
            var query = _context.Places
                .Where(p => p.Status == PlaceStatus.Available)
                .Include(p => p.Category)
                .AsQueryable(); // Agregar AsQueryable() para solucionar el error de conversión

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

        public async Task<IActionResult> PlaceDetails(int id)
        {
            var place = await _context.Places
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (place == null)
            {
                return NotFound();
            }

            return View(place);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeReservation(int placeId, DateTime startDate, DateTime endDate, int numberOfPeople)
        {
            var user = await _userManager.GetUserAsync(User);
            var place = await _context.Places.FindAsync(placeId);

            if (place == null || place.Status != PlaceStatus.Available)
            {
                TempData["Error"] = "El lugar no está disponible";
                return RedirectToAction("PlaceDetails", new { id = placeId });
            }

            // Generar código de reserva
            var reservationCode = await GenerateReservationCode();

            // Calcular total
            var days = Math.Max(1, (int)(endDate - startDate).TotalDays);
            var totalAmount = place.Price * numberOfPeople * days;

            var reservation = new Reservation
            {
                ReservationCode = reservationCode,
                PlaceId = placeId,
                ClientName = user.FullName,
                ClientEmail = user.Email,
                StartDate = startDate,
                EndDate = endDate,
                NumberOfPeople = numberOfPeople, // Usar NumberOfPeople en lugar de Guests
                TotalAmount = totalAmount,
                Status = ReservationStatus.Pending, // Cambiar a Pending en lugar de Confirmed
                CreatedDate = DateTime.Now, // Usar CreatedDate en lugar de CreatedAt
                UpdatedDate = DateTime.Now
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "¡Reserva creada exitosamente!";
            return RedirectToAction("MyReservations");
        }

        public async Task<IActionResult> MyReservations()
        {
            var user = await _userManager.GetUserAsync(User);
            var reservations = await _context.Reservations
                .Where(r => r.ClientEmail == user.Email)
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .OrderByDescending(r => r.CreatedDate) // Usar CreatedDate
                .ToListAsync();

            return View(reservations);
        }

        // Método para generar código de reserva
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

        // Método para cancelar reserva (solo el usuario puede cancelar sus propias reservas)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientEmail == user.Email);

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

        // Método para obtener detalles de una reserva específica
        public async Task<IActionResult> ReservationDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var reservation = await _context.Reservations
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientEmail == user.Email);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }
    }
}