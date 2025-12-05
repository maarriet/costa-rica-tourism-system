using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Sistema_GuiaLocal_Turismo.Models
{
    public class Place
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Código Único")]
        public string Code { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [StringLength(1000)]
        [Display(Name = "Descripción")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Categoría")]
        public int CategoryId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Precio")]
        public decimal Price { get; set; }

        [Display(Name = "Capacidad")]
        public int? Capacity { get; set; }

        [StringLength(300)]
        [Display(Name = "Ubicación")]
        public string Location { get; set; }

        [Display(Name = "Estado")]
        public PlaceStatus Status { get; set; } = PlaceStatus.Available;

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Última Actualización")]
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Category Category { get; set; }
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public virtual ICollection<PlaceImage> Images { get; set; } = new List<PlaceImage>();
        public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }

    public enum PlaceStatus
    {
        Available = 1,
        Occupied = 2,
        Maintenance = 3,
        Inactive = 4
    }
}
