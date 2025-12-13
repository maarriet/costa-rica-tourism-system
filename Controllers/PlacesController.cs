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
        public IActionResult Index(string searchTerm, int? categoryFilter, PlaceStatus? selectedStatus, int page = 1)
        {
            // Datos de prueba - TEMPORAL
            var testPlaces = new List<PlaceViewModel>
        {
            new PlaceViewModel
            {
                Id = 1,
                Code = "HTL001",
                Name = "Hotel Vista Mar",
                Description = "Hermoso hotel frente al mar",
                CategoryId = 1,
                CategoryName = "Alojamiento",
                Price = 120.00m,
                Capacity = 50,
                CurrentOccupancy = 35,
                Location = "Guanacaste, Tamarindo",
                Status = PlaceStatus.Available,
                Contact = "Tel: 2653-0123",
                CreatedDate = DateTime.Now.AddDays(-30),
                UpdatedDate = DateTime.Now.AddDays(-5),
                ReservationCount = 45,
                ActiveReservations = 8,
                TotalRevenue = 15000m,
                MonthlyRevenue = 3500m,
                OccupancyRate = 70.0
            },
            new PlaceViewModel
            {
                Id = 2,
                Code = "EXP002",
                Name = "Aventura Canopy",
                Description = "Emocionante tour de canopy en el bosque",
                CategoryId = 2,
                CategoryName = "Experiencias",
                Price = 75.00m,
                Capacity = 20,
                CurrentOccupancy = 15,
                Location = "Monteverde, Puntarenas",
                Status = PlaceStatus.Occupied,
                Contact = "Tel: 2645-5678",
                CreatedDate = DateTime.Now.AddDays(-45),
                UpdatedDate = DateTime.Now.AddDays(-2),
                ReservationCount = 89,
                ActiveReservations = 5,
                TotalRevenue = 8900m,
                MonthlyRevenue = 2100m,
                OccupancyRate = 75.0
            },
            new PlaceViewModel
            {
                Id = 3,
                Code = "RST003",
                Name = "Restaurante Típico La Casona",
                Description = "Auténtica comida costarricense",
                CategoryId = 3,
                CategoryName = "Restaurantes",
                Price = 25.00m,
                Capacity = 80,
                CurrentOccupancy = 20,
                Location = "San José, Centro",
                Status = PlaceStatus.Available,
                Contact = "Tel: 2222-3456",
                CreatedDate = DateTime.Now.AddDays(-60),
                UpdatedDate = DateTime.Now.AddDays(-1),
                ReservationCount = 156,
                ActiveReservations = 12,
                TotalRevenue = 12500m,
                MonthlyRevenue = 2800m,
                OccupancyRate = 25.0
            }
        };

            var testCategories = new List<Category>
        {
            new Category { Id = 1, Name = "Alojamiento", Description = "Hoteles y hospedajes" },
            new Category { Id = 2, Name = "Experiencias", Description = "Tours y actividades" },
            new Category { Id = 3, Name = "Restaurantes", Description = "Comida y bebidas" },
            new Category { Id = 4, Name = "Vida Nocturna", Description = "Bares y entretenimiento" }
        };

            // Aplicar filtros
            var filteredPlaces = testPlaces.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                filteredPlaces = filteredPlaces.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

            if (categoryFilter.HasValue)
                filteredPlaces = filteredPlaces.Where(p => p.CategoryId == categoryFilter.Value);

            if (selectedStatus.HasValue)
                filteredPlaces = filteredPlaces.Where(p => p.Status == selectedStatus.Value);

            var places = filteredPlaces.ToList();

            var viewModel = new PlaceListViewModel
            {
                Places = places,
                Categories = testCategories,
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
            //Lineas para debug
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

                foreach (var error in errors)
                {
                    Console.WriteLine($"Campo {error.Field}: {string.Join(", ", error.Errors)}");
                }
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
            // DEBUG: Verificar errores de validación
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();

                TempData["PlaceEditErrors"] = string.Join(" | ", errors);
            }

            TempData["PlaceEditDebug"] = $"ID: {id}, ModelID: {model.Id}, Valid: {ModelState.IsValid}";

            if (id != model.Id)
            {
                return NotFound();
            }

            // Llenar campos automáticamente
            if (model.CategoryId > 0)
            {
                var category = await _context.Categories.FindAsync(model.CategoryId);
                if (category != null)
                {
                    model.CategoryName = category.Name;
                    model.CategoryIcon = category.Icon;
                    model.CategoryColor = category.Color;
                }
            }

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
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlaceExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
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
        public IActionResult Delete(int id)
        {
            var place = GetTestPlace(id);
            if (place == null)
                return NotFound();

            return View(place);
        }

        [HttpPost]
        public IActionResult Delete(PlaceViewModel model)
        {
            TempData["Success"] = $"Lugar '{model.Name}' eliminado exitosamente!";
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