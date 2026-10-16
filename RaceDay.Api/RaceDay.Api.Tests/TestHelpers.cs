using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public static class TestHelpers
{
    public const string OrganiserCode = "TEST-ORGANISER-CODE";
    public const string Password = "Password123";

    public static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client, string email, string role = "Participant", string? organiserCode = null) =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = Password,
            role,
            organiserCode
        });

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });
}