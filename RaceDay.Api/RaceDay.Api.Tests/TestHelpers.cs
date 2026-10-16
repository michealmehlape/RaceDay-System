using System.Net.Http.Json;
using System.Text.Json;

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

    // A brand-new client that has registered and logged in with the given role
    public static async Task<HttpClient> LoggedInClientAsync(
        ApiFactory factory, string email, string role = "Participant")
    {
        var client = factory.NewClient();
        await RegisterAsync(client, email, role, role == "Organiser" ? OrganiserCode : null);
        await LoginAsync(client, email);
        return client;
    }

    public static object ValidEvent(string eventType = "Run", int daysFromNow = 30) => new
    {
        eventName = "Soweto Fun Run",
        eventDate = DateTime.Today.AddDays(daysFromNow).ToString("yyyy-MM-dd"),
        location = "Soweto, Gauteng",
        description = "Community fun run.",
        eventType
    };

    public static async Task<int> CreateEventAsync(HttpClient organiser, object? body = null)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", body ?? ValidEvent());
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("eventID").GetInt32();
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10km Fun Run",
            distanceKM = 10,
            maxParticipants,
            entryFee = 150
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("categoryID").GetInt32();
    }
}