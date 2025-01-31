using Microsoft.EntityFrameworkCore;
using LeeterviewBackend.Models; 

namespace LeeterviewBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 定義資料表
        public DbSet<User> Users { get; set; }
        public DbSet<ArticleLabel> ArticleLabels { get; set; }
        public DbSet<Article> Articles { get; set; }
    }
}

