using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace StockMind.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.ClearProviders())).CreateClient();

    [Fact]
    public async Task Health_RespondeOk()
    {
        using var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task BusinessEndpoint_RequiresAuthentication()
    {
        using var response = await _client.GetAsync("/api/productos");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
