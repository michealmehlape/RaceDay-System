using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    // Public: the leaderboard for a category, finishers first by position
    [HttpGet("categories/{categoryId}/results")]
    public async Task<ActionResult<List<ResultResponse>>> GetForCategory(int categoryId)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryID == categoryId))
            return NotFound("Category not found.");

        var results = await WithDetails()
            .Where(r => r.Enrolment.CategoryID == categoryId)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .ToListAsync();

        var counts = await FinisherCounts(new[] { categoryId });
        return Ok(results.Select(r => ToResponse(r, counts)).ToList());
    }

    [HttpGet("results/{id}")]
    public async Task<ActionResult<ResultResponse>> GetById(int id)
    {
        var result = await WithDetails().FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null) return NotFound("Result not found.");

        var counts = await FinisherCounts(new[] { result.Enrolment.CategoryID });
        return Ok(ToResponse(result, counts));
    }

    [HttpPost("enrolments/{enrolmentId}/results")]
    [SessionAuthorize(Role = "Organiser")]
    public async Task<ActionResult<ResultResponse>> Create(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);
        if (enrolment is null) return NotFound("Enrolment not found.");

        if (enrolment.Category.Event.OrganiserID != HttpContext.Session.GetCurrentUserId())
            return StatusCode(StatusCodes.Status403Forbidden, "You can only record results for your own events.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentID == enrolmentId))
            return Conflict("A result has already been recorded for this enrolment.");

        // A finisher needs a time and a position; DNF and DNS don't
        if (request.Status == "Finished" && (request.FinishTime is null || request.Position is null))
            return BadRequest("A finished result needs both a finish time and a position.");

        // Two people can't share a position within one category
        if (request.Position is not null &&
            await _db.Results.AnyAsync(r => r.Enrolment.CategoryID == enrolment.CategoryID
                                            && r.Position == request.Position))
            return Conflict("That position is already taken in this category.");

        var result = new Result
        {
            EnrolmentID = enrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await WithDetails().FirstAsync(r => r.ResultID == result.ResultID);
        var counts = await FinisherCounts(new[] { saved.Enrolment.CategoryID });
        return StatusCode(StatusCodes.Status201Created, ToResponse(saved, counts));
    }

    [HttpGet("users/results")]
    [SessionAuthorize(Role = "Participant")]
    public async Task<ActionResult<List<ResultResponse>>> GetMine()
    {
        var participantId = HttpContext.Session.GetCurrentUserId();

        var results = await WithDetails()
            .Where(r => r.Enrolment.ParticipantID == participantId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .ToListAsync();

        var counts = await FinisherCounts(results.Select(r => r.Enrolment.CategoryID).Distinct().ToList());
        return Ok(results.Select(r => ToResponse(r, counts)).ToList());
    }



    // Loads everything the response needs
    private IQueryable<Result> WithDetails() => _db.Results
        .Include(r => r.Enrolment.Participant)
        .Include(r => r.Enrolment.Category.Event);

    // How many people finished each category, so a position can be shown as "5th of 312"
    private async Task<Dictionary<int, int>> FinisherCounts(IEnumerable<int> categoryIds) =>
        await _db.Results
            .Where(r => r.Status == "Finished" && categoryIds.Contains(r.Enrolment.CategoryID))
            .GroupBy(r => r.Enrolment.CategoryID)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

    private static ResultResponse ToResponse(Result r, Dictionary<int, int> counts) => new()
    {
        ResultID = r.ResultID,
        EnrolmentID = r.EnrolmentID,
        ParticipantName = r.Enrolment.Participant.FullName,
        BibNumber = r.Enrolment.BibNumber,
        CategoryID = r.Enrolment.CategoryID,
        CategoryName = r.Enrolment.Category.CategoryName,
        EventID = r.Enrolment.Category.EventID,
        EventName = r.Enrolment.Category.Event.EventName,
        EventDate = r.Enrolment.Category.Event.EventDate,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status,
        TotalFinishers = counts.GetValueOrDefault(r.Enrolment.CategoryID)
    };
}