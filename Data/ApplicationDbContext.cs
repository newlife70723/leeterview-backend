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

        // user table
        public DbSet<User> Users { get; set; }

        // article label
        public DbSet<ArticleLabel> ArticleLabels { get; set; }

        // article
        public DbSet<Article> Articles { get; set; }
    }
}

