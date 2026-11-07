using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiService _api;

    public HomeController(ApiService api)
    {
        _api = api;
    }

    // The home page: shows upcoming events, optionally filtered by type
    public async Task<IActionResult> Index(string? eventType)
    {
        var model = new HomeViewModel();
        model.SelectedType = eventType;

        // Ask the API for the events. The web app has no database of its own.
        var result = await _api.GetEventsAsync(eventType);

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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}