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

    /// <summary>
    /// Lists the categories of an event, shortest distance first.
    /// </summary>
    /// <remarks>
    /// Open to everyone.
    /// </remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <response code="200">The event's categories, empty if it has none.</response>
    /// <response code="404">No event has that ID.</response>
    [ProducesResponseType<List<CategoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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


    /// <summary>
    /// Adds a category (for example 10km or 21km) to an event.
    /// </summary>
    /// <remarks>
    /// Organiser only, and only for events they own.
    /// </remarks>
    /// <param name="eventId">The event's ID.</param>
    /// <param name="request">Category name, distance in km, maximum participants, and entry fee.</param>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed, for example a distance of zero.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No event has that ID.</response>
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPost("events/{eventId}/categories")]
    [SessionAuthorize(Role = "Organiser")]
    public async Task<ActionResult<CategoryResponse>> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound("Event not found.");

        if (ev.OrganiserID != HttpContext.Session.GetCurrentUserId())
            return StatusCode(StatusCodes.Status403Forbidden, "You can only add categories to your own events.");

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKM = request.DistanceKM,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(category));
    }

    /// <summary>
    /// Updates a category on an event the Organiser owns.
    /// </summary>
    /// <remarks>
    /// Organiser only. The participant limit can't be lowered below the number already enrolled.
    /// </remarks>
    /// <param name="id">The category's ID.</param>
    /// <param name="request">The new category details.</param>
    /// <response code="200">Category updated. Returns the new details.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category has that ID.</response>
    /// <response code="409">The new limit is lower than the current number of enrolments.</response>
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    [HttpPut("categories/{id}")]
    [SessionAuthorize(Role = "Organiser")]
    public async Task<ActionResult<CategoryResponse>> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category is null) return NotFound("Category not found.");

        // Ownership comes from the category's event
        if (category.Event.OrganiserID != HttpContext.Session.GetCurrentUserId())
            return StatusCode(StatusCodes.Status403Forbidden, "You can only edit categories on your own events.");

        // Don't allow shrinking below the number of people already enrolled
        var enrolled = await _db.Enrolments.CountAsync(e => e.CategoryID == id);
        if (request.MaxParticipants < enrolled)
            return Conflict($"{enrolled} participants are already enrolled, so the limit can't go below that.");

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKM = request.DistanceKM;
        category.MaxParticipants = request.MaxParticipants;
        category.EntryFee = request.EntryFee;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(category));
    }

    /// <summary>
    /// Deletes a category from an event the Organiser owns.
    /// </summary>
    /// <remarks>
    /// Organiser only. A category that already has enrolments can't be deleted.
    /// </remarks>
    /// <param name="id">The category's ID.</param>
    /// <response code="200">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category has that ID.</response>
    /// <response code="409">The category has enrolments.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    [HttpDelete("categories/{id}")]
    [SessionAuthorize(Role = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category is null) return NotFound("Category not found.");

        if (category.Event.OrganiserID != HttpContext.Session.GetCurrentUserId())
            return StatusCode(StatusCodes.Status403Forbidden, "You can only delete categories on your own events.");

        if (await _db.Enrolments.AnyAsync(e => e.CategoryID == id))
            return Conflict("This category has enrolments and can't be deleted.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return Ok("Category deleted.");
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