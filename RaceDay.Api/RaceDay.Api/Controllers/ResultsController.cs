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

    /// <summary>
    /// Records the result for an enrolment.
    /// </summary>
    /// <remarks>
    /// Organiser only, and only for events they own. A Finished result needs a finish time and a position.
    /// DNF (did not finish) and DNS (did not start) don't. Each enrolment can have one result,
    /// and no two results in a category can share a position.
    /// </remarks>
    /// <param name="enrolmentId">The enrolment's ID.</param>
    /// <param name="request">Finish time (for example 00:52:14), position, and status (Finished, DNF or DNS).</param>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Validation failed, for example a Finished result with no time.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">No enrolment has that ID.</response>
    /// <response code="409">This enrolment already has a result, or the position is taken.</response>
    [ProducesResponseType<ResultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]

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

    // Public: the leaderboard for a category, finishers first by position

    /// <summary>
    /// Returns the results leaderboard for a category.
    /// </summary>
    /// <remarks>
    /// Open to everyone. Ordered by finishing position, with results that have no position last.
    /// </remarks>
    /// <param name="categoryId">The category's ID.</param>
    /// <response code="200">The results, empty if none are recorded yet.</response>
    /// <response code="404">No category has that ID.</response>
    [ProducesResponseType<List<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]


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

    /// <summary>
    /// Returns a single result.
    /// </summary>
    /// <remarks>
    /// Open to everyone.
    /// </remarks>
    /// <param name="id">The result's ID.</param>
    /// <response code="200">The result.</response>
    /// <response code="404">No result has that ID.</response>
    [ProducesResponseType<ResultResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    [HttpGet("results/{id}")]
    public async Task<ActionResult<ResultResponse>> GetById(int id)
    {
        var result = await WithDetails().FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null) return NotFound("Result not found.");

        var counts = await FinisherCounts(new[] { result.Enrolment.CategoryID });
        return Ok(ToResponse(result, counts));
    }

    /// <summary>
    /// Returns the logged-in Participant's own race history.
    /// </summary>
    /// <remarks>
    /// Participant only. Most recent events first. Each result includes the number of finishers
    /// in the category, so a position can be shown as "5th of 312".
    /// </remarks>
    /// <response code="200">The Participant's results, empty if none.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The user is not a Participant.</response>
    [ProducesResponseType<List<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]

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