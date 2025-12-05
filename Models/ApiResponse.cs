// Models/ApiResponse.cs
namespace CostaRicaTourism.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public object Errors { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(T data, string message = "Success")
        {
            Success = true;
            Message = message;
            Data = data;
        }

        public ApiResponse(string message, object errors = null)
        {
            Success = false;
            Message = message;
            Errors = errors;
        }
    }

    // For responses without data
    public class ApiResponse : ApiResponse<object>
    {
        public ApiResponse(string message) : base(message)
        {
        }

        public ApiResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}