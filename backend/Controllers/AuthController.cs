
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DoctorsAppointment.Api.Controllers;

public record LoginRequest(string Username, string Password);

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ClinicDbContext _db;
    private readonly IPasswordHasher<ClinicUser> _hasher;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _clock;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ClinicDbContext db,
        IPasswordHasher<ClinicUser> hasher,
        IConfiguration configuration,
        TimeProvider clock,
        ILogger<AuthController> logger)
    {
        _db = db;
        _hasher = hasher;
        _configuration = configuration;
        _clock = clock;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Problem(
                statusCode: 400,
                title: "Username and password are required."
            );
        }

        // Find the user in PostgreSQL
        var user = await _db.ClinicUsers
            .FirstOrDefaultAsync(u => u.Username == request.Username.Trim());

        // Verify the supplied password
        var validPassword = user != null &&
            _hasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            ) != PasswordVerificationResult.Failed;

        if (!validPassword || user == null)
        {
            _logger.LogWarning("Failed login attempt");

            return Problem(
                statusCode: 401,
                title: "Invalid username or password."
            );
        }

        // Add the user's identity and role to the token
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.DoctorId.HasValue)
        {
            claims.Add(new Claim("doctorId", user.DoctorId.Value.ToString()));
        }

        // Sign the token
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
        );

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: _clock.GetUtcNow().UtcDateTime.AddHours(2),
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            )
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            role = user.Role,
            doctorId = user.DoctorId
        });
    }
}
