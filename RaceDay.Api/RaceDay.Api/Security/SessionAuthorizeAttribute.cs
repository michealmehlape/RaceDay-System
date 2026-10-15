using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

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

        if (userId is null)
        {
            context.Result = new UnauthorizedObjectResult("You must be logged in.");
            return;
        }

        if (Role is not null && role != Role)
        {
            context.Result = new ObjectResult($"This action requires the {Role} role.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}

