using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class EnrolmentTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // An organiser, plus the ID of a category they created with the given limit
    private async Task<(HttpClient organiser, int categoryId)> SetUpCategoryAsync(int maxParticipants = 100)
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId, maxParticipants);
        return (organiser, categoryId);
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201WithBibNumber()
    {
        var (_, categoryId) = await SetUpCategoryAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("BIB-", enrolment.GetProperty("bibNumber").GetString());
        Assert.Equal("Registered", enrolment.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Enrol_IsRecordedAgainstTheParticipantAndCategory()
    {
        var (_, categoryId) = await SetUpCategoryAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");
        await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/users/enrolments");

        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(categoryId, mine[0].GetProperty("categoryID").GetInt32());
        Assert.Equal("Soweto Fun Run", mine[0].GetProperty("eventName").GetString());
    }

    [Fact]
    public async Task Enrol_TwiceInSameCategory_Returns409()
    {
        var (_, categoryId) = await SetUpCategoryAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");
        await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns409()
    {
        var (_, categoryId) = await SetUpCategoryAsync(maxParticipants: 1);
        var first = await TestHelpers.LoggedInClientAsync(_factory, "runner1@test.com");
        var second = await TestHelpers.LoggedInClientAsync(_factory, "runner2@test.com");
        await first.PostAsync($"/api/categories/{categoryId}/enrol", null);

        var response = await second.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InPastEvent_Returns400()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser, TestHelpers.ValidEvent(daysFromNow: -10));
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InNonExistentCategory_Returns404()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await participant.PostAsync("/api/categories/9999/enrol", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_Returns401()
    {
        var (_, categoryId) = await SetUpCategoryAsync();

        var response = await _factory.NewClient().PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnCategory()
    {
        var (organiser, categoryId) = await SetUpCategoryAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");
        await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        var enrolments = await organiser.GetFromJsonAsync<JsonElement>($"/api/categories/{categoryId}/enrolments");

        Assert.Equal(1, enrolments.GetArrayLength());
    }

    [Fact]
    public async Task OtherOrganiser_ViewingEnrolments_Returns403()
    {
        var (_, categoryId) = await SetUpCategoryAsync();
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "org2@test.com", "Organiser");

        var response = await otherOrganiser.GetAsync($"/api/categories/{categoryId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnEnrolment_Returns200()
    {
        var (_, categoryId) = await SetUpCategoryAsync();
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");
        var enrol = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        var enrolmentId = (await enrol.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("enrolmentID").GetInt32();

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/users/enrolments");
        Assert.Equal(0, mine.GetArrayLength());
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_Returns403()
    {
        var (_, categoryId) = await SetUpCategoryAsync();
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "runner1@test.com");
        var stranger = await TestHelpers.LoggedInClientAsync(_factory, "runner2@test.com");
        var enrol = await owner.PostAsync($"/api/categories/{categoryId}/enrol", null);
        var enrolmentId = (await enrol.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("enrolmentID").GetInt32();

        var response = await stranger.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}