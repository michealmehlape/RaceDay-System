using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Services;
using Xunit;

namespace RaceDay.Api.Tests;

// A pretend upload service: it never calls Azure, it just returns a made-up address
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        return Task.FromResult("https://fake.blob.test/" + folder + "/" + file.FileName);
    }
}

// The normal test factory, but with the pretend upload service swapped in
public class FakeBlobApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBlobStorageService>();
            services.AddScoped<IBlobStorageService, FakeBlobStorageService>();
        });
    }
}

public class UploadTests : IDisposable
{
    private readonly FakeBlobApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    // Builds the form a browser sends when a file is chosen (the field must be called "File")
    private static MultipartFormDataContent ImageForm(string contentType, string fileName)
    {
        var form = new MultipartFormDataContent();

        var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        form.Add(file, "File", fileName);
        return form;
    }

    // ---------- Profile picture ----------

    [Fact]
    public async Task UploadProfilePicture_WithoutLogin_Returns401()
    {
        var anonymous = _factory.NewClient();

        var response = await anonymous.PostAsync("/api/users/profile/picture", ImageForm("image/png", "me.png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_WrongFileType_Returns400()
    {
        var runner = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await runner.PostAsync("/api/users/profile/picture", ImageForm("text/plain", "notes.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_ValidImage_Returns200AndSavesUrl()
    {
        var runner = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await runner.PostAsync("/api/users/profile/picture", ImageForm("image/png", "me.png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The new address should now appear on the user's profile
        var profile = await runner.GetFromJsonAsync<JsonElement>("/api/users/profile");
        Assert.Equal("https://fake.blob.test/profiles/me.png", profile.GetProperty("profilePictureUrl").GetString());
    }

    // ---------- Event banner ----------

    [Fact]
    public async Task UploadBanner_AsParticipant_Returns403()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var runner = await TestHelpers.LoggedInClientAsync(_factory, "runner@test.com");

        var response = await runner.PostAsync($"/api/events/{eventId}/banner", ImageForm("image/png", "banner.png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AnotherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoggedInClientAsync(_factory, "org1@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);
        var otherOrganiser = await TestHelpers.LoggedInClientAsync(_factory, "org2@test.com", "Organiser");

        var response = await otherOrganiser.PostAsync($"/api/events/{eventId}/banner", ImageForm("image/png", "banner.png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_NonExistentEvent_Returns404()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");

        var response = await organiser.PostAsync("/api/events/9999/banner", ImageForm("image/png", "banner.png"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ValidImage_Returns200AndSavesUrl()
    {
        var organiser = await TestHelpers.LoggedInClientAsync(_factory, "org@test.com", "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageForm("image/png", "banner.png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Anyone should now see the banner address on the event
        var fetched = await _factory.NewClient().GetFromJsonAsync<JsonElement>($"/api/events/{eventId}");
        Assert.Equal("https://fake.blob.test/banners/banner.png", fetched.GetProperty("bannerImageUrl").GetString());
    }
}