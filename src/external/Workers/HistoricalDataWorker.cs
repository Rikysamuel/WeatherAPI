using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeatherApi.Core.Interfaces;

namespace WeatherApi.External.Workers;

public class HistoricalDataWorker(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<HistoricalDataWorker> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<HistoricalDataWorker> _logger = logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(configuration.GetValue<int>("Worker:RefreshIntervalMinutes", 60));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HistoricalDataWorker starting at {Time}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();
                var locationService = scope.ServiceProvider.GetRequiredService<ILocationService>();

                var locations = await locationService.GetAllOrByNameAsync(null, stoppingToken);
                foreach(var location in locations)
                {
                    await weatherService.RefreshCurrentWeatherFromOwmAsync(
                        location.Id,
                        stoppingToken);
                }

                // Periodically prune old data
                await weatherService.PruneOldDataAsync(stoppingToken);

                _logger.LogDebug("Historical data refresh and pruning cycle completed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during historical data refresh at {Time}", DateTime.UtcNow);
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("HistoricalDataWorker stopping at {Time}", DateTime.UtcNow);
    }
}
