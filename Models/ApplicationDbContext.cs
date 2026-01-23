using Microsoft.EntityFrameworkCore;

namespace HeatmapSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
              : base(options)
        {
        }

        public DbSet<SVN_User> SVN_User { get; set; }
        public DbSet<SVN_Logs> SVN_Logs { get; set; }
        

    }
}