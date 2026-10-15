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