namespace LeeterviewBackend.DTOs
{
    public class ApiResponse<T>
    {
        public required int Status { get; set; } // HTTP 状态码，例如 200, 400, 500 等
        public required string Message { get; set; } // 成功或失败的消息
        public T? Data { get; set; } // 实际返回的数据
        public object? Error { get; set; } // 错误详细信息（如果有）
    }
}
