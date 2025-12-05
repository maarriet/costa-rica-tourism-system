// ViewModels/PlaceListViewModel.cs - Complete version with CategoryFilter
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class PlaceListViewModel
    {
        public List<PlaceViewModel> Places { get; set; } = new List<PlaceViewModel>();
        public List<Category> Categories { get; set; } = new List<Category>();

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int TotalItems => TotalCount;
        public int PageSize { get; set; } = 10;

        // Filter properties
        public string SearchTerm { get; set; } = "";
        public int? SelectedCategoryId { get; set; }
        public PlaceStatus? SelectedStatus { get; set; }

        public int? CategoryFilter { get; set; }

        public string StatusFilter { get; set; } = "";
        public string SearchFilter { get; set; } = "";
        public string SortBy { get; set; } = "Name";
        public string SortDirection { get; set; } = "asc";

        // Computed properties
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);
        public bool HasResults => Places.Any();

        // Filter summary
        public string FilterSummary => GetFilterSummary();

        // Selected category name (computed property)
        public string SelectedCategoryName =>
            CategoryFilter.HasValue ?
            Categories.FirstOrDefault(c => c.Id == CategoryFilter.Value)?.Name ?? "Desconocida" :
            "Todas";

        private string GetFilterSummary()
        {
            var filters = new List<string>();

            if (!string.IsNullOrEmpty(SearchTerm))
                filters.Add($"Búsqueda: '{SearchTerm}'");

            if (CategoryFilter.HasValue)
            {
                var category = Categories.FirstOrDefault(c => c.Id == CategoryFilter.Value);
                if (category != null)
                    filters.Add($"Categoría: {category.Name}");
            }

            if (SelectedStatus.HasValue)
                filters.Add($"Estado: {GetStatusText(SelectedStatus.Value)}");

            return filters.Any() ? string.Join(", ", filters) : "Sin filtros";
        }

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

        // Method to clear all filters
        public void ClearFilters()
        {
            SearchTerm = "";
            CategoryFilter = null;
            SelectedCategoryId = null;
            SelectedStatus = null;
            StatusFilter = "";
            SearchFilter = "";
        }

        // Method to check if any filters are applied
        public bool HasActiveFilters =>
            !string.IsNullOrEmpty(SearchTerm) ||
            CategoryFilter.HasValue ||
            SelectedStatus.HasValue;
    }
}