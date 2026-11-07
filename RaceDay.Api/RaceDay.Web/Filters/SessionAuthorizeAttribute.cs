using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    // Leave null to allow any logged-in user
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role");

        // Not logged in: send them to the login page
        if (userId is null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        // Logged in, but the wrong role
        if (Role is not null && role != Role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}