using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MusicFinder.Api.Controllers;

[ApiController]
[Route("api/resume")]
public class ResumeController : ControllerBase
{
    private readonly MySqlConnection _db;

    public ResumeController(MySqlConnection db)
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

    // GET /api/resume  ->  { "songId": positionSeconds, ... }
    [HttpGet]
    public async Task<IActionResult> GetResumeTimes()
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        var result = new Dictionary<string, int>();

        await using var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            SELECT ur.song_id, s.youtube_video_id, ur.position_seconds
            FROM user_resume ur
            JOIN songs s ON s.id = ur.song_id
            WHERE ur.user_id = @uid";
        cmd.Parameters.AddWithValue("@uid", userId.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var ytId = reader.IsDBNull(reader.GetOrdinal("youtube_video_id"))
                ? null
                : reader.GetString("youtube_video_id");
            var key = ytId is { Length: > 0 } ? $"yt_{ytId}" : $"db_{reader.GetInt64("song_id")}";
            result[key] = reader.GetInt32("position_seconds");
        }

        return Ok(result);
    }

    // PUT /api/resume/{songId}
    [HttpPut("{songId:long}")]
    public async Task<IActionResult> SaveResumeTime(long songId, [FromBody] ResumeRequest req)
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        await using var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO user_resume (user_id, song_id, position_seconds)
            VALUES (@uid, @sid, @pos)
            ON DUPLICATE KEY UPDATE position_seconds = VALUES(position_seconds)";
        cmd.Parameters.AddWithValue("@uid", userId.Value);
        cmd.Parameters.AddWithValue("@sid", songId);
        cmd.Parameters.AddWithValue("@pos", req.PositionSeconds);
        await cmd.ExecuteNonQueryAsync();

        return Ok();
    }
}

public record ResumeRequest(int PositionSeconds);
