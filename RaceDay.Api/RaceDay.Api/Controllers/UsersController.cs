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


    /// <summary>
    /// Returns the logged-in user's own profile.
    /// </summary>
    /// <remarks>
    /// The user is identified from the session, so no ID is sent.
    /// </remarks>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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


    /// <summary>
    /// Updates the logged-in user's name and email.
    /// </summary>
    /// <param name="request">The new full name and email address.</param>
    /// <response code="200">Profile updated. Returns the new details.</response>
    /// <response code="400">Validation failed, for example a badly formed email.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="409">That email already belongs to another account.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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