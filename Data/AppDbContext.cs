using Microsoft.EntityFrameworkCore;

namespace WeatherService.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<RequestLog> Requests { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RequestLog>(entity =>
            {
                entity.HasIndex(x => x.TimestampUtc);
                entity.HasIndex(x => new { x.City, x.Date });
                entity.Property(x => x.EndPoint).HasMaxLength(32);
                entity.Property(x => x.City).HasMaxLength(256);
                entity.Property(x => x.DisplayCity).HasMaxLength(256);
            });
        }
    }
}
