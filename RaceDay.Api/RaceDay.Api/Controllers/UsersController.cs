using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[SessionAuthorize]   // any logged-in user; no Role set
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public UsersController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }


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


    /// <summary>
    /// Uploads a profile picture for the logged-in user.
    /// </summary>
    /// <remarks>The picture is stored in Azure Blob Storage. JPG, PNG or WEBP, up to 2 MB.</remarks>
    /// <param name="request">The image file to upload.</param>
    /// <response code="200">Picture saved. Returns the user's new details.</response>
    /// <response code="400">No file, wrong file type, or the file is too big.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="503">The image storage could not be reached.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [HttpPost("profile/picture")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UserResponse>> UploadProfilePicture([FromForm] ImageUploadRequest request)
    {
        var file = request.File;

        var userId = HttpContext.Session.GetCurrentUserId();
        
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return Unauthorized("Account no longer exists.");

        // Check the file before uploading it
        if (file.Length == 0)
            return BadRequest("Please choose an image to upload.");

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest("The picture must be 2 MB or smaller.");

        string[] allowedTypes = { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Only JPG, PNG or WEBP images are allowed.");

        // Upload to Azure and keep the address it gives back
        try
        {
            user.ProfilePictureUrl = await _blobs.UploadImageAsync(file, "profiles");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "We couldn't save the picture right now. Please try again later.");
        }

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