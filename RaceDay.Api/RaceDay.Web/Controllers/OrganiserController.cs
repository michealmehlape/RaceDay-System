using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

// Every page in this controller is only for logged-in Organisers
[SessionAuthorize(Role = "Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiService _api;

    public OrganiserController(ApiService api)
    {
        _api = api;
    }

    // The Organiser's home page: a summary of all their events
    public async Task<IActionResult> Dashboard()
    {
        var result = await _api.GetMyEventsAsync();

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        var model = new OrganiserDashboardViewModel();

        if (result.Success && result.Data != null)
        {
            model.Events = result.Data;
        }
        else
        {
            model.ErrorMessage = result.Error;
        }

        return View(model);
    }
    // The management page: the same list as the dashboard, with Edit and Delete buttons
    public async Task<IActionResult> Events()
    {
        var result = await _api.GetMyEventsAsync();

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        var model = new OrganiserDashboardViewModel();

        if (result.Success && result.Data != null)
        {
            model.Events = result.Data;
        }
        else
        {
            model.ErrorMessage = result.Error;
        }

        return View(model);
    }

    // Shows the empty Create form
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateEventViewModel());
    }

    // Runs when the Create form is submitted
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEventViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Step 1: create the event
        var eventResult = await _api.CreateEventAsync(model);

        if (eventResult.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (!eventResult.Success || eventResult.Data == null)
        {
            ModelState.AddModelError(string.Empty, eventResult.Error ?? "We couldn't create the event. Please try again.");
            return View(model);
        }

        // Step 2: create the first category from the distance, limit and fee
        string categoryName = model.DistanceKM!.Value.ToString("0.##") + " km";

        var categoryResult = await _api.CreateCategoryAsync(
            eventResult.Data.EventID,
            categoryName,
            model.DistanceKM.Value,
            model.MaxParticipants!.Value,
            model.EntryFee!.Value);

        if (categoryResult.Success)
        {
            TempData["Success"] = "Your event has been created.";
        }
        else
        {
            TempData["Error"] = "Your event was created, but its first category could not be added: " + categoryResult.Error;
        }

        return RedirectToAction("Events");
    }

    // Shows the Edit form filled in with the event's current details
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // We look the event up in this Organiser's own list, so other people's events never show
        var result = await _api.GetMyEventsAsync();

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction("Events");
        }

        var found = result.Data.FirstOrDefault(e => e.EventID == id);

        if (found == null)
        {
            TempData["Error"] = "We couldn't find that event.";
            return RedirectToAction("Events");
        }

        var model = new EventFormViewModel();
        model.EventID = found.EventID;
        model.EventName = found.EventName;
        model.Description = found.Description;
        model.EventDate = found.EventDate;
        model.Location = found.Location;
        model.EventType = found.EventType;

        return View(model);
    }

    // Runs when the Edit form is submitted
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _api.UpdateEventAsync(model.EventID, model);

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't save your changes. Please try again.");
            return View(model);
        }

        TempData["Success"] = "Your event has been updated.";
        return RedirectToAction("Events");
    }

    // Runs when the Organiser clicks Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteEventAsync(id);

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (result.Success)
        {
            TempData["Success"] = "The event has been deleted.";
        }
        else
        {
            // For example: the event still has categories
            TempData["Error"] = result.Error;
        }

        return RedirectToAction("Events");
    }

    // Looks up one of THIS organiser's events (so other people's events are never shown)
    private async Task<EventDto?> FindMyEventAsync(int eventId)
    {
        var result = await _api.GetMyEventsAsync();

        if (!result.Success || result.Data == null)
        {
            return null;
        }

        return result.Data.FirstOrDefault(e => e.EventID == eventId);
    }

    // Lists the categories of one event
    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var myEvent = await FindMyEventAsync(eventId);

        if (myEvent == null)
        {
            TempData["Error"] = "We couldn't find that event.";
            return RedirectToAction("Events");
        }

        var model = new CategoriesViewModel();
        model.EventID = myEvent.EventID;
        model.EventName = myEvent.EventName;

        var result = await _api.GetCategoriesAsync(eventId);

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (result.Success && result.Data != null)
        {
            model.Categories = result.Data;
        }
        else
        {
            model.ErrorMessage = result.Error;
        }

        return View(model);
    }

    // Shows the empty Add category form
    [HttpGet]
    public async Task<IActionResult> AddCategory(int eventId)
    {
        var myEvent = await FindMyEventAsync(eventId);

        if (myEvent == null)
        {
            TempData["Error"] = "We couldn't find that event.";
            return RedirectToAction("Events");
        }

        var model = new CategoryFormViewModel();
        model.EventID = myEvent.EventID;
        model.EventName = myEvent.EventName;

        return View("CategoryForm", model);
    }

    // Runs when the Add category form is submitted
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CategoryForm", model);
        }

        var result = await _api.CreateCategoryAsync(
            model.EventID,
            model.CategoryName,
            model.DistanceKM!.Value,
            model.MaxParticipants!.Value,
            model.EntryFee!.Value);

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't add the category. Please try again.");
            return View("CategoryForm", model);
        }

        TempData["Success"] = "The category has been added.";
        return RedirectToAction("Categories", new { eventId = model.EventID });
    }

    // Shows the Edit category form filled in with the current details
    [HttpGet]
    public async Task<IActionResult> EditCategory(int eventId, int categoryId)
    {
        var myEvent = await FindMyEventAsync(eventId);

        if (myEvent == null)
        {
            TempData["Error"] = "We couldn't find that event.";
            return RedirectToAction("Events");
        }

        var result = await _api.GetCategoriesAsync(eventId);
        var found = result.Data?.FirstOrDefault(c => c.CategoryID == categoryId);

        if (!result.Success || found == null)
        {
            TempData["Error"] = "We couldn't find that category.";
            return RedirectToAction("Categories", new { eventId = eventId });
        }

        var model = new CategoryFormViewModel();
        model.CategoryID = found.CategoryID;
        model.EventID = myEvent.EventID;
        model.EventName = myEvent.EventName;
        model.CategoryName = found.CategoryName;
        model.DistanceKM = found.DistanceKM;
        model.MaxParticipants = found.MaxParticipants;
        model.EntryFee = found.EntryFee;

        return View("CategoryForm", model);
    }

    // Runs when the Edit category form is submitted
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("CategoryForm", model);
        }

        var result = await _api.UpdateCategoryAsync(model.CategoryID, model);

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't save your changes. Please try again.");
            return View("CategoryForm", model);
        }

        TempData["Success"] = "The category has been updated.";
        return RedirectToAction("Categories", new { eventId = model.EventID });
    }

    // Runs when the Organiser clicks Delete on a category
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var result = await _api.DeleteCategoryAsync(categoryId);

        if (result.IsUnauthorized)
        {
            return RedirectToAction("SessionExpired", "Account");
        }

        if (result.Success)
        {
            TempData["Success"] = "The category has been deleted.";
        }
        else
        {
            // For example: people are already enrolled in it
            TempData["Error"] = result.Error;
        }

        return RedirectToAction("Categories", new { eventId = eventId });
    }
}