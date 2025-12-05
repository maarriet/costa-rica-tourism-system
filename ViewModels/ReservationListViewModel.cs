using Sistema_GuiaLocal_Turismo.Models;
using System.ComponentModel.DataAnnotations;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class ReservationListViewModel
    {
        // Collection of reservations to display
        public IEnumerable<ReservationViewModel> Reservations { get; set; } = new List<ReservationViewModel>();

        // Filter properties
        [Display(Name = "Fecha Desde")]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [Display(Name = "Fecha Hasta")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        [Display(Name = "Estado")]
        public ReservationStatus? StatusFilter { get; set; }

        // Pagination properties
        [Display(Name = "Página Actual")]
        public int CurrentPage { get; set; } = 1;

        [Display(Name = "Total de Páginas")]
        public int TotalPages { get; set; }

        [Display(Name = "Total de Elementos")]
        public int TotalItems { get; set; }

        [Display(Name = "Elementos por Página")]
        public int PageSize { get; set; } = 10;

        // Helper properties for pagination
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int PreviousPage => CurrentPage - 1;
        public int NextPage => CurrentPage + 1;

     
        public IEnumerable<int> GetPageNumbers()
        {
            var startPage = Math.Max(1, CurrentPage - 2);
            var endPage = Math.Min(TotalPages, CurrentPage + 2);

            return Enumerable.Range(startPage, endPage - startPage + 1);
        }


        public string ItemsDisplayRange
        {
            get
            {
                if (TotalItems == 0) return "0 elementos";

                var start = (CurrentPage - 1) * PageSize + 1;
                var end = Math.Min(CurrentPage * PageSize, TotalItems);
                return $"Mostrando {start}-{end} de {TotalItems} elementos";
            }
        }
    }
}