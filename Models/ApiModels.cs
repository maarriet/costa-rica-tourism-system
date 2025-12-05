namespace sistema_GuiaLocal_Turismo.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
        public Dictionary<string, string[]> ValidationErrors { get; set; }
        public PaginationInfo Pagination { get; set; }
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class ApiError
    {
        public string Message { get; set; }
        public string Details { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}