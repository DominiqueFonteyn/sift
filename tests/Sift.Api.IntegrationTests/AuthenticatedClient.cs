using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sift.Api.IntegrationTests;

public static class AuthenticatedClient
{
    private const string Password = "correct horse battery";

    public static async Task<HttpClient> CreateSignedInAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync("/auth/register", new { email, password = Password });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());

        return client;
    }

    public static async Task<Guid> GetUserIdAsync(HttpClient client)
    {
        var me = await client.GetFromJsonAsync<JsonElement>("/auth/me");
        return me.GetProperty("id").GetGuid();
    }
}
