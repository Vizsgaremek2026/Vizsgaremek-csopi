using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MusicFinder.Api.Controllers;

[ApiController]
[Route("api/search-history")]
public class SearchHistoryController : ControllerBase
{
    private readonly MySqlConnection _db;

    public SearchHistoryController(MySqlConnection db)
    {
        _db = db;
    }

    private async Task<long?> GetUserIdAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        email = email.Trim().ToLowerInvariant();
        await _db.OpenAsync();
        await using var cmd = _db.CreateCommand();
        cmd.CommandText = "SELECT id FROM users WHERE email = @email LIMIT 1";
        cmd.Parameters.AddWithValue("@email", email);
        var result = await cmd.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? null : Convert.ToInt64(result);
    }

    // POST /api/search-history
    [HttpPost]
    public async Task<IActionResult> SaveSearch([FromBody] SearchHistoryRequest req)
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        if (string.IsNullOrWhiteSpace(req.Query))
            return BadRequest(new { error = "query is required." });

        await using var cmd = _db.CreateCommand();
        cmd.CommandText = "INSERT INTO user_search_history (user_id, query) VALUES (@uid, @query)";
        cmd.Parameters.AddWithValue("@uid", userId.Value);
        cmd.Parameters.AddWithValue("@query", req.Query.Trim());
        await cmd.ExecuteNonQueryAsync();

        return Ok();
    }
}

public record SearchHistoryRequest(string Query);
