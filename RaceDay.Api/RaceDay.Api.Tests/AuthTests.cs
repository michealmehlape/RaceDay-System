using System.Net;
using System.Net.Http.Json;
using Xunit;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class AuthTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public AuthTests() => _client = _factory.NewClient();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_ValidParticipant_Returns201AndHidesPasswordHash()
    {
        var response = await TestHelpers.RegisterAsync(_client, "runner@test.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        await TestHelpers.RegisterAsync(_client, "runner@test.com");

        var response = await TestHelpers.RegisterAsync(_client, "runner@test.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithWrongCode_Returns403()
    {
        var response = await TestHelpers.RegisterAsync(_client, "org@test.com", "Organiser", "wrong-code");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithCorrectCode_Returns201()
    {
        var response = await TestHelpers.RegisterAsync(
            _client, "org@test.com", "Organiser", TestHelpers.OrganiserCode);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email = "short@test.com",
            password = "abc",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200AndStartsSession()
    {
        await TestHelpers.RegisterAsync(_client, "runner@test.com");

        var login = await TestHelpers.LoginAsync(_client, "runner@test.com");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // The session cookie lets the same client reach a protected endpoint
        var profile = await _client.GetAsync("/api/users/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await TestHelpers.RegisterAsync(_client, "runner@test.com");

        var response = await TestHelpers.LoginAsync(_client, "runner@test.com", "WrongPassword1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutLogin_Returns401()
    {
        var response = await _client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}