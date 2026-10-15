using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[SessionAuthorize]   // any logged-in user; no Role set
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db) => _db = db;

    [HttpGet("profile")]
    public async Task<ActionResult<UserResponse>> GetProfile()
    {
        var userId = HttpContext.Session.GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Unauthorized("Account no longer exists.");

        return Ok(new UserResponse
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserResponse>> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = HttpContext.Session.GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Unauthorized("Account no longer exists.");

        var email = request.Email.Trim().ToLowerInvariant();

        // The email must not belong to someone else
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserID != userId))
            return Conflict("That email is already in use.");

        user.FullName = request.FullName.Trim();
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(new UserResponse
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            ProfilePictureUrl = user.ProfilePictureUrl
        });
    }
}