using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using BCrypt.Net;

namespace MusicFinder.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MySqlConnection _db;

    public AuthController(MySqlConnection db)
    {
        _db = db;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Email and password are required." });

        var email = req.Email.Trim().ToLowerInvariant();

        await _db.OpenAsync();

        // Check for existing user
        await using (var checkCmd = _db.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM users WHERE email = @email";
            checkCmd.Parameters.AddWithValue("@email", email);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (count > 0)
                return Conflict(new { error = "This email is already registered." });
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        long newId;
        await using (var insertCmd = _db.CreateCommand())
        {
            insertCmd.CommandText =
                "INSERT INTO users (email, password_hash) VALUES (@email, @hash); SELECT LAST_INSERT_ID();";
            insertCmd.Parameters.AddWithValue("@email", email);
            insertCmd.Parameters.AddWithValue("@hash", hash);
            newId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());
        }

        return Ok(new { userId = newId, email });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Email and password are required." });

        var email = req.Email.Trim().ToLowerInvariant();

        await _db.OpenAsync();

        string? storedHash = null;
        long userId = 0;

        await using (var cmd = _db.CreateCommand())
        {
            cmd.CommandText = "SELECT id, password_hash FROM users WHERE email = @email LIMIT 1";
            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                userId = reader.GetInt64("id");
                storedHash = reader.GetString("password_hash");
            }
        }

        if (storedHash == null || !BCrypt.Net.BCrypt.Verify(req.Password, storedHash))
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(new { userId, email });
    }
}

public record AuthRequest(string Email, string Password);
