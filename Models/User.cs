namespace LeeterviewBackend.Models  // 與專案名稱一致，並且表示資料模型類
{
    public class User
    {
        public int Id { get; set; }
        
        public required string Username { get; set; }  
        public required string Password { get; set; } 
        public string? Email { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

}
