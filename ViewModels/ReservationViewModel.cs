// ViewModels/ReservationViewModel.cs
using Sistema_GuiaLocal_Turismo.Models;

using System.ComponentModel.DataAnnotations;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class ReservationViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Código de Reserva")]
        public string ReservationCode { get; set; }

        [Required]
        [Display(Name = "Lugar")]
        public int PlaceId { get; set; }

        [Required]
        [Display(Name = "Nombre del Cliente")]
        public string ClientName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string ClientEmail { get; set; }

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

        [Display(Name = "Total")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Estado")]
        public ReservationStatus Status { get; set; }

        [Display(Name = "Días faltantes hasta la reservación")]
        public int DaysUntilReservation { get; set; }

        [Display(Name = "Notas")]
        public string Notes { get; set; }

        [Display(Name = "Precio por noche")]
        public decimal PlacePrice { get; set; }

        // Additional properties for display
        public string PlaceName { get; set; }
        public string CategoryName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
    }
}