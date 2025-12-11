using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Telefoniraamatu.Server.DTOs;
using Telefoniraamatu.Server.Models;
using Telefoniraamatu.Server.Services;
using Swashbuckle.AspNetCore.Filters;

namespace Telefoniraamatu.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IDataStore _store;
    private readonly IConfiguration _config;

    public AuthController(IDataStore store, IConfiguration config)
    {
        _store = store;
        _config = config;
    }

    [HttpPost("register")]
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.AuthResponseExample))]
    public ActionResult<AuthResponse> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Username and password are required");

        if (_store.Users.Values.Any(u => u.Username.Equals(req.Username, StringComparison.OrdinalIgnoreCase)))
            return Conflict("Username already exists");

        var (hash, salt) = PasswordHasher.HashPassword(req.Password);
        var user = new User
        {
            Username = req.Username,
            PasswordHash = hash,
            PasswordSalt = salt
        };
        _store.Users[user.Id] = user;
        return Ok(new AuthResponse { Token = GenerateJwt(user) });
    }

    [HttpPost("login")]
    [SwaggerResponseExample(200, typeof(Telefoniraamatu.Server.Swagger.Examples.AuthResponseExample))]
    public ActionResult<AuthResponse> Login([FromBody] LoginRequest req)
    {
        var user = _store.Users.Values.FirstOrDefault(u => u.Username.Equals(req.Username, StringComparison.OrdinalIgnoreCase));
        if (user is null) return Unauthorized("Invalid credentials");
        if (!PasswordHasher.Verify(req.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized("Invalid credentials");

        return Ok(new AuthResponse { Token = GenerateJwt(user) });
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        return Ok(new { id = userId, username });
    }

    private string GenerateJwt(User user)
    {
        var issuer = _config["Jwt:Issuer"] ?? "telefoniraamatu";
        var audience = _config["Jwt:Audience"] ?? "telefoniraamatu-client";
        var secret = _config["Jwt:Secret"] ?? "dev-secret-change-me";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
