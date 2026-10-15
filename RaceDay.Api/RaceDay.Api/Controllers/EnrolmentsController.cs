using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>
    /// Enrols the logged-in Participant in a category.
    /// </summary>
    /// <remarks>
    /// Participant only. No body is needed: the category comes from the URL and the participant from the session.
    /// A bib number is generated automatically. Enrolment is refused if the event has passed,
    /// the Participant is already enrolled, or the category is full.
    /// </remarks>
    /// <param name="categoryId">The category's ID.</param>
    /// <response code="201">Enrolled. Returns the enrolment, including the bib number.</response>
    /// <response code="400">The event has already taken place.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not a Participant.</response>
    /// <response code="404">No category has that ID.</response>
    /// <response code="409">Already enrolled in this category, or the category is full.</response>
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    [HttpPost("categories/{categoryId}/enrol")]
    [SessionAuthorize(Role = "Participant")]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int categoryId)
    {
        var participantId = HttpContext.Session.GetCurrentUserId();

        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category is null) return NotFound("Category not found.");

        if (category.Event.EventDate < DateTime.Today)
            return BadRequest("This event has already taken place.");

        if (await _db.Enrolments.AnyAsync(e => e.ParticipantID == participantId && e.CategoryID == categoryId))
            return Conflict("You are already enrolled in this category.");

        var taken = await _db.Enrolments.CountAsync(e => e.CategoryID == categoryId);
        if (taken >= category.MaxParticipants)
            return Conflict("This category is full.");

        try
        {
            // Save once with a throwaway unique bib to get the new EnrolmentID...
            var enrolment = new Enrolment
            {
                ParticipantID = participantId,
                CategoryID = categoryId,
                BibNumber = Guid.NewGuid().ToString("N")[..20]
            };
            _db.Enrolments.Add(enrolment);
            await _db.SaveChangesAsync();

            // ...then turn the ID into the real bib number
            enrolment.BibNumber = $"BIB-{enrolment.EnrolmentID:D5}";
            await _db.SaveChangesAsync();

            var saved = await WithDetails().FirstAsync(e => e.EnrolmentID == enrolment.EnrolmentID);
            return StatusCode(StatusCodes.Status201Created, ToResponse(saved));
        }
        catch (DbUpdateException)
        {
            // Two requests racing past the checks above hit the unique index
            return Conflict("You are already enrolled in this category.");
        }
    }


    /// <summary>
    /// Lists the logged-in Participant's own enrolments.
    /// </summary>
    /// <remarks>
    /// Participant only. Ordered by event date.
    /// </remarks>
    /// <response code="200">The Participant's enrolments, empty if none.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not a Participant.</response>
    [ProducesResponseType<List<EnrolmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]

    [HttpGet("users/enrolments")]
    [SessionAuthorize(Role = "Participant")]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetMine()
    {
        var participantId = HttpContext.Session.GetCurrentUserId();

        var enrolments = await WithDetails()
            .Where(e => e.ParticipantID == participantId)
            .OrderBy(e => e.Category.Event.EventDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse).ToList());
    }

    /// <summary>
    /// Lists everyone enrolled in a category.
    /// </summary>
    /// <remarks>
    /// Organiser only, and only for events they own.
    /// </remarks>
    /// <param name="categoryId">The category's ID.</param>
    /// <response code="200">The enrolments, ordered by bib number, empty if none.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No category has that ID.</response>
    [ProducesResponseType<List<EnrolmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    [HttpGet("categories/{categoryId}/enrolments")]
    [SessionAuthorize(Role = "Organiser")]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetForCategory(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (category is null) return NotFound("Category not found.");

        if (category.Event.OrganiserID != HttpContext.Session.GetCurrentUserId())
            return StatusCode(StatusCodes.Status403Forbidden, "You can only view enrolments for your own events.");

        var enrolments = await WithDetails()
            .Where(e => e.CategoryID == categoryId)
            .OrderBy(e => e.BibNumber)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse).ToList());
    }


    /// <summary>
    /// Cancels the logged-in Participant's own enrolment.
    /// </summary>
    /// <remarks>
    /// Participant only. An enrolment that already has a result can't be cancelled.
    /// </remarks>
    /// <param name="id">The enrolment's ID.</param>
    /// <response code="200">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not a Participant, or the enrolment belongs to someone else.</response>
    /// <response code="404">No enrolment has that ID.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

    [HttpDelete("enrolments/{id}")]
    [SessionAuthorize(Role = "Participant")]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment is null) return NotFound("Enrolment not found.");

        if (enrolment.ParticipantID != HttpContext.Session.GetCurrentUserId())
            return StatusCode(StatusCodes.Status403Forbidden, "You can only cancel your own enrolments.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentID == id))
            return Conflict("A result has already been recorded for this enrolment.");

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return Ok("Enrolment cancelled.");
    }

    // Loads everything the response needs
    private IQueryable<Enrolment> WithDetails() => _db.Enrolments
        .Include(e => e.Participant)
        .Include(e => e.Category).ThenInclude(c => c.Event);

    private static EnrolmentResponse ToResponse(Enrolment e) => new()
    {
        EnrolmentID = e.EnrolmentID,
        ParticipantID = e.ParticipantID,
        ParticipantName = e.Participant.FullName,
        CategoryID = e.CategoryID,
        CategoryName = e.Category.CategoryName,
        DistanceKM = e.Category.DistanceKM,
        EventID = e.Category.EventID,
        EventName = e.Category.Event.EventName,
        EventDate = e.Category.Event.EventDate,
        BibNumber = e.BibNumber,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };
}