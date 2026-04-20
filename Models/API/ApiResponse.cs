namespace Manage_KPI_or_OKR_System.Models.API
{
    public class ApiResponse<T>
    {
        public bool Success => StatusCode >= 200 && StatusCode < 300;
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public object? Errors { get; set; }

        public ApiResponse() { }

        public ApiResponse(int statusCode, string message, T? data = default, object? errors = null)
        {
            StatusCode = statusCode;
            Message = message;
            Data = data;
            Errors = errors;
        }

        public static ApiResponse<T> OkResult(T data, string message = "Success")
        {
            return new ApiResponse<T>(200, message, data);
        }

        public static ApiResponse<T> ErrorResult(int statusCode, string message, object? errors = null)
        {
            return new ApiResponse<T>(statusCode, message, default, errors);
        }
    }
}
