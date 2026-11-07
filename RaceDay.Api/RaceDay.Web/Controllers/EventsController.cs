using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class EventsController : Controller
{
    private readonly ApiService _api;

    public EventsController(ApiService api)
    {
        _api = api;
    }

    // Shows one event and its categories, for example /Events/Details/3
    public async Task<IActionResult> Details(int id)
    {
        // Step 1: get the event from the API
        var eventResult = await _api.GetEventAsync(id);

        if (!eventResult.Success || eventResult.Data == null)
        {
            // Event not found or API problem: go back home with a message
            TempData["Error"] = eventResult.Error;
            return RedirectToAction("Index", "Home");
        }

        var model = new EventDetailsViewModel();
        model.Event = eventResult.Data;
        model.Role = HttpContext.Session.GetString("Role");

        // Step 2: get the categories for this event
        var categoryResult = await _api.GetCategoriesAsync(id);

        if (categoryResult.Success && categoryResult.Data != null)
        {
            model.Categories = categoryResult.Data;
        }

        return View(model);
    }

    // Runs when the Participant clicks "Enter" next to a category
    [HttpPost]
    [ValidateAntiForgeryToken]
    [SessionAuthorize(Role = "Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await _api.EnrolAsync(categoryId);

        if (result.Success && result.Data != null)
        {
            TempData["Success"] = "You are in! Your bib number is " + result.Data.BibNumber + ".";
        }
        else
        {
            // The API decides if the category is full, or you are already enrolled
            TempData["Error"] = result.Error;
        }

        return RedirectToAction("Details", new { id = eventId });
    }
}