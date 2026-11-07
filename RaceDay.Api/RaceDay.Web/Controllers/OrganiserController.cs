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
}