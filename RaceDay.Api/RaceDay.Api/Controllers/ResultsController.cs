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