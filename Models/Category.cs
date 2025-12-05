using System.ComponentModel.DataAnnotations;

namespace Sistema_GuiaLocal_Turismo.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Icono")]
        public string Icon { get; set; }

        [StringLength(20)]
        [Display(Name = "Color")]
        public string Color { get; set; }

        [Display(Name = "Activa")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Place> Places { get; set; } = new List<Place>();
    }
}
