using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Sift.Api.IntegrationTests;

// Guards the secure-by-default rule: a new endpoint must not become public by accident.
public sealed partial class AuthorizationCoverageTests(SiftApiFactory factory) : IClassFixture<SiftApiFactory>
{
    private static readonly string[] PublicEndpoints = ["POST /auth/register", "POST /auth/login", "GET /health"];

    [Fact]
    public void Only_register_login_and_health_allow_anonymous_access()
    {
        var anonymous = Endpoints()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .SelectMany(Describe)
            .Order()
            .ToArray();

        Assert.Equal(PublicEndpoints.Order().ToArray(), anonymous);
    }

    [Fact]
    public async Task Every_other_endpoint_rejects_an_anonymous_request_with_a_401_problem()
    {
        using var client = factory.CreateClient();
        var protectedRoutes = Endpoints().SelectMany(Describe).Except(PublicEndpoints).ToArray();

        Assert.NotEmpty(protectedRoutes);

        foreach (var route in protectedRoutes)
        {
            var parts = route.Split(' ', 2);
            var path = RouteParameter().Replace(parts[1], Guid.NewGuid().ToString());

            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(parts[0]), path));

            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"{route} returned {(int)response.StatusCode}.");
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    private RouteEndpoint[] Endpoints()
    {
        // Touch the server so the endpoint data source is populated.
        using var _ = factory.CreateClient();

        return factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToArray();
    }

    private static IEnumerable<string> Describe(RouteEndpoint endpoint)
    {
        var path = "/" + endpoint.RoutePattern.RawText!.Trim('/');
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["GET"];

        return methods.Select(method => $"{method} {path}");
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();
}
