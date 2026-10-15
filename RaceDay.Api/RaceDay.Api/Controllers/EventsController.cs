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

    [HttpGet("{id}")]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (ev is null) return NotFound("Event not found.");
        return Ok(ToResponse(ev));
    }

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