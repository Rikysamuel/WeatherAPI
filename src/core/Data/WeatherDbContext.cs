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
    public DbSet<DailyWeatherEntity> DailyWeather { get; set; }
    public DbSet<HourlySummaryEntity> HourlySummaries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("weather");

        modelBuilder.Entity<AlertEntity>(entity =>
        {
            entity.HasIndex(e => new { e.LocationId, e.CreatedAt, e.Message }).IsUnique();
            entity.HasOne(e => e.Location).WithMany().HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Severity).HasConversion<int>();
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<AlertSubscriptionEntity>(entity =>
        {
            entity.HasIndex(e => new { e.LocationId, e.Email }).IsUnique();
            entity.HasOne(e => e.Location).WithMany().HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocationEntity>(entity =>
        {
            entity.HasIndex(e => e.City).IsUnique();
        });

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<DailyWeatherEntity>(entity =>
        {
            entity.HasIndex(e => new { e.LocationId, e.Date }).IsUnique();
            entity.HasOne(e => e.Location).WithMany().HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.HasIndex(e => e.ObservedTimestamp);
        });

        modelBuilder.Entity<HourlySummaryEntity>(entity =>
        {
            entity.HasIndex(e => new { e.LocationId, e.Timestamp }).IsUnique();
            entity.HasOne(e => e.Location).WithMany().HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Restrict);
        });

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
