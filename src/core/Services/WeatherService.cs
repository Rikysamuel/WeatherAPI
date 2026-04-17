using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WeatherApi.Core.Data;
using WeatherApi.Core.Data.Entities;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Core.Services;

public class WeatherService : IWeatherService
{
    private readonly IMemoryCache _cache;
    private readonly IOwmClient _owmClient;
    private readonly WeatherDbContext _dbContext;
    private readonly ILocationService _locationService;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(IMemoryCache cache, IOwmClient owmClient, WeatherDbContext dbContext, ILocationService locationService, ILogger<WeatherService> logger)
    {
        _cache = cache;
        _owmClient = owmClient;
        _dbContext = dbContext;
        _locationService = locationService;
        _logger = logger;
    }

    public async Task<WeatherData> GetPersistedCurrentWeatherAsync(int locationId, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdAsync(locationId, ct)
            ?? throw new KeyNotFoundException($"Location with ID {locationId} not found.");

        _logger.LogInformation("Getting persisted current weather for {City} (LocationId: {LocationId})", location.City, locationId);
        return await GetOrCreateCacheAsync(locationId, location.City, ct)
            ?? throw new InvalidOperationException("Failed to retrieve weather data.");
    }

    public async Task RetrieveCurrentWeatherToOwmAsync(int locationId, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdAsync(locationId, ct)
            ?? throw new KeyNotFoundException($"Location with ID {locationId} not found.");

        _logger.LogInformation("Retrieving weather for {City} (LocationId: {LocationId}) from OWM if stale", location.City, locationId);
        var latestSavedData = await GetLatestWeatherFromDatabaseAsync(location.City, ct);
        
        if (latestSavedData == null || (DateTimeOffset.UtcNow - latestSavedData.Timestamp).Duration().TotalMinutes >= 10)
        {
            _logger.LogDebug("Weather data for {City} is stale or missing. Fetching fresh data.", location.City);
            var weatherData = await GetCurrentWeatherDataFromOwmAsync(locationId, location.City, location.Country, location.Latitude, location.Longitude, ct);

            var weatherEntity = ConvertToEntity(weatherData);
            await _dbContext.WeatherData.AddAsync(weatherEntity, ct);

            await UpsertForecastsAsync(location.City, location.Country, weatherData, ct);

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully updated weather and alerts for {City}", location.City);
        }
    }

    private async Task PersistOwmAlertsAsync(int locationId, OwmAlert[] alerts, CancellationToken ct)
    {
        foreach (var owmAlert in alerts)
        {
            var message = $"{owmAlert.Event}: {owmAlert.Description}";
            var createdAt = DateTimeOffset.FromUnixTimeSeconds(owmAlert.Start);

            var exists = await _dbContext.Alerts.AnyAsync(a =>
                a.LocationId == locationId &&
                a.Message == message &&
                a.CreatedAt == createdAt, ct);

            if (!exists)
            {
                _logger.LogWarning("New OWM alert for {CityId}: {Event}", locationId, owmAlert.Event);
                var entity = new AlertEntity
                {
                    LocationId = locationId,
                    Message = message,
                    Severity = AlertSeverity.High,
                    CreatedAt = createdAt,
                    IsActive = true
                };
                await _dbContext.Alerts.AddAsync(entity, ct);

                // ── MOCK EMAIL DISPATCHING ──
                // Find all subscribers for this location
                var subscribers = await _dbContext.AlertSubscriptions
                    .Where(s => s.LocationId == locationId)
                    .ToListAsync(ct);

                foreach (var sub in subscribers)
                {
                    _logger.LogInformation("[MOCK EMAIL SENT] To: {Email} | Subject: WEATHER ALERT for {City} | Body: {Message}",
                        sub.Email, locationId, entity.Message);
                }
            }
        }
    }

    private async Task UpsertForecastsAsync(string city, string country, WeatherData data, CancellationToken ct)
    {
        _logger.LogDebug("Upserting forecasts for {City}", city);
        foreach (var h in data.HourlySummary)
        {
            var existing = await _dbContext.HourlySummaries
                .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower() && x.Timestamp == h.Timestamp, ct);
            
            if (existing != null) 
            { 
                existing.Temperature = h.Temperature; 
                existing.Description = h.Description; 
            }
            else 
            { 
                await _dbContext.HourlySummaries.AddAsync(new HourlySummaryEntity 
                { 
                    City = city, 
                    Country = country, 
                    Timestamp = h.Timestamp, 
                    Temperature = h.Temperature, 
                    Description = h.Description 
                }, ct); 
            }
        }

        foreach (var d in data.DailySummary)
        {
            var existing = await _dbContext.DailySummaries
                .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower() && x.Timestamp == d.Timestamp, ct);
            
            if (existing != null) 
            { 
                existing.MinTemperature = d.MinTemperature; 
                existing.MaxTemperature = d.MaxTemperature; 
                existing.Description = d.Description; 
            }
            else 
            { 
                await _dbContext.DailySummaries.AddAsync(new DailySummaryEntity 
                { 
                    City = city, 
                    Country = country, 
                    Timestamp = d.Timestamp, 
                    MinTemperature = d.MinTemperature, 
                    MaxTemperature = d.MaxTemperature, 
                    Description = d.Description 
                }, ct); 
            }
        }
    }

    public async Task<WeatherData> GetHistoricalAsync(int locationId, DateTime date, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdAsync(locationId, ct)
            ?? throw new KeyNotFoundException($"Location with ID {locationId} not found.");

        _logger.LogInformation("Getting historical weather for {City} on {Date} (LocationId: {LocationId})", location.City, date, locationId);
        
        var cacheKey = $"weather:historical:{locationId}:{date:yyyyMMddHH}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            entry.Size = 1;

            var targetTime = new DateTimeOffset(date);
            var startTime = targetTime.AddMinutes(-30);
            var endTime = targetTime.AddMinutes(30);
            
            var candidates = await _dbContext.WeatherData
                .Where(w => w.City.ToLower() == location.City.ToLower())
                .Where(w => w.Timestamp >= startTime && w.Timestamp <= endTime)
                .ToListAsync(ct);

            var persisted = candidates
                .OrderBy(w => Math.Abs((w.Timestamp - targetTime).Ticks))
                .FirstOrDefault();

            if (persisted != null)
            {
                return ConvertToModel(persisted, new List<HourlySummaryEntity>(), new List<DailySummaryEntity>());
            }

            var unixTime = targetTime.ToUnixTimeSeconds();
            var owmHistorical = await _owmClient.GetHistoricalWeatherAsync(location.Latitude, location.Longitude, unixTime, ct)
                ?? throw new InvalidOperationException("OWM Time Machine API returned no data.");

            var hist = owmHistorical.Data.FirstOrDefault() 
                ?? throw new InvalidOperationException("No historical data found.");

            var newEntity = new WeatherDataEntity
            {
                City = location.City,
                Country = location.Country,
                Temperature = hist.Temp,
                Description = hist.Weather.FirstOrDefault()?.Description ?? "N/A",
                FeelsLike = hist.FeelsLike,
                Humidity = hist.Humidity,
                Pressure = hist.Pressure,
                WindSpeed = hist.WindSpeed,
                WindDirection = hist.WindDeg,
                Visibility = hist.Visibility,
                UVI = hist.Uvi,
                Sunrise = DateTimeOffset.FromUnixTimeSeconds(hist.Sunrise),
                Sunset = DateTimeOffset.FromUnixTimeSeconds(hist.Sunset),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(hist.Dt)
            };

            await _dbContext.WeatherData.AddAsync(newEntity, ct);
            await _dbContext.SaveChangesAsync(ct);

            return ConvertToModel(newEntity, new List<HourlySummaryEntity>(), new List<DailySummaryEntity>());
        }) ?? throw new InvalidOperationException("Failed to retrieve historical data.");
    }

    public async Task PruneOldDataAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting scheduled pruning of stale weather data");
        var snapshotCutoff = DateTimeOffset.UtcNow.AddDays(-30);
        var forecastCutoff = DateTimeOffset.UtcNow;

        var oldSnapshots = _dbContext.WeatherData.Where(w => w.Timestamp < snapshotCutoff);
        var oldHourly = _dbContext.HourlySummaries.Where(h => h.Timestamp < forecastCutoff);
        var oldDaily = _dbContext.DailySummaries.Where(d => d.Timestamp < forecastCutoff);
        
        _dbContext.WeatherData.RemoveRange(oldSnapshots);
        _dbContext.HourlySummaries.RemoveRange(oldHourly);
        _dbContext.DailySummaries.RemoveRange(oldDaily);

        var deleted = await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Pruning completed. Removed {Count} stale records.", deleted);
    }

    public async Task<int> DeleteByLocationIdAsync(int locationId, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdAsync(locationId, ct)
            ?? throw new KeyNotFoundException($"Location with ID {locationId} not found.");

        _logger.LogWarning("Manually deleting all weather data for city: {City} (LocationId: {LocationId})", location.City, locationId);
        
        var snapshots = await _dbContext.WeatherData.Where(x => x.City.ToLower() == location.City.ToLower()).ToListAsync(ct);
        var hourly = await _dbContext.HourlySummaries.Where(x => x.City.ToLower() == location.City.ToLower()).ToListAsync(ct);
        var daily = await _dbContext.DailySummaries.Where(x => x.City.ToLower() == location.City.ToLower()).ToListAsync(ct);

        _dbContext.WeatherData.RemoveRange(snapshots);
        _dbContext.HourlySummaries.RemoveRange(hourly);
        _dbContext.DailySummaries.RemoveRange(daily);

        var count = await _dbContext.SaveChangesAsync(ct);
        
        // Invalidate cache
        _cache.Remove($"weather:current:{locationId}");
        _cache.Remove($"weather:forecast:{locationId}:5"); 
        
        _logger.LogInformation("Manually deleted {Count} weather records for {City}", count, location.City);
        return count;
    }

    public async Task<WeatherForecast> GetForecastAsync(int locationId, int days = 5, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdAsync(locationId, ct)
            ?? throw new KeyNotFoundException($"Location with ID {locationId} not found.");

        _logger.LogInformation("Getting {Days}-day forecast for {City} (LocationId: {LocationId})", days, location.City, locationId);
        
        var cacheKey = $"weather:forecast:{locationId}:{days}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.Size = 1;

            var latest = await GetLatestWeatherFromDatabaseAsync(location.City, ct);
            bool isStale = latest == null || (DateTimeOffset.UtcNow - latest.Timestamp).Duration().TotalMinutes >= 10;

            if (isStale)
            {
                var weatherData = await GetCurrentWeatherDataFromOwmAsync(
                    locationId,
                    location.City,
                    location.Country,
                    location.Latitude,
                    location.Longitude,
                    ct);

                var weatherEntity = ConvertToEntity(weatherData);
                await _dbContext.WeatherData.AddAsync(weatherEntity, ct);
                await UpsertForecastsAsync(location.City, location.Country, weatherData, ct);
                await _dbContext.SaveChangesAsync(ct);
            }

            var daily = await _dbContext.DailySummaries
                .Where(d => d.City.ToLower() == location.City.ToLower() && d.Timestamp >= DateTimeOffset.UtcNow)
                .OrderBy(d => d.Timestamp)
                .Take(days)
                .ToListAsync(ct);

            var forecasts = daily.Select(d => new WeatherData(
                City: location.City,
                Country: d.Country,
                Temperature: (d.MinTemperature + d.MaxTemperature) / 2,
                Description: d.Description,
                NextEventNote: string.Empty,
                FeelsLike: 0, Humidity: 0, Pressure: 0, WindSpeed: 0, WindDirection: 0, Visibility: 0, UVI: 0,
                Sunrise: DateTimeOffset.MinValue, Sunset: DateTimeOffset.MinValue,
                Timestamp: d.Timestamp,
                HourlySummary: new List<HourlySummary>(),
                DailySummary: new List<DailySummary>()
            )).ToList();

            return new WeatherForecast(location.City, forecasts);
        }) ?? throw new InvalidOperationException("Failed to retrieve forecast data.");
    }

    public async Task<WeatherDataEntity?> GetLatestWeatherFromDatabaseAsync(string city, CancellationToken ct)
    {
        return await _dbContext.WeatherData
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower(), ct);
    }

    public async Task<WeatherData> GetCurrentWeatherDataFromOwmAsync(int locationId, string city, string country, double lat, double lon, CancellationToken ct)
    {
        var oneCall = await _owmClient.GetOneCallAsync(lat, lon, ct)
                ?? throw new InvalidOperationException("OWM One Call API returned no data.");

        var current = oneCall.Current;
        var mainWeather = current.Weather.FirstOrDefault();
        
        string nextEventNotes = string.Empty;
        var nextChange = oneCall.Hourly
            .Where(x => !(x.Weather.FirstOrDefault()?.Main ?? "N/A").Equals(mainWeather?.Main, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Dt)
            .FirstOrDefault();

        if (nextChange != null)
        {
            var nextTime = DateTimeOffset.FromUnixTimeSeconds(nextChange.Dt).LocalDateTime;
            nextEventNotes = $"{nextChange.Weather.FirstOrDefault()?.Main} expected at {nextTime:HH:mm}";
        }

        if (oneCall.Alerts != null && oneCall.Alerts.Length != 0)
        {
            await PersistOwmAlertsAsync(locationId, oneCall.Alerts, ct);
        }

        return new WeatherData(
            City: city,
            Country: country,
            Temperature: current.Temp,
            Description: mainWeather?.Description ?? "N/A",
            NextEventNote: nextEventNotes,
            FeelsLike: current.FeelsLike,
            Humidity: current.Humidity,
            Pressure: current.Pressure,
            WindSpeed: current.WindSpeed,
            WindDirection: current.WindDeg,
            Visibility: current.Visibility,
            UVI: current.Uvi,
            Sunrise: DateTimeOffset.FromUnixTimeSeconds(current.Sunrise),
            Sunset: DateTimeOffset.FromUnixTimeSeconds(current.Sunset),
            Timestamp: DateTimeOffset.FromUnixTimeSeconds(current.Dt),
            HourlySummary: oneCall.Hourly.Select(x => new HourlySummary(
                Timestamp: DateTimeOffset.FromUnixTimeSeconds(x.Dt),
                Temperature: x.Temp,
                Description: x.Weather.FirstOrDefault()?.Description ?? "N/A"
            )).ToList(),
            DailySummary: oneCall.Daily.Select(x => new DailySummary(
                Timestamp: DateTimeOffset.FromUnixTimeSeconds(x.Dt),
                Description: x.Weather.FirstOrDefault()?.Description ?? "N/A",
                MinTemperature: x.Temp.Min,
                MaxTemperature: x.Temp.Max
            )).ToList()
        );
    }

    private async Task<WeatherData?> GetOrCreateCacheAsync(int locationId, string city, CancellationToken ct = default)
    {
        var cacheKey = $"weather:current:{locationId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.Size = 1;
            
            var weatherDataEntity = await GetLatestWeatherFromDatabaseAsync(city, ct)
                ?? throw new InvalidOperationException("No weather data available.");

            var hourly = await _dbContext.HourlySummaries
                .Where(h => h.City.ToLower() == city.ToLower() && h.Timestamp >= DateTimeOffset.UtcNow)
                .OrderBy(h => h.Timestamp).Take(24).ToListAsync(ct);
            
            var daily = await _dbContext.DailySummaries
                .Where(d => d.City.ToLower() == city.ToLower() && d.Timestamp >= DateTimeOffset.UtcNow)
                .OrderBy(d => d.Timestamp).Take(7).ToListAsync(ct);

            return ConvertToModel(weatherDataEntity, hourly, daily);
        });
    }

    private WeatherData ConvertToModel(WeatherDataEntity entity, List<HourlySummaryEntity> hourly, List<DailySummaryEntity> daily)
    {
        return new WeatherData(
            City: entity.City,
            Country: entity.Country,
            Temperature: entity.Temperature,
            Description: entity.Description,
            NextEventNote: entity.NextEventNote,
            FeelsLike: entity.FeelsLike,
            Humidity: entity.Humidity,
            Pressure: entity.Pressure,
            WindSpeed: entity.WindSpeed,
            WindDirection: entity.WindDirection,
            Visibility: entity.Visibility,
            UVI: entity.UVI,
            Sunrise: entity.Sunrise,
            Sunset: entity.Sunset,
            Timestamp: entity.Timestamp,
            HourlySummary: hourly.Select(h => new HourlySummary(h.Timestamp, h.Temperature, h.Description)).ToList(),
            DailySummary: daily.Select(d => new DailySummary(d.Timestamp, d.Description, d.MinTemperature, d.MaxTemperature)).ToList()
        );
    }

    private WeatherDataEntity ConvertToEntity(WeatherData model)
    {
        return new WeatherDataEntity
        {
            City = model.City,
            Country = model.Country,
            Temperature = model.Temperature,
            Description = model.Description,
            NextEventNote = model.NextEventNote,
            FeelsLike = model.FeelsLike,
            Humidity = model.Humidity,
            Pressure = model.Pressure,
            WindSpeed = model.WindSpeed,
            WindDirection = model.WindDirection,
            Visibility = model.Visibility,
            UVI = model.UVI,
            Sunrise = model.Sunrise,
            Sunset = model.Sunset,
            Timestamp = model.Timestamp
        };
    }
}
