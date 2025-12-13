using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;


namespace Sistema_GuiaLocal_Turismo.Controllers
{
    
    
    [Authorize(Roles = "Administrador")]
    public class PlacesController : Controller
    {
        private readonly TourismContext _context;

        public PlacesController(TourismContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string searchTerm, int? categoryFilter, PlaceStatus? selectedStatus, int page = 1)
        {
            // USAR DATOS REALES DE LA BASE DE DATOS
            var query = _context.Places
                .Include(p => p.Category)
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));

            if (categoryFilter.HasValue)
                query = query.Where(p => p.CategoryId == categoryFilter.Value);

            if (selectedStatus.HasValue)
                query = query.Where(p => p.Status == selectedStatus.Value);

            var places = await query
                .Select(p => new PlaceViewModel
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    CategoryIcon = p.Category.Icon,
                    CategoryColor = p.Category.Color,
                    Price = p.Price,
                    Capacity = p.Capacity,
                    Location = p.Location,
                    Status = p.Status,
                    Contact = "", // Si no tienes Contact en el modelo Place
                    CreatedDate = p.CreatedDate,
                    UpdatedDate = p.UpdatedDate,
                    // Valores por defecto para campos calculados
                    ReservationCount = 0,
                    ActiveReservations = 0,
                    CurrentOccupancy = 0,
                    TotalRevenue = 0,
                    MonthlyRevenue = 0,
                    OccupancyRate = 0
                })
                .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            var viewModel = new PlaceListViewModel
            {
                Places = places,
                Categories = categories,
                SearchTerm = searchTerm ?? "",
                CategoryFilter = categoryFilter,
                SelectedStatus = selectedStatus,
                CurrentPage = page,
                PageSize = 10,
                TotalCount = places.Count,
                TotalPages = (int)Math.Ceiling(places.Count / 10.0)
            };

            return View(viewModel);
        }


        public IActionResult Details(int id)
        {
            // Buscar el lugar por ID en los datos de prueba
            var place = GetTestPlace(id);
            if (place == null)
                return NotFound();

            return View(place);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new PlaceViewModel();

            // Usar categorías reales de la base de datos
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlaceViewModel model)
        {
            // DEBUG: Verificar errores de validación
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();

                TempData["PlaceCreateErrors"] = string.Join(" | ", errors);
            }

            TempData["PlaceCreateDebug"] = $"Valid: {ModelState.IsValid}";

            // Llenar campos automáticamente
            if (model.CategoryId > 0)
            {
                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category != null)
                {
                    model.CategoryName = category.Name;
                    model.CategoryIcon = category.Icon ?? "";
                    model.CategoryColor = category.Color ?? "";
                }
            }

            // FORZAR remover errores de campos problemáticos
            if (ModelState.ContainsKey("CategoryIcon"))
            {
                ModelState.Remove("CategoryIcon");
            }
            if (ModelState.ContainsKey("CategoryColor"))
            {
                ModelState.Remove("CategoryColor");
            }
            if (ModelState.ContainsKey("CategoryName"))
            {
                ModelState.Remove("CategoryName");
            }

            if (ModelState.IsValid)
            {
                var place = new Place
                {
                    Name = model.Name,
                    Code = model.Code,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    Price = model.Price,
                    Location = model.Location,
                    Capacity = model.Capacity,
                    Status = model.Status,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                _context.Places.Add(place);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Lugar '{model.Name}' creado exitosamente!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View(model);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var place = await _context.Places
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (place == null)
            {
                return NotFound();
            }

            var viewModel = new PlaceViewModel
            {
                Id = place.Id,
                Code = place.Code,
                Name = place.Name,
                Description = place.Description,
                CategoryId = place.CategoryId,
                CategoryName = place.Category?.Name,
                Price = place.Price,
                Capacity = place.Capacity,
                Location = place.Location,
                Status = place.Status,
                CreatedDate = place.CreatedDate,
                UpdatedDate = place.UpdatedDate
            };

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", place.CategoryId);
            return View(viewModel);
        }

        // POST: Places/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlaceViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // LLENAR CAMPOS AUTOMÁTICAMENTE ANTES DE VALIDAR
            if (model.CategoryId > 0)
            {
                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category != null)
                {
                    model.CategoryName = category.Name;
                    model.CategoryIcon = category.Icon ?? "";
                    model.CategoryColor = category.Color ?? "";
                }
            }

            // Asegurar que los campos no estén vacíos
            model.CategoryIcon = model.CategoryIcon ?? "";
            model.CategoryColor = model.CategoryColor ?? "";

            // FORZAR remover errores de estos campos problemáticos
            if (ModelState.ContainsKey("CategoryIcon"))
            {
                ModelState.Remove("CategoryIcon");
            }
            if (ModelState.ContainsKey("CategoryColor"))
            {
                ModelState.Remove("CategoryColor");
            }
            if (ModelState.ContainsKey("CategoryName"))
            {
                ModelState.Remove("CategoryName");
            }

            // DEBUG: Verificar errores de validación
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();

                TempData["PlaceEditErrors"] = string.Join(" | ", errors);
            }

            TempData["PlaceEditDebug"] = $"ID: {id}, ModelID: {model.Id}, Valid: {ModelState.IsValid}, CategoryIcon: '{model.CategoryIcon}', CategoryColor: '{model.CategoryColor}'";

            if (ModelState.IsValid)
            {
                try
                {
                    var place = await _context.Places.FindAsync(id);
                    if (place == null)
                    {
                        return NotFound();
                    }

                    // Actualizar campos
                    place.Code = model.Code;
                    place.Name = model.Name;
                    place.Description = model.Description;
                    place.CategoryId = model.CategoryId;
                    place.Price = model.Price;
                    place.Capacity = model.Capacity;
                    place.Location = model.Location;
                    place.Status = model.Status;
                    place.UpdatedDate = DateTime.Now;

                    _context.Update(place);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Lugar '{model.Name}' actualizado exitosamente!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al actualizar: {ex.Message}";
                }
            }

            // Si hay errores, recargar las categorías
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        // Método helper
        private bool PlaceExists(int id)
        {
            return _context.Places.Any(e => e.Id == id);
        }
        private List<SelectListItem> GetTestCategories()
        {
            return new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Alojamiento" },
        new SelectListItem { Value = "2", Text = "Experiencias" },
        new SelectListItem { Value = "3", Text = "Restaurantes" },
        new SelectListItem { Value = "4", Text = "Vida Nocturna" },
        new SelectListItem { Value = "5", Text = "Bodas" }
    };
        }
        // GET: Places/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var place = await _context.Places
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (place == null)
            {
                return NotFound();
            }

            var viewModel = new PlaceViewModel
            {
                Id = place.Id,
                Code = place.Code,
                Name = place.Name,
                Description = place.Description,
                CategoryId = place.CategoryId,
                CategoryName = place.Category?.Name,
                CategoryIcon = place.Category?.Icon,
                CategoryColor = place.Category?.Color,
                Price = place.Price,
                Capacity = place.Capacity,
                Location = place.Location,
                Status = place.Status,
                CreatedDate = place.CreatedDate,
                UpdatedDate = place.UpdatedDate,
                // Valores por defecto
                ReservationCount = 0,
                ActiveReservations = 0,
                CurrentOccupancy = 0,
                TotalRevenue = 0,
                MonthlyRevenue = 0,
                OccupancyRate = 0
            };

            return View(viewModel);
        }

        // POST: Places/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var place = await _context.Places.FindAsync(id);
            if (place == null)
            {
                return NotFound();
            }

            // Verificar si tiene reservas asociadas
            var hasReservations = await _context.Reservations.AnyAsync(r => r.PlaceId == id);
            if (hasReservations)
            {
                TempData["Error"] = "No se puede eliminar el lugar porque tiene reservas asociadas.";
                return RedirectToAction(nameof(Index));
            }

            _context.Places.Remove(place);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Lugar '{place.Name}' eliminado exitosamente!";
            return RedirectToAction(nameof(Index));
        }

        private PlaceViewModel GetTestPlace(int id)
        {
            var testPlaces = new Dictionary<int, PlaceViewModel>
            {
                [1] = new PlaceViewModel
                {
                    Id = 1,
                    Code = "HTL001",
                    Name = "Hotel Vista Mar",
                    Description = "Hermoso hotel frente al mar con todas las comodidades",
                    CategoryId = 1,
                    CategoryName = "Alojamiento",
                    Price = 120.00m,
                    Capacity = 50,
                    CurrentOccupancy = 35,
                    Location = "Guanacaste, Tamarindo",
                    Status = PlaceStatus.Available,
                    Contact = "Tel: 2653-0123, Email: info@hotelvistamar.cr",
                    CreatedDate = DateTime.Now.AddDays(-30),
                    UpdatedDate = DateTime.Now.AddDays(-5),
                    ReservationCount = 45,
                    ActiveReservations = 8,
                    TotalRevenue = 15000m,
                    MonthlyRevenue = 3500m,
                    OccupancyRate = 70.0
                },
                [2] = new PlaceViewModel
                {
                    Id = 2,
                    Code = "EXP002",
                    Name = "Aventura Canopy",
                    Description = "Emocionante tour de canopy en el bosque nuboso",
                    CategoryId = 2,
                    CategoryName = "Experiencias",
                    Price = 75.00m,
                    Capacity = 20,
                    CurrentOccupancy = 15,
                    Location = "Monteverde, Puntarenas",
                    Status = PlaceStatus.Occupied,
                    Contact = "Tel: 2645-5678, WhatsApp: 8888-9999",
                    CreatedDate = DateTime.Now.AddDays(-45),
                    UpdatedDate = DateTime.Now.AddDays(-2),
                    ReservationCount = 89,
                    ActiveReservations = 5,
                    TotalRevenue = 8900m,
                    MonthlyRevenue = 2100m,
                    OccupancyRate = 75.0
                },
                [3] = new PlaceViewModel
                {
                    Id = 3,
                    Code = "RST003",
                    Name = "Restaurante Típico La Casona",
                    Description = "Auténtica comida costarricense en ambiente tradicional",
                    CategoryId = 3,
                    CategoryName = "Restaurantes",
                    Price = 25.00m,
                    Capacity = 80,
                    CurrentOccupancy = 20,
                    Location = "San José, Centro",
                    Status = PlaceStatus.Available,
                    Contact = "Tel: 2222-3456, Email: lacasona@email.cr",
                    CreatedDate = DateTime.Now.AddDays(-60),
                    UpdatedDate = DateTime.Now.AddDays(-1),
                    ReservationCount = 156,
                    ActiveReservations = 12,
                    TotalRevenue = 12500m,
                    MonthlyRevenue = 2800m,
                    OccupancyRate = 25.0
                }
            };

            return testPlaces.ContainsKey(id) ? testPlaces[id] : null;
        }
    }
}