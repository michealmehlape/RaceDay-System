using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class RoleTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Participant_CreatingEvent_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await participant.PostAsJsonAsync("/api/events", TestHelpers.ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_DeletingEvent_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await participant.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CreatingCategory_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Sneaky",
            distanceKM = 5,
            maxParticipants = 10,
            entryFee = 0
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewingCategoryEnrolments_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var categoryId = await TestHelpers.CreateCategoryAsync(
            organiser, await TestHelpers.CreateEventAsync(organiser));
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await participant.GetAsync($"/api/categories/{categoryId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_RecordingResult_Returns403()
    {
        var participant = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await participant.PostAsJsonAsync("/api/enrolments/1/results", new
        {
            finishTime = "00:50:00",
            position = 1,
            status = "Finished"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_EnrollingInCategory_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var categoryId = await TestHelpers.CreateCategoryAsync(
            organiser, await TestHelpers.CreateEventAsync(organiser));

        var response = await organiser.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewingParticipantEnrolments_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");

        var response = await organiser.GetAsync("/api/users/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_ViewingMyEnrolments_Returns401()
    {
        var anonymous = _factory.NewClient();

        var response = await anonymous.GetAsync("/api/users/enrolments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}