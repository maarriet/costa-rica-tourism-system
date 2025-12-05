using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;
using System.ComponentModel.DataAnnotations;


namespace Sistema_GuiaLocal_Turismo.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly TourismContext _context;

        public CategoriesController(TourismContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Places)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Icon = c.Icon,
                    Color = c.Color,
                    IsActive = c.IsActive,
                    PlaceCount = c.Places.Count,
                    ReservationCount = c.Places.SelectMany(p => p.Reservations).Count(),
                    Revenue = c.Places.SelectMany(p => p.Reservations)
                        .Where(r => r.Status == ReservationStatus.Completed)
                        .Sum(r => r.TotalAmount)
                })
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Places)
                .ThenInclude(p => p.Reservations)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Icon = category.Icon,
                Color = category.Color,
                IsActive = category.IsActive,
                PlaceCount = category.Places.Count,
                ReservationCount = category.Places.SelectMany(p => p.Reservations).Count(),
                Revenue = category.Places.SelectMany(p => p.Reservations)
                    .Where(r => r.Status == ReservationStatus.Completed)
                    .Sum(r => r.TotalAmount),
                Places = category.Places.Select(p => new PlaceViewModel
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Price = p.Price,
                    Status = p.Status,
                    ReservationCount = p.Reservations.Count
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View(new CategoryViewModel());
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var category = new Category
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    Icon = viewModel.Icon,
                    Color = viewModel.Color,
                    IsActive = viewModel.IsActive,
                    CreatedDate = DateTime.Now
                };

                _context.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Categoría creada exitosamente";
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Icon = category.Icon,
                Color = category.Color,
                IsActive = category.IsActive
            };

            return View(viewModel);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var category = await _context.Categories.FindAsync(id);
                    if (category == null)
                    {
                        return NotFound();
                    }

                    category.Name = viewModel.Name;
                    category.Description = viewModel.Description;
                    category.Icon = viewModel.Icon;
                    category.Color = viewModel.Color;
                    category.IsActive = viewModel.IsActive;

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Categoría actualizada exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(viewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(viewModel);
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Places)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return Json(new { success = false, message = "Categoría no encontrada" });
            }

            if (category.Places.Any())
            {
                return Json(new { success = false, message = "No se puede eliminar la categoría porque tiene lugares asociados" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Categoría eliminada exitosamente" });
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }

    // CategoryViewModel
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        [Display(Name = "Descripción")]
        public string Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Icono (Font Awesome)")]
        public string Icon { get; set; } = "fas fa-tag";

        [StringLength(20)]
        [Display(Name = "Color (Hex)")]
        public string Color { get; set; } = "#007bff";

        [Display(Name = "Activa")]
        public bool IsActive { get; set; } = true;

        // Statistics
        public int PlaceCount { get; set; }
        public int ReservationCount { get; set; }
        public decimal Revenue { get; set; }

        // Related data
        public List<PlaceViewModel> Places { get; set; } = new List<PlaceViewModel>();
    }
}
