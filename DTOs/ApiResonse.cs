namespace LeeterviewBackend.DTOs
{
    public class ApiResponse<T>
    {
        public required int Status { get; set; } 
        public required string Message { get; set; } 
        public T? Data { get; set; } 
        public object? Error { get; set; } 
    }
}
