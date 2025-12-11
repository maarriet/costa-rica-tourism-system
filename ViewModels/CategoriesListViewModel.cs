using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class CategoryListViewModel
    {
        // Pagination
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;

        // Pagination helpers
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

        // Filters
        public string SearchTerm { get; set; } = string.Empty;
        public bool? StatusFilter { get; set; }
        public string SortBy { get; set; } = "Name";
        public string SortDirection { get; set; } = "asc";

        // Filter helpers
        public bool HasActiveFilters => !string.IsNullOrEmpty(SearchTerm) || StatusFilter.HasValue;
        public bool HasResults => Categories != null && Categories.Any();

        public string FormattedTotalRevenue => $"${TotalRevenueFromCategories:N2}";
        public string FilterSummary
        {
            get
            {
                var filters = new List<string>();

                if (!string.IsNullOrEmpty(SearchTerm))
                    filters.Add($"Búsqueda: '{SearchTerm}'");

                if (StatusFilter.HasValue)
                    filters.Add($"Estado: {(StatusFilter.Value ? "Activas" : "Inactivas")}");

                return string.Join(", ", filters);
            }
        }

        // Statistics
        public int TotalActiveCategories { get; set; }
        public int TotalInactiveCategories { get; set; }
        public int TotalPlacesInCategories { get; set; }
        public decimal TotalRevenueFromCategories { get; set; }
    }
}