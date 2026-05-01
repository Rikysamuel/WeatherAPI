using WeatherApi.Core.Models;

namespace WeatherApi.Core.Interfaces;

public interface IWeatherService
{
    Task<WeatherData> GetPersistedCurrentWeatherAsync(int locationId, CancellationToken ct = default);
    Task RefreshCurrentWeatherFromOwmAsync(int locationId, CancellationToken ct = default);
    Task<WeatherForecast> GetForecastAsync(int locationId, int days = 5, CancellationToken ct = default);
    Task<WeatherData> GetHistoricalAsync(int locationId, DateTime date, CancellationToken ct = default);
    Task PruneOldDataAsync(CancellationToken ct = default);
    Task<int> DeleteByLocationIdAsync(int locationId, CancellationToken ct = default);
}

public interface ILocationService
{
    Task<IEnumerable<LocationResponse>> GetAllOrByNameAsync(string? cityName, CancellationToken ct = default);
    Task<LocationResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<LocationResponse> GetByIdOrThrowAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<LocationResponse?> AddOrGetByCityNameAsync(string cityName, CancellationToken ct = default);
}

public interface IAlertService
{
    Task<IReadOnlyList<AlertResponse>> GetAllAsync(CancellationToken ct = default);
    Task<AlertResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    Task<AlertSubscriptionResponse> SubscribeAsync(AlertSubscriptionDto dto, CancellationToken ct = default);
    Task UnsubscribeAsync(int subscriptionId, CancellationToken ct = default);
    Task<IReadOnlyList<AlertSubscriptionResponse>> GetSubscriptionsAsync(string? email = null, CancellationToken ct = default);
}

public interface IExportService
{
    Task<byte[]> ExportAsync(int locationId, int days = 5, CancellationToken ct = default);
}

public interface IOwmClient
{
    Task<OwmGeoResult[]?> GeocodeAsync(string city, CancellationToken ct = default);
    Task<OwmOneCallResponse?> GetOneCallAsync(double lat, double lon, CancellationToken ct = default);
    Task<OwmHistoricalResponse?> GetHistoricalWeatherAsync(double lat, double lon, long dt, CancellationToken ct = default);
}
