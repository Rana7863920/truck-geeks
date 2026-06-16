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
        public DbSet<Service> Services { get; set; }
        public DbSet<CompanyService> CompanyServices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompanyService>()
                .HasKey(cs => new { cs.CompanyId, cs.ServiceId });

            modelBuilder.Entity<CompanyService>()
                .HasOne(cs => cs.Company)
                .WithMany(c => c.CompanyServices)
                .HasForeignKey(cs => cs.CompanyId);

            modelBuilder.Entity<CompanyService>()
                .HasOne(cs => cs.Service)
                .WithMany(s => s.CompanyServices)
                .HasForeignKey(cs => cs.ServiceId);
        }

    }
}
