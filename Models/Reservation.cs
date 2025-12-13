using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_GuiaLocal_Turismo.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(15)]
        [Display(Name = "Código de Reserva")]
        public string ReservationCode { get; set; }

        [Required]
        [Display(Name = "Lugar")]
        public int PlaceId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Nombre del Cliente")]
        public string ClientName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Email")]
        public string ClientEmail { get; set; }

        [StringLength(20)]
        [Display(Name = "Teléfono")]
        public string ClientPhone { get; set; }

        [Required]
        [Display(Name = "Fecha de Inicio")]
        public DateTime StartDate { get; set; }

        [Display(Name = "Fecha de Fin")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Hora de Inicio")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "Hora de Fin")]
        public TimeSpan? EndTime { get; set; }

        [Required]
        [Display(Name = "Número de Personas")]
        public int NumberOfPeople { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Total")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Estado")]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [Display(Name = "Check-In")]
        public DateTime? CheckInDate { get; set; }

        [Display(Name = "Check-Out")]
        public DateTime? CheckOutDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Notas")]
        public string Notes { get; set; }

        [Display(Name = "Precio por noche")]
        public decimal PlacePrice {get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Última Actualización")]
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        public bool AlertSent { get; set; } = false;

        // Navigation Properties
        public virtual Place Place { get; set; }
        public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }

    public enum ReservationStatus
    {
        Pending = 1,
        Confirmed = 2,
        CheckedIn = 3,
        CheckedOut = 4,
        Completed = 5,
        Cancelled = 6
    }
}
