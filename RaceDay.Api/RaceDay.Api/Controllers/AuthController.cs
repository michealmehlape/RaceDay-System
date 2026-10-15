using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }


    /// <summary>
    /// Creates a new Participant or Organiser account.
    /// </summary>
    /// <remarks>
    /// Organisers must also supply the organiser access code. The password is hashed before it is stored.
    /// </remarks>
    /// <param name="request">Full name, email, password (8+ characters), role, and the access code if registering as an Organiser.</param>
    /// <response code="201">Account created. Returns the user without the password.</response>
    /// <response code="400">Validation failed, for example a bad email or a short password.</response>
    /// <response code="403">The organiser access code is missing or wrong.</response>
    /// <response code="409">That email is already registered.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        if (request.Role != "Organiser" && request.Role != "Participant")
            return BadRequest("Role must be 'Organiser' or 'Participant'.");

        // Organisers must prove they are allowed to be one
        if (request.Role == "Organiser")
        {
            var expectedCode = _config["OrganiserAccessCode"];
            if (string.IsNullOrEmpty(expectedCode) || request.OrganiserCode != expectedCode)
                return StatusCode(StatusCodes.Status403Forbidden, "A valid organiser access code is required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("That email is already registered.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(user));
    }


    /// <summary>
    /// Logs a user in and starts a server-side session.
    /// </summary>
    /// <remarks>
    /// The session stores the user's ID and role and is sent back as a cookie on later requests.
    /// </remarks>
    /// <param name="request">Email and password.</param>
    /// <response code="200">Login successful. Returns the user's details.</response>
    /// <response code="401">The email or password is wrong.</response>
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password"
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        // This is the session: the server remembers who this browser is
        HttpContext.Session.SetInt32("UserId", user.UserID);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(ToResponse(user));
    }


    /// <summary>
    /// Ends the current session.
    /// </summary>
    /// <remarks>
    /// Clears the stored user ID and role, so later requests are treated as not logged in.
    /// </remarks>
    /// <response code="200">Logged out.</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok("Logged out.");
    }

    private static UserResponse ToResponse(User user) => new()
    {
        UserID = user.UserID,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        ProfilePictureUrl = user.ProfilePictureUrl
    };
}