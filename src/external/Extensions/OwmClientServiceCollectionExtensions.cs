using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherApi.Core.Interfaces;
using WeatherApi.External.Owm;

namespace WeatherApi.External.Extensions;

public static class OwmClientServiceCollectionExtensions
{
    public static IServiceCollection AddOwmClient(this IServiceCollection services, IConfiguration configuration)
    {
        var owmSection = configuration.GetSection("OpenWeatherMap");

        services.Configure<OwmOptions>(owmSection);

        var options = owmSection.Get<OwmOptions>() ?? new OwmOptions();

        services.AddHttpClient<IOwmClient, OwmClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
        .AddStandardResilienceHandler();

        return services;
    }
}
