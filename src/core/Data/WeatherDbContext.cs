using Microsoft.EntityFrameworkCore;
using WeatherApi.Core.Data.Entities;

namespace WeatherApi.Core.Data;

public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

    public DbSet<LocationEntity> Locations => Set<LocationEntity>();
    public DbSet<AlertEntity> Alerts => Set<AlertEntity>();
    public DbSet<AlertSubscriptionEntity> AlertSubscriptions => Set<AlertSubscriptionEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<WeatherDataEntity> WeatherData { get; set; }
    public DbSet<HourlySummaryEntity> HourlySummaries { get; set; }
    public DbSet<DailySummaryEntity> DailySummaries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
        })
        .HasDefaultSchema("weather");

        modelBuilder.Entity<LocationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.City).IsUnique();
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
        })
        .HasDefaultSchema("weather");

        modelBuilder.Entity<AlertEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(a => a.Location)
                .WithMany()
                .HasForeignKey(a => a.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Message).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Severity).HasConversion<int>();
            entity.HasIndex(e => new { e.LocationId, e.CreatedAt, e.Message }).IsUnique();
        })
        .HasDefaultSchema("weather");

        modelBuilder.Entity<AlertSubscriptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => new { e.LocationId, e.Email }).IsUnique();
        })
        .HasDefaultSchema("weather");

        modelBuilder.Entity<WeatherDataEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.City, e.Timestamp }).IsUnique(); 
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.NextEventNote).HasMaxLength(500);
        })
        .HasDefaultSchema("weather");

        modelBuilder.Entity<HourlySummaryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.City, e.Timestamp }).IsUnique(); 
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(100).IsRequired();
        })
        .HasDefaultSchema("weather");

        modelBuilder.Entity<DailySummaryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.City, e.Timestamp }).IsUnique(); 
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Country).HasMaxLength(100).IsRequired();
        })
        .HasDefaultSchema("weather");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTimeOffset)
                         || p.ClrType == typeof(DateTimeOffset?));

            foreach (var property in properties)
            {
                property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTimeOffset>(
                    v => v.ToUniversalTime(), 
                    v => v 
                ));
            }
        }
    }
}
