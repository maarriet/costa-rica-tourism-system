// ViewModels/PlaceViewModel.cs 
using System.ComponentModel.DataAnnotations;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class PlaceViewModel
    {
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
        [Display(Name = "Precio")]
        public decimal Price { get; set; }

        [Display(Name = "Capacidad")]
        public int? Capacity { get; set; }

        [StringLength(300)]
        [Display(Name = "Ubicación")]
        public string Location { get; set; }

        [Display(Name = "Estado")]
        public PlaceStatus Status { get; set; }

        [Display(Name = "Contacto")]
        public String Contact { get; set; }

        // Additional properties for display
        public string CategoryName { get; set; }
        public string CategoryIcon { get; set; }
        public string CategoryColor { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int ReservationCount { get; set; }
        public int ActiveReservations { get; set; }


        public int CurrentOccupancy { get; set; } = 0;

        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double? OccupancyRate { get; set; }

  
        public int? AvailableSpaces => Capacity.HasValue && CurrentOccupancy >= 0
            ? Math.Max(0, Capacity.Value - CurrentOccupancy)
            : null;

        public bool IsAvailable => Status == PlaceStatus.Available;
        public bool HasCapacity => Capacity.HasValue && Capacity > 0;
        public string StatusText => GetStatusText(Status);

        private static string GetStatusText(PlaceStatus status)
        {
            return status switch
            {
                PlaceStatus.Available => "Disponible",
                PlaceStatus.Occupied => "Ocupado",
                PlaceStatus.Maintenance => "Mantenimiento",
                PlaceStatus.Inactive => "Inactivo",
                _ => "Desconocido"
            };
        }
    }
}