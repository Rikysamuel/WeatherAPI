using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WeatherApi.Core.Common;
using WeatherApi.Core.Data;
using WeatherApi.Core.Data.Entities;
using WeatherApi.Core.Interfaces;
using WeatherApi.Core.Models;

namespace WeatherApi.Core.Services;

public class WeatherService(IMemoryCache cache, IOwmClient owmClient, WeatherDbContext dbContext,
                    ILocationService locationService, ILogger<WeatherService> logger) : IWeatherService
{
    private readonly IMemoryCache _cache = cache;
    private readonly IOwmClient _owmClient = owmClient;
    private readonly WeatherDbContext _dbContext = dbContext;
    private readonly ILocationService _locationService = locationService;
    private readonly ILogger<WeatherService> _logger = logger;

    public async Task<WeatherData> GetPersistedCurrentWeatherAsync(int locationId, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdOrThrowAsync(locationId, ct);

        _logger.LogInformation("Getting persisted current weather for {City} (LocationId: {LocationId})", location.City, locationId);
        return await GetOrCreateCacheAsync(locationId, location.City, ct)
            ?? throw new InvalidOperationException("Failed to retrieve weather data.");
    }

    public async Task RefreshCurrentWeatherFromOwmAsync(int locationId, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdOrThrowAsync(locationId, ct);

        _logger.LogInformation("Retrieving weather for {City} (LocationId: {LocationId}) from OWM", location.City, locationId);
        var weatherData = await GetCurrentWeatherDataFromOwmAsync(locationId, location.City, location.Country, location.Latitude, location.Longitude, ct);

        await UpsertDailyObservedAsync(location.City, location.Country, weatherData, ct);
        await UpsertDailyPredictionsAsync(location.City, location.Country, weatherData.DailySummary, ct);
        await RefreshHourlyForecastsAsync(location.City, location.Country, weatherData.HourlySummary, ct);

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Successfully updated weather and alerts for {City}", location.City);
    }

    private async Task UpsertDailyObservedAsync(string city, string country, WeatherData data, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var existing = await _dbContext.DailyWeather
            .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower() && x.Date == today, ct);

        if (existing != null)
        {
            existing.ObservedTemperature = data.Temperature;
            existing.ObservedDescription = data.Description;
            existing.ObservedFeelsLike = data.FeelsLike;
            existing.ObservedHumidity = data.Humidity;
            existing.ObservedPressure = data.Pressure;
            existing.ObservedWindSpeed = data.WindSpeed;
            existing.ObservedWindDirection = data.WindDirection;
            existing.ObservedVisibility = data.Visibility;
            existing.ObservedUVI = data.UVI;
            existing.ObservedSunrise = data.Sunrise;
            existing.ObservedSunset = data.Sunset;
            existing.ObservedNextEventNote = data.NextEventNote;
            existing.ObservedTimestamp = data.Timestamp;
            existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            await _dbContext.DailyWeather.AddAsync(new DailyWeatherEntity
            {
                City = city,
                Country = country,
                Date = today,
                ObservedTemperature = data.Temperature,
                ObservedDescription = data.Description,
                ObservedFeelsLike = data.FeelsLike,
                ObservedHumidity = data.Humidity,
                ObservedPressure = data.Pressure,
                ObservedWindSpeed = data.WindSpeed,
                ObservedWindDirection = data.WindDirection,
                ObservedVisibility = data.Visibility,
                ObservedUVI = data.UVI,
                ObservedSunrise = data.Sunrise,
                ObservedSunset = data.Sunset,
                ObservedNextEventNote = data.NextEventNote,
                ObservedTimestamp = data.Timestamp,
            }, ct);
        }
    }

    private async Task UpsertDailyPredictionsAsync(string city, string country, IEnumerable<DailySummary> dailySummaries, CancellationToken ct)
    {
        foreach (var d in dailySummaries)
        {
            var date = d.Timestamp.Date;

            var existing = _dbContext.ChangeTracker.Entries<DailyWeatherEntity>()
                .Select(e => e.Entity)
                .FirstOrDefault(x => x.City.ToLower() == city.ToLower() && x.Date == date);

            existing ??= await _dbContext.DailyWeather
                .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower() && x.Date == date, ct);

            if (existing != null)
            {
                existing.PredictedTemperature = (d.MinTemperature + d.MaxTemperature) / 2;
                existing.PredictedDescription = d.Description;
                existing.PredictedMinTemperature = d.MinTemperature;
                existing.PredictedMaxTemperature = d.MaxTemperature;
                existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
            else
            {
                await _dbContext.DailyWeather.AddAsync(new DailyWeatherEntity
                {
                    City = city,
                    Country = country,
                    Date = date,
                    PredictedTemperature = (d.MinTemperature + d.MaxTemperature) / 2,
                    PredictedDescription = d.Description,
                    PredictedMinTemperature = d.MinTemperature,
                    PredictedMaxTemperature = d.MaxTemperature,
                }, ct);
            }
        }
    }

    private async Task RefreshHourlyForecastsAsync(string city, string country, IEnumerable<HourlySummary> hourlyData, CancellationToken ct)
    {
        var existing = await _dbContext.HourlySummaries
            .Where(h => h.City.ToLower() == city.ToLower())
            .ToListAsync(ct);

        _dbContext.HourlySummaries.RemoveRange(existing);

        foreach (var h in hourlyData)
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

    public async Task<WeatherData> GetHistoricalAsync(int locationId, DateTime date, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdOrThrowAsync(locationId, ct);

        _logger.LogInformation("Getting historical weather for {City} on {Date} (LocationId: {LocationId})", location.City, date, locationId);

        var cacheKey = $"weather:historical:{locationId}:{date:yyyyMMddHH}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            entry.Size = 1;

            var targetDate = date.Date;
            var persisted = await _dbContext.DailyWeather
                .Where(x => x.City.ToLower() == location.City.ToLower() && x.Date == targetDate)
                .FirstOrDefaultAsync(ct);

            if (persisted?.ObservedTemperature != null)
            {
                return MapToWeatherData(persisted, location.City, location.Country);
            }

            var unixTime = new DateTimeOffset(date).ToUnixTimeSeconds();
            var owmHistorical = await _owmClient.GetHistoricalWeatherAsync(location.Latitude, location.Longitude, unixTime, ct)
                ?? throw new InvalidOperationException("OWM Time Machine API returned no data.");

            var hist = owmHistorical.Data.FirstOrDefault()
                ?? throw new InvalidOperationException("No historical data found.");

            var existing = await _dbContext.DailyWeather
                .FirstOrDefaultAsync(x => x.City.ToLower() == location.City.ToLower() && x.Date == targetDate, ct);

            if (existing != null)
            {
                existing.ObservedTemperature = hist.Temp;
                existing.ObservedDescription = hist.Weather.FirstOrDefault()?.Description ?? "N/A";
                existing.ObservedFeelsLike = hist.FeelsLike;
                existing.ObservedHumidity = hist.Humidity;
                existing.ObservedPressure = hist.Pressure;
                existing.ObservedWindSpeed = hist.WindSpeed;
                existing.ObservedWindDirection = hist.WindDeg;
                existing.ObservedVisibility = hist.Visibility;
                existing.ObservedUVI = hist.Uvi;
                existing.ObservedSunrise = DateTimeOffset.FromUnixTimeSeconds(hist.Sunrise);
                existing.ObservedSunset = DateTimeOffset.FromUnixTimeSeconds(hist.Sunset);
                existing.ObservedTimestamp = DateTimeOffset.FromUnixTimeSeconds(hist.Dt);
                existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
            else
            {
                await _dbContext.DailyWeather.AddAsync(new DailyWeatherEntity
                {
                    City = location.City,
                    Country = location.Country,
                    Date = targetDate,
                    ObservedTemperature = hist.Temp,
                    ObservedDescription = hist.Weather.FirstOrDefault()?.Description ?? "N/A",
                    ObservedFeelsLike = hist.FeelsLike,
                    ObservedHumidity = hist.Humidity,
                    ObservedPressure = hist.Pressure,
                    ObservedWindSpeed = hist.WindSpeed,
                    ObservedWindDirection = hist.WindDeg,
                    ObservedVisibility = hist.Visibility,
                    ObservedUVI = hist.Uvi,
                    ObservedSunrise = DateTimeOffset.FromUnixTimeSeconds(hist.Sunrise),
                    ObservedSunset = DateTimeOffset.FromUnixTimeSeconds(hist.Sunset),
                    ObservedTimestamp = DateTimeOffset.FromUnixTimeSeconds(hist.Dt),
                }, ct);
            }

            await _dbContext.SaveChangesAsync(ct);

            var created = await _dbContext.DailyWeather
                .FirstOrDefaultAsync(x => x.City.ToLower() == location.City.ToLower() && x.Date == targetDate, ct);

            return created != null ? MapToWeatherData(created, location.City, location.Country) : MapToWeatherData(existing ?? new DailyWeatherEntity
            {
                City = location.City,
                Country = location.Country,
                Date = targetDate,
                ObservedTemperature = hist.Temp,
                ObservedDescription = hist.Weather.FirstOrDefault()?.Description ?? "N/A",
                ObservedFeelsLike = hist.FeelsLike,
                ObservedHumidity = hist.Humidity,
                ObservedPressure = hist.Pressure,
                ObservedWindSpeed = hist.WindSpeed,
                ObservedWindDirection = hist.WindDeg,
                ObservedVisibility = hist.Visibility,
                ObservedUVI = hist.Uvi,
                ObservedSunrise = DateTimeOffset.FromUnixTimeSeconds(hist.Sunrise),
                ObservedSunset = DateTimeOffset.FromUnixTimeSeconds(hist.Sunset),
                ObservedTimestamp = DateTimeOffset.FromUnixTimeSeconds(hist.Dt),
            }, location.City, location.Country);
        }) ?? throw new InvalidOperationException("Failed to retrieve historical data.");
    }

    public async Task PruneOldDataAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting scheduled pruning of stale weather data");
        var cutoff = DateTime.UtcNow.Date.AddDays(-30);
        var forecastCutoff = DateTimeOffset.UtcNow;

        var oldDaily = _dbContext.DailyWeather.Where(x => x.Date < cutoff);
        var oldHourly = _dbContext.HourlySummaries.Where(h => h.Timestamp < forecastCutoff);

        _dbContext.DailyWeather.RemoveRange(oldDaily);
        _dbContext.HourlySummaries.RemoveRange(oldHourly);

        var deleted = await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Pruning completed. Removed {Count} stale records.", deleted);
    }

    public async Task<int> DeleteByLocationIdAsync(int locationId, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdOrThrowAsync(locationId, ct);

        _logger.LogWarning("Manually deleting all weather data for city: {City} (LocationId: {LocationId})", location.City, locationId);

        var daily = await _dbContext.DailyWeather
            .Where(x => x.City.ToLower() == location.City.ToLower())
            .ToListAsync(ct);

        var hourly = await _dbContext.HourlySummaries
            .Where(x => x.City.ToLower() == location.City.ToLower())
            .ToListAsync(ct);

        _dbContext.DailyWeather.RemoveRange(daily);
        _dbContext.HourlySummaries.RemoveRange(hourly);

        var count = await _dbContext.SaveChangesAsync(ct);

        _cache.Remove($"weather:current:{locationId}");
        _cache.Remove($"weather:forecast:{locationId}:5");

        _logger.LogInformation("Manually deleted {Count} weather records for {City}", count, location.City);
        return count;
    }

    public async Task<WeatherForecast> GetForecastAsync(int locationId, int days = 5, CancellationToken ct = default)
    {
        var location = await _locationService.GetByIdOrThrowAsync(locationId, ct);

        _logger.LogInformation("Getting {Days}-day forecast for {City} (LocationId: {LocationId})", days, location.City, locationId);

        var cacheKey = $"weather:forecast:{locationId}:{days}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.Size = 1;

            var latestObserved = await _dbContext.DailyWeather
                .Where(x => x.City.ToLower() == location.City.ToLower() && x.Date == DateTime.UtcNow.Date)
                .FirstOrDefaultAsync(ct);

            bool isStale = latestObserved?.ObservedTimestamp == null ||
                (DateTimeOffset.UtcNow - latestObserved.ObservedTimestamp.Value).Duration().TotalMinutes >= 60;

            if (isStale)
            {
                var weatherData = await GetCurrentWeatherDataFromOwmAsync(
                    locationId,
                    location.City,
                    location.Country,
                    location.Latitude,
                    location.Longitude,
                    ct);

                await UpsertDailyObservedAsync(location.City, location.Country, weatherData, ct);
                await UpsertDailyPredictionsAsync(location.City, location.Country, weatherData.DailySummary, ct);
                await RefreshHourlyForecastsAsync(location.City, location.Country, weatherData.HourlySummary, ct);
                await _dbContext.SaveChangesAsync(ct);
            }

            var today = DateTime.UtcNow.Date;
            var daily = await _dbContext.DailyWeather
                .Where(d => d.City.ToLower() == location.City.ToLower() && d.Date >= today)
                .OrderBy(d => d.Date)
                .Take(days)
                .ToListAsync(ct);

            var forecasts = daily.Select(d => new ForecastDayResponse(
                City: location.City,
                Country: d.Country,
                Date: DateOnly.FromDateTime(d.Date),
                Description: d.PredictedDescription ?? "N/A",
                Temperature: d.PredictedTemperature,
                MinTemperature: d.PredictedMinTemperature,
                MaxTemperature: d.PredictedMaxTemperature
            )).ToList();

            return new WeatherForecast(location.City, forecasts);
        }) ?? throw new InvalidOperationException("Failed to retrieve forecast data.");
    }

    private async Task<WeatherData> GetCurrentWeatherDataFromOwmAsync(int locationId, string city, string country, double lat, double lon, CancellationToken ct)
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
            var nextTime = DateTimeUtil.ConvertToSGTime(DateTimeOffset.FromUnixTimeSeconds(nextChange.Dt));
            nextEventNotes = $"{nextChange.Weather.FirstOrDefault()?.Main} expected at {nextTime:HH:mm} SGT";
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
            HourlySummary: [.. oneCall.Hourly.Select(x => new HourlySummary(
                Timestamp: DateTimeOffset.FromUnixTimeSeconds(x.Dt),
                Temperature: x.Temp,
                Description: x.Weather.FirstOrDefault()?.Description ?? "N/A"
            ))],
            DailySummary: [.. oneCall.Daily.Select(x => new DailySummary(
                Timestamp: DateTimeOffset.FromUnixTimeSeconds(x.Dt),
                Description: x.Weather.FirstOrDefault()?.Description ?? "N/A",
                MinTemperature: x.Temp.Min,
                MaxTemperature: x.Temp.Max
            ))]
        );
    }

    private async Task<WeatherData?> GetOrCreateCacheAsync(int locationId, string city, CancellationToken ct = default)
    {
        var cacheKey = $"weather:current:{locationId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.Size = 1;

            var today = DateTime.UtcNow.Date;
            var dailyWeather = await _dbContext.DailyWeather
                .FirstOrDefaultAsync(x => x.City.ToLower() == city.ToLower() && x.Date == today, ct)
                ?? throw new InvalidOperationException("No weather data available.");

            var hourly = await _dbContext.HourlySummaries
                .Where(h => h.City.ToLower() == city.ToLower() && h.Timestamp >= DateTimeOffset.UtcNow)
                .OrderBy(h => h.Timestamp).Take(24).ToListAsync(ct);

            var daily = await _dbContext.DailyWeather
                .Where(d => d.City.ToLower() == city.ToLower() && d.Date >= today)
                .OrderBy(d => d.Date).Take(7).ToListAsync(ct);

            return MapToWeatherData(dailyWeather, city, dailyWeather.Country, hourly, daily);
        });
    }

    private static WeatherData MapToWeatherData(DailyWeatherEntity entity, string city, string country,
        List<HourlySummaryEntity>? hourly = null, List<DailyWeatherEntity>? dailyFromDb = null)
    {
        return new WeatherData(
            City: city,
            Country: country,
            Temperature: entity.ObservedTemperature ?? 0,
            Description: entity.ObservedDescription ?? entity.PredictedDescription ?? "N/A",
            NextEventNote: entity.ObservedNextEventNote ?? string.Empty,
            FeelsLike: entity.ObservedFeelsLike ?? 0,
            Humidity: entity.ObservedHumidity ?? 0,
            Pressure: entity.ObservedPressure ?? 0,
            WindSpeed: entity.ObservedWindSpeed ?? 0,
            WindDirection: entity.ObservedWindDirection ?? 0,
            Visibility: entity.ObservedVisibility ?? 0,
            UVI: entity.ObservedUVI ?? 0,
            Sunrise: entity.ObservedSunrise ?? DateTimeOffset.MinValue,
            Sunset: entity.ObservedSunset ?? DateTimeOffset.MinValue,
            Timestamp: entity.ObservedTimestamp ?? new DateTimeOffset(entity.Date, TimeSpan.Zero),
            HourlySummary: (hourly ?? []).Select(h => new HourlySummary(h.Timestamp, h.Temperature, h.Description)).ToList(),
            DailySummary: [.. (dailyFromDb ?? []).Select(d => new DailySummary(
                new DateTimeOffset(d.Date, TimeSpan.Zero),
                d.PredictedDescription ?? "N/A",
                d.PredictedMinTemperature ?? 0,
                d.PredictedMaxTemperature ?? 0
            ))]
        );
    }
}
