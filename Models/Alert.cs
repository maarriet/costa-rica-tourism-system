using System.ComponentModel.DataAnnotations;

namespace Sistema_GuiaLocal_Turismo.Models
{
    public class Alert
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Reserva")]
        public int ReservationId { get; set; }

        [Required]
        [Display(Name = "Tipo de Alerta")]
        public AlertType Type { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Título")]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        [Display(Name = "Mensaje")]
        public string Message { get; set; }

        [Display(Name = "Fecha de Alerta")]
        public DateTime AlertDate { get; set; }

        [Display(Name = "Enviada")]
        public bool IsSent { get; set; } = false;

        [Display(Name = "Fecha de Envío")]
        public DateTime? SentDate { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Reservation Reservation { get; set; }
    }

    public enum AlertType
    {
        ReservationReminder = 1,
        CheckInReminder = 2,
        CheckOutReminder = 3,
        PaymentReminder = 4,
        CancellationNotice = 5
    }
}
