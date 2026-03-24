using Microsoft.EntityFrameworkCore;
using InvestmentService.Models;

namespace InvestmentService.Data
{
    public class AppDbContext : DbContext
    
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Investment> Investments { get; set; }
    }
}
