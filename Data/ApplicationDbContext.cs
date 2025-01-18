using Microsoft.EntityFrameworkCore;

namespace leeterview_backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 定義資料表
        public DbSet<User> Users { get; set; }
    }

    // 資料表對應的 Model（Entity）
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

