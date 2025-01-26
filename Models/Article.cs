namespace LeeterviewBackend.Models
{
    public class Article
    {
        public int Id { get; set; }
        public required int UserId { get; set; }
        public required string Title { get; set; } 
        public required string Category { get; set; } = "Other";
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } 
        public int Like { get; set; } = 0;
    }
}