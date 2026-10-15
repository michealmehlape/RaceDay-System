using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    // Public: anyone can see the categories for an event
    [HttpGet("events/{eventId}/categories")]
    public async Task<ActionResult<List<CategoryResponse>>> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound("Event not found.");

        var categories = await _db.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.DistanceKM)
            .ToListAsync();

        return Ok(categories.Select(ToResponse).ToList());
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        CategoryID = c.CategoryID,
        EventID = c.EventID,
        CategoryName = c.CategoryName,
        DistanceKM = c.DistanceKM,
        MaxParticipants = c.MaxParticipants,
        EntryFee = c.EntryFee
    };
}