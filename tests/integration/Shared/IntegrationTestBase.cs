using Xunit;

namespace WeatherApi.Tests.Integration.Shared;

[Collection("IntegrationTests")]
public abstract class IntegrationTestBase(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    protected CustomWebApplicationFactory Factory { get; } = factory;

    public async Task InitializeAsync()
    {
        await Factory.ResetStateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
