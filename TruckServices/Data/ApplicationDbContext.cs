using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TruckServices.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CustomersData> CustomersData { get; set; }
    }
}
