using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;


    /// <summary>
    /// Lists all upcoming events, soonest first.
    /// </summary>
    /// <remarks>
    /// Open to everyone. Optionally filter by event type (Run, Walk or Cycle).
    /// </remarks>
    /// <param name="eventType">Optional filter: Run, Walk or Cycle.</param>
    /// <response code="200">The list of upcoming events, empty if none match.</response>
    [ProducesResponseType<List<EventResponse>>(StatusCodes.Status200OK)]
    // Public: anyone can browse upcoming events, optionally filtered by type
    [HttpGet]
    public async Task<ActionResult<List<EventResponse>>> GetUpcoming([FromQuery] string? eventType)
    {
        var query = _db.Events
            .Include(e => e.Organiser)
            .Where(e => e.EventDate >= DateTime.Today);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToResponse).ToList());
    }


    /// <summary>
    /// Returns the details of one event.
    /// </summary>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">The event.</response>
    /// <response code="404">No event has that ID.</response>
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id}")]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev is null) return NotFound("Event not found.");
        return Ok(ToResponse(ev));
    }

    /// <summary>
    /// Creates a new event owned by the logged-in Organiser.
    /// </summary>
    /// <remarks>
    /// Organiser only. The owner is taken from the session, never from the request.
    /// </remarks>
    /// <param name="request">Name, date, location, optional description, and event type (Run, Walk or Cycle).</param>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed, for example an event type that isn't Run, Walk or Cycle.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not an Organiser.</response>
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpPost]
    [SessionAuthorize(Role = "Organiser")]
    public async Task<ActionResult<EventResponse>> Create(EventRequest request)
    {
        var organiserId = HttpContext.Session.GetCurrentUserId();

        var ev = new Event
        {
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate.Date,
            Location = request.Location.Trim(),
            Description = request.Description?.Trim(),
            EventType = request.EventType,
            OrganiserID = organiserId     // from the session, never from the request
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();
        return StatusCode(StatusCodes.Status201Created, ToResponse(ev));
    }


    /// <summary>
    /// Updates an event owned by the logged-in Organiser.
    /// </summary>
    /// <remarks>
    /// Organiser only, and only for events they own. All fields are replaced.
    /// </remarks>
    /// <param name="id">The event's ID.</param>
    /// <param name="request">The new event details.</param>
    /// <response code="200">Event updated. Returns the new details.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has that ID.</response>
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut("{id}")]
    [SessionAuthorize(Role = "Organiser")]
    public async Task<ActionResult<EventResponse>> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev is null) return NotFound("Event not found.");

        // An organiser may only edit their own events
        if (ev.OrganiserID != HttpContext.Session.GetCurrentUserId())
            return StatusCode(StatusCodes.Status403Forbidden, "You can only edit your own events.");

        ev.EventName = request.EventName.Trim();
        ev.EventDate = request.EventDate.Date;
        ev.Location = request.Location.Trim();
        ev.Description = request.Description?.Trim();
        ev.EventType = request.EventType;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(ev));
    }

    /// <summary>
    /// Deletes an event owned by the logged-in Organiser.
    /// </summary>
    /// <remarks>
    /// Organiser only, and only for events they own. The event's categories must be deleted first.
    /// </remarks>
    /// <param name="id">The event's ID.</param>
    /// <response code="200">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has that ID.</response>
    /// <response code="409">The event still has categories.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpDelete("{id}")]
    [SessionAuthorize(Role = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound("Event not found.");

        if (ev.OrganiserID != HttpContext.Session.GetCurrentUserId())
            return StatusCode(StatusCodes.Status403Forbidden, "You can only delete your own events.");

        // The database refuses to delete an event that still has categories (DeleteBehavior.Restrict)
        if (await _db.Categories.AnyAsync(c => c.EventID == id))
            return Conflict("Delete this event's categories first.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return Ok("Event deleted.");
    }

    private static EventResponse ToResponse(Event e) => new()
    {
        EventID = e.EventID,
        EventName = e.EventName,
        EventDate = e.EventDate,
        Location = e.Location,
        Description = e.Description,
        EventType = e.EventType,
        BannerImageUrl = e.BannerImageUrl,
        OrganiserID = e.OrganiserID,
        OrganiserName = e.Organiser.FullName
    };
}