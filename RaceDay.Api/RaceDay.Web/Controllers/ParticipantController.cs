using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

// Every page in this controller is only for logged-in Participants
[SessionAuthorize(Role = "Participant")]
public class ParticipantController : Controller
{
    private readonly ApiService _api;

    public ParticipantController(ApiService api)
    {
        _api = api;
    }

    // Lists every event this Participant has entered
    public async Task<IActionResult> MyEvents()
    {
        var result = await _api.GetMyEnrolmentsAsync();

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        var model = new MyEventsViewModel();

        if (result.Success && result.Data != null)
        {
            model.Enrolments = result.Data;
        }
        else
        {
            model.ErrorMessage = result.Error;
        }

        return View(model);
    }

    // Runs when the Participant clicks "Cancel" on one of their enrolments
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEnrolment(int enrolmentId)
    {
        var result = await _api.CancelEnrolmentAsync(enrolmentId);

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (result.Success)
        {
            TempData["Success"] = "Your enrolment has been cancelled.";
        }
        else
        {
            // For example: a result has already been recorded
            TempData["Error"] = result.Error;
        }

        return RedirectToAction("MyEvents");
    }

    // Shows this Participant's race history
    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetMyResultsAsync();

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        var model = new MyResultsViewModel();

        if (result.Success && result.Data != null)
        {
            model.Results = result.Data;
        }
        else
        {
            model.ErrorMessage = result.Error;
        }

        return View(model);
    }
}