using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_AsOrganiser_Returns201()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.ValidEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidEventType_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelpers.ValidEvent(eventType: "Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutLogin_Returns401()
    {
        var anonymous = _factory.NewClient();

        var response = await anonymous.PostAsJsonAsync("/api/events", TestHelpers.ValidEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_WithoutLogin_Returns200AndFiltersByType()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        await TestHelpers.CreateEventAsync(organiser, TestHelpers.ValidEvent("Run"));
        await TestHelpers.CreateEventAsync(organiser, TestHelpers.ValidEvent("Cycle"));
        var anonymous = _factory.NewClient();

        var response = await anonymous.GetAsync("/api/events?eventType=Cycle");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, events.GetArrayLength());
        Assert.Equal("Cycle", events[0].GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Update_OwnEvent_Returns200AndSavesChange()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var update = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Soweto Fun Run",
            eventDate = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"),
            location = "Orlando Stadium",
            description = "Updated.",
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var fetched = await _factory.NewClient().GetFromJsonAsync<JsonElement>($"/api/events/{eventId}");
        Assert.Equal("Orlando Stadium", fetched.GetProperty("location").GetString());
    }

    [Fact]
    public async Task Update_AnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "org1@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "org2@test.com", "Organiser");

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}", TestHelpers.ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentEvent_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/events/9999", TestHelpers.ValidEvent());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnEvent_Returns200AndEventIsGone()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        var fetched = await _factory.NewClient().GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    [Fact]
    public async Task Delete_EventThatStillHasCategories_Returns409()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.CreateCategoryAsync(organiser, eventId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}