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