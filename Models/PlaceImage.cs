using System.ComponentModel.DataAnnotations;

namespace Sistema_GuiaLocal_Turismo.Models
{
    public class PlaceImage
    {
        public int Id { get; set; }

        [Required]
        public int PlaceId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "URL de la Imagen")]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        [Display(Name = "Texto Alternativo")]
        public string AltText { get; set; }

        [Display(Name = "Es Imagen Principal")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Orden de Visualización")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Fecha de Subida")]
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        // Navigation property
        public virtual Place Place { get; set; }
    }
}