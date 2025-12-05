// ViewModels/CategoryViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
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