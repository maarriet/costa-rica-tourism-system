using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class PlacesController : Controller
    {
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

        public IActionResult Create()
        {
            var viewModel = new PlaceViewModel();

            // Agregar las categorías de prueba
            ViewBag.Categories = GetTestCategories();

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Create(PlaceViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Simular guardado exitoso
                TempData["Success"] = $"Lugar '{model.Name}' creado exitosamente!";
                return RedirectToAction(nameof(Index));
            }

            // Si hay errores, recargar las categorías
            ViewBag.Categories = GetTestCategories();
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var place = GetTestPlace(id);
            if (place == null)
                return NotFound();

            // Agregar las categorías
            ViewBag.Categories = GetTestCategories();

            return View(place);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, PlaceViewModel model)
        {
            // Verificar que el ID coincida
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Simular actualización exitosa
                    TempData["Success"] = $"Lugar '{model.Name}' actualizado exitosamente!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al actualizar: {ex.Message}";
                }
            }
            else
            {
                // Mostrar errores de validación
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

                foreach (var error in errors)
                {
                    TempData["Error"] += $"Campo {error.Field}: {string.Join(", ", error.Errors)} ";
                }
            }

            // Si hay errores, recargar las categorías y volver a la vista
            ViewBag.Categories = GetTestCategories();
            return View(model);
        }

        // Método helper para obtener categorías de prueba
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