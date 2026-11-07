using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiService _api;

    public AccountController(ApiService api) => _api = api;

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.LoginAsync(model.Email, model.Password);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't log you in. Please try again.");
            return View(model);
        }

        // Remember who is logged in, for every page that follows
        HttpContext.Session.SetInt32("UserId", result.Data.UserID);
        HttpContext.Session.SetString("Role", result.Data.Role);
        HttpContext.Session.SetString("FullName", result.Data.FullName);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // The API decides whether the organiser code is right. The web app doesn't repeat that rule.
        var result = await _api.RegisterAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't create your account. Please try again.");
            return View(model);
        }

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.LogoutAsync();
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
    [HttpGet]
    public IActionResult AccessDenied() => View();
    // Used when the API says the user's session has ended
    public IActionResult SessionExpired()
    {
        HttpContext.Session.Clear();
        TempData["Info"] = "Your session has ended. Please log in again.";
        return RedirectToAction("Login");
    }
}
