using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WeatherApi.Core.Data;
using WeatherApi.Core.Data.Entities;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Core.Services;

public class LocationService(WeatherDbContext dbContext, IOwmClient owmClient, ILogger<LocationService> logger) : ILocationService
{
    private readonly WeatherDbContext _dbContext = dbContext;
    private readonly IOwmClient _owmClient = owmClient;
    private readonly ILogger<LocationService> _logger = logger;

    public async Task<IEnumerable<LocationResponse>> GetAllOrByNameAsync(string? cityName, CancellationToken ct = default)
    {
        _logger.LogInformation("Searching for locations matching: {CityName}", cityName ?? "ALL");
        
        var query = _dbContext.Locations.AsQueryable();
        if (!string.IsNullOrEmpty(cityName))
        {
            query = query.Where(x => x.City.ToLower().Contains(cityName.ToLower()));
        }

        var locations = await query.OrderBy(x => x.City).ToListAsync(ct);
        _logger.LogInformation("Found {Count} locations", locations.Count);

        return locations.Select(ToResponse);
    }

    public async Task<LocationResponse> CreateAsync(LocationDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(dto.City))
        {
            _logger.LogWarning("Attempted to create location with empty city name");
            throw new ArgumentException("City cannot be empty.");
        }

        _logger.LogInformation("Creating location: {City}, {Country}", dto.City, dto.Country);

        var existing = await _dbContext.Locations
            .FirstOrDefaultAsync(x => x.City.ToLower() == dto.City.ToLower(), ct);

        if (existing != null)
        {
            _logger.LogInformation("Location {City} already exists with ID {Id}", dto.City, existing.Id);
            return ToResponse(existing);
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
        return ToResponse(entity);
    }

    public async Task<LocationResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting location by ID: {Id}", id);
        var entity = await _dbContext.Locations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity == null ? null : ToResponse(entity);
    }

    public async Task<LocationResponse> GetByIdOrThrowAsync(int id, CancellationToken ct = default)
    {
        var result = await GetByIdAsync(id, ct);
        return result ?? throw new KeyNotFoundException($"Location with ID {id} not found.");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogWarning("Attempting to delete location by ID: {Id}", id);
        var entity = await _dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity != null)
        {
            // Check if weather data exists for this city
            var hasWeatherData = await _dbContext.DailyWeather.AnyAsync(x => x.City.ToLower() == entity.City.ToLower(), ct)
                || await _dbContext.HourlySummaries.AnyAsync(x => x.City.ToLower() == entity.City.ToLower(), ct);

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

            entity = new LocationEntity
            {
                City = data.Name,
                Country = data.Country,
                Latitude = data.Lat,
                Longitude = data.Lon
            };

            await _dbContext.Locations.AddAsync(entity, ct);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Persisted new geocoded location: {City} (ID: {Id})", data.Name, entity.Id);
        }

        return ToResponse(entity);
    }

    private LocationResponse ToResponse(LocationEntity entity)
        => new(entity.Id, entity.ZipCode, entity.City, entity.Country ?? "N/A", entity.Latitude, entity.Longitude, entity.CreatedAt);
}
