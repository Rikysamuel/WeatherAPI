using WeatherApi.Core.Data;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;
using WeatherApi.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WeatherApi.Core.Services;

public class LocationService : ILocationService
{
    private readonly WeatherDbContext _dbContext;
    private readonly IOwmClient _owmClient;
    private readonly ILogger<LocationService> _logger;

    public LocationService() {}

    public LocationService(WeatherDbContext dbContext, IOwmClient owmClient, ILogger<LocationService> logger)
    {
        _dbContext = dbContext;
        _owmClient = owmClient;
        _logger = logger;
    }

    public async Task<IEnumerable<LocationResponse>> FindByNameAsync(string cityName, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for locations matching: {CityName}", cityName ?? "ALL");
        
        var query = _dbContext.Locations.AsQueryable();
        if (!string.IsNullOrEmpty(cityName))
        {
            query = query.Where(x => x.City.ToLower().Contains(cityName.ToLower()));
        }

        var locations = await query.OrderBy(x => x.City).ToListAsync(ct);
        _logger.LogInformation("Found {Count} locations", locations.Count);

        return locations.Select(GetLocationReponse);
    }

    public async Task<LocationResponse> CreateAsync(LocationDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(dto.City))
        {
            _logger.LogWarning("Attempted to create location with empty city name");
            throw new ArgumentException("City cannot be empty.", nameof(dto.City));
        }

        _logger.LogInformation("Creating location: {City}, {Country}", dto.City, dto.Country);

        var existing = await _dbContext.Locations
            .FirstOrDefaultAsync(x => x.City.ToLower() == dto.City.ToLower(), ct);

        if (existing != null)
        {
            _logger.LogInformation("Location {City} already exists with ID {Id}", dto.City, existing.Id);
            return GetLocationReponse(existing);
        }

        var entity = new LocationEntity
        {
            ZipCode = dto.ZipCode,
            City = dto.City,
            Country = dto.Country ?? "N/A",
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        await _dbContext.Locations.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Successfully created location {City} with ID {Id}", dto.City, entity.Id);
        return GetLocationReponse(entity);
    }

    public async Task<LocationResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting location by ID: {Id}", id);
        var entity = await _dbContext.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity == null ? null : GetLocationReponse(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogWarning("Attempting to delete location by ID: {Id}", id);
        var entity = await _dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity != null)
        {
            // Check if weather data exists for this city
            var hasWeatherData = await _dbContext.WeatherData.AnyAsync(x => x.City.ToLower() == entity.City.ToLower(), ct)
                || await _dbContext.HourlySummaries.AnyAsync(x => x.City.ToLower() == entity.City.ToLower(), ct)
                || await _dbContext.DailySummaries.AnyAsync(x => x.City.ToLower() == entity.City.ToLower(), ct);

            if (hasWeatherData)
            {
                _logger.LogWarning("Deletion blocked: Location {City} has associated weather data.", entity.City);
                throw new InvalidOperationException($"Cannot delete location '{entity.City}' because it still has associated weather data. Please delete the weather data first.");
            }

            _dbContext.Locations.Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully deleted location {City} (ID: {Id})", entity.City, id);
        }
        else
        {
            _logger.LogInformation("Location ID {Id} not found for deletion", id);
        }
    }

    public async Task DeleteByCityNameAsync(string cityName, CancellationToken ct = default)
    {
        _logger.LogWarning("Deleting location by name: {CityName}", cityName);
        var entity = await _dbContext.Locations.FirstOrDefaultAsync(x => x.City.ToLower() == cityName.ToLower(), ct);

        if (entity != null)
        {
            _dbContext.Locations.Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully deleted location {City} (ID: {Id})", cityName, entity.Id);
        }
        else
        {
            _logger.LogInformation("Location {City} not found for deletion", cityName);
        }
    }

    public async Task<LocationResponse?> AddOrGetByCityNameAsync(string cityName, CancellationToken ct = default)
    {
        _logger.LogInformation("AddOrGetByCityName for: {CityName}", cityName);
        
        var entity = await _dbContext.Locations
            .FirstOrDefaultAsync(x => x.City.ToLower() == cityName.ToLower(), ct);

        if (entity == null)
        {
            _logger.LogInformation("City {CityName} not in DB. Geocoding via OWM...", cityName);
            var results = await _owmClient.GeocodeAsync(cityName, ct);
            var data = results?.FirstOrDefault();
            
            if (data == null)
            {
                _logger.LogError("Geocoding failed for city: {CityName}", cityName);
                throw new KeyNotFoundException($"City '{cityName}' not found.");
            }

            entity = await PopulateEntity(data, ct);
        }

        return GetLocationReponse(entity);
    }


    private async Task<LocationEntity> PopulateEntity(OwmGeoResult data, CancellationToken ct = default, string? zipCode = null)
    {
        var entity = new LocationEntity
        {
            ZipCode = zipCode,
            City = data.Name,
            Country = data.Country,
            Latitude = data.Lat,
            Longitude = data.Lon
        };

        await _dbContext.Locations.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);
        
        _logger.LogInformation("Persisted new geocoded location: {City} (ID: {Id})", data.Name, entity.Id);
        return entity;
    }

    private LocationResponse GetLocationReponse(LocationEntity entity)
    {
        return new LocationResponse(
            Id: entity.Id,
            ZipCode: entity.ZipCode,
            City: entity.City,
            Country: entity.Country ?? "N/A",
            Latitude: entity.Latitude,
            Longitude: entity.Longitude,
            CreatedAt: entity.CreatedAt
        );
    }
}
