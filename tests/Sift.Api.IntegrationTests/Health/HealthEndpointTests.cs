using System.Net;
using System.Net.Http.Json;

namespace Sift.Api.IntegrationTests.Health;

public sealed class HealthEndpointTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    [Fact]
    public async Task Reports_healthy_against_a_real_sqlite_database()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponseDto>();

        Assert.Equal("healthy", body?.Status);
    }

    private sealed record HealthResponseDto(string Status);
}
