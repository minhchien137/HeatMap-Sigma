using Microsoft.EntityFrameworkCore;

namespace HeatmapSystem.Models
{
    public class ZKBioTimeDbContext : DbContext
    {
        public ZKBioTimeDbContext(DbContextOptions<ZKBioTimeDbContext> options) 
            : base(options)
        {
        }
        
    }
}