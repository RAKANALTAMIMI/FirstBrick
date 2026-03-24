using Microsoft.EntityFrameworkCore;
using AccountService.Models;

namespace AccountService.Data
{
    public class AppDbContext : DbContext
    
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
