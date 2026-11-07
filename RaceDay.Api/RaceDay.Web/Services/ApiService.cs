using RaceDay.Web.ViewModels;
using System.Net.Http.Json;
using System.Text.Json;
using static RaceDay.Web.ViewModels.ResultDto;

namespace RaceDay.Web.Services;

public class ApiService
{
    private const string ApiSessionCookieName = ".AspNetCore.Session";
    private const string ApiCookieKey = "ApiCookie";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiService(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    // ---------- Authentication ----------

    public Task<ApiResult<UserDto>> RegisterAsync(RegisterViewModel model) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            organiserCode = model.OrganiserCode
        });

    public Task<ApiResult<UserDto>> LoginAsync(string email, string password) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/login", new { email, password });

    public Task<ApiResult<string>> LogoutAsync() =>
        SendAsync<string>(HttpMethod.Post, "api/auth/logout");

    // ---------- Events ----------

    public Task<ApiResult<List<EventDto>>> GetEventsAsync(string? eventType)
    {
        var url = "api/events";

        // Only add the filter to the address if the user picked one
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            url = url + "?eventType=" + Uri.EscapeDataString(eventType);
        }

        return SendAsync<List<EventDto>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<EventDto>> GetEventAsync(int id)
    {
        return SendAsync<EventDto>(HttpMethod.Get, "api/events/" + id);
    }

    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId)
    {
        return SendAsync<List<CategoryDto>>(HttpMethod.Get, "api/events/" + eventId + "/categories");
    }

    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId)
    {
        return SendAsync<EnrolmentDto>(HttpMethod.Post, "api/categories/" + categoryId + "/enrol");
    }

    // ---------- Participant ----------

    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync()
    {
        return SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/users/enrolments");
    }

    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync()
    {
        return SendAsync<List<ResultDto>>(HttpMethod.Get, "api/users/results");
    }

    public Task<ApiResult<string>> CancelEnrolmentAsync(int enrolmentId)
    {
        return SendAsync<string>(HttpMethod.Delete, "api/enrolments/" + enrolmentId);
    }

    public Task<ApiResult<UserDto>> GetProfileAsync()
    {
        return SendAsync<UserDto>(HttpMethod.Get, "api/users/profile");
    }

    public Task<ApiResult<UserDto>> UpdateProfileAsync(string fullName, string email)
    {
        var body = new
        {
            fullName = fullName,
            email = email
        };

        return SendAsync<UserDto>(HttpMethod.Put, "api/users/profile", body);
    }

    // ---------- Organiser ----------

    public Task<ApiResult<List<OrganiserEventDto>>> GetMyEventsAsync()
    {
        return SendAsync<List<OrganiserEventDto>>(HttpMethod.Get, "api/events/mine");

    }

    public Task<ApiResult<EventDto>> CreateEventAsync(EventFormViewModel model)
    {
        return SendAsync<EventDto>(HttpMethod.Post, "api/events", BuildEventBody(model));
    }

    public Task<ApiResult<EventDto>> UpdateEventAsync(int id, EventFormViewModel model)
    {
        return SendAsync<EventDto>(HttpMethod.Put, "api/events/" + id, BuildEventBody(model));
    }

    public Task<ApiResult<string>> DeleteEventAsync(int id)
    {
        return SendAsync<string>(HttpMethod.Delete, "api/events/" + id);
    }

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, string name, decimal distanceKm, int maxParticipants, decimal entryFee)
    {
        var body = new
        {
            categoryName = name,
            distanceKM = distanceKm,
            maxParticipants = maxParticipants,
            entryFee = entryFee
        };

        return SendAsync<CategoryDto>(HttpMethod.Post, "api/events/" + eventId + "/categories", body);
    }

    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, CategoryFormViewModel model)
    {
        var body = new
        {
            categoryName = model.CategoryName,
            distanceKM = model.DistanceKM,
            maxParticipants = model.MaxParticipants,
            entryFee = model.EntryFee
        };

        return SendAsync<CategoryDto>(HttpMethod.Put, "api/categories/" + categoryId, body);
    }

    public Task<ApiResult<string>> DeleteCategoryAsync(int categoryId)
    {
        return SendAsync<string>(HttpMethod.Delete, "api/categories/" + categoryId);
    }

    public Task<ApiResult<List<EnrolmentDto>>> GetCategoryEnrolmentsAsync(int categoryId)
    {
        return SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/categories/" + categoryId + "/enrolments");
    }

    public Task<ApiResult<string>> RecordResultAsync(int enrolmentId, string? finishTime, int? position, string status)
    {
        var body = new
        {
            finishTime = finishTime,
            position = position,
            status = status
        };

        return SendAsync<string>(HttpMethod.Post, "api/enrolments/" + enrolmentId + "/results", body);
    }

    // Builds the JSON the API expects when creating or updating an event
    private static object BuildEventBody(EventFormViewModel model)
    {
        return new
        {
            eventName = model.EventName,
            eventDate = model.EventDate,
            location = model.Location,
            description = model.Description,
            eventType = model.EventType
        };
    }

    // ---------- Core: every API call goes through here ----------

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null)
                request.Content = JsonContent.Create(body);

            // Send this user's own API session cookie, if they have one
            var cookie = Session.GetString(ApiCookieKey);
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.TryAddWithoutValidation("Cookie", cookie);

            using var response = await _http.SendAsync(request);
            CaptureApiCookie(response);
            return await ReadResultAsync<T>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Fail<T>(0, "RaceDay is having trouble reaching its server. Please try again in a moment.");
        }
    }

    // Keep the API's session cookie in this user's web session
    private void CaptureApiCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        var sessionCookie = values.FirstOrDefault(v =>
            v.StartsWith(ApiSessionCookieName + "=", StringComparison.Ordinal));
        if (sessionCookie is null) return;

        // Keep only "name=value", dropping path, samesite and httponly
        Session.SetString(ApiCookieKey, sessionCookie.Split(';')[0]);
    }

    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
            return Fail<T>(status, await FriendlyErrorAsync(response));

        // Some endpoints reply with a plain sentence, such as "Logged out."
        if (typeof(T) == typeof(string))
        {
            var text = await response.Content.ReadAsStringAsync();
            return new ApiResult<T> { Success = true, StatusCode = status, Data = (T)(object)text };
        }

        var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return new ApiResult<T> { Success = true, StatusCode = status, Data = data };
    }

    // Turn whatever the API said into something safe to show a user
    private static async Task<string> FriendlyErrorAsync(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        if (status >= 500)
            return "Something went wrong on our side. Please try again.";

        var body = (await response.Content.ReadAsStringAsync()).Trim();

        if (body.StartsWith('{'))
        {
            // Validation errors from the API arrive as JSON
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("errors", out var errors)
                    && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => p.Value.EnumerateArray().Select(m => m.GetString()))
                        .Where(m => !string.IsNullOrWhiteSpace(m));

                    var joined = string.Join(" ", messages);
                    if (joined.Length > 0) return joined;
                }
            }
            catch (JsonException) { }

            return "Please check the details you entered and try again.";
        }

        // The API writes its own plain-sentence messages, such as "That email is already registered."
        if (body.Length > 0 && body.Length < 200)
            return body.Trim('"');

        return status switch
        {
            401 => "Please log in to continue.",
            403 => "You don't have permission to do that.",
            404 => "We couldn't find what you were looking for.",
            _ => "That didn't work. Please try again."
        };
    }

    private static ApiResult<T> Fail<T>(int status, string message) =>
        new() { Success = false, StatusCode = status, Error = message };
}