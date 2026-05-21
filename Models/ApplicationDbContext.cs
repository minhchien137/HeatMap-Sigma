using HeatMap_Sigma.Models;
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
        public DbSet<SVN_Projects> SVN_Projects { get; set; }

        public DbSet<SVN_StaffDetail> SVN_StaffDetail { get; set; }

        public DbSet<SVN_Department> SVN_Department { get; set; }

        public DbSet<SVN_ProjectPhase> SVN_ProjectPhase { get; set; }

        public DbSet<SVN_ChatbotFAQ> SVN_ChatbotFAQ { get; set; }

        public DbSet<SM_HMHolidays> SM_HMHolidays { get; set; }
        // authencation
        public DbSet<AuthToken> AuthTokens { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }

        public DbSet<personnel_employee_workconfig> personnel_employee_workconfig { get; set; }


    }
}