namespace LeeterviewBackend.Models
{
    public class User
    {
        public int Id { get; set; }

        public required string Username { get; set; }

        public required string Password { get; set; }

        public string? Email { get; set; }

        public string AvatarUrl { get; set; } = "/images/customer.webp";

        public int TotalPosts { get; set; } = 0; // 預設為 0

        public int TotalLikes { get; set; } = 0; // 預設為 0

        public string Bio { get; set; } = "This is your bio. Click edit to update.";

        public string Location { get; set; } = "Unknown";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
