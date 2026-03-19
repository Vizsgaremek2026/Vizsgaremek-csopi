using Microsoft.AspNetCore.Mvc;
using MusicFinder.Api.Helpers;
using MySqlConnector;

namespace MusicFinder.Api.Controllers;

[ApiController]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly MySqlConnection _db;

    public FavoritesController(MySqlConnection db)
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

    // GET /api/favorites
    [HttpGet]
    public async Task<IActionResult> GetFavorites()
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        var songs = new List<SongDto>();
        await using var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            SELECT s.id, s.youtube_video_id, s.title, s.artist, s.artwork_url
            FROM user_favorites uf
            JOIN songs s ON s.id = uf.song_id
            WHERE uf.user_id = @uid
            ORDER BY uf.created_at";
        cmd.Parameters.AddWithValue("@uid", userId.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            songs.Add(DbHelper.MapSong(reader));

        return Ok(songs);
    }

    // POST /api/favorites/{songId}
    [HttpPost("{songId:long}")]
    public async Task<IActionResult> AddFavorite(long songId)
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        await using var cmd = _db.CreateCommand();
        cmd.CommandText = "INSERT IGNORE INTO user_favorites (user_id, song_id) VALUES (@uid, @sid)";
        cmd.Parameters.AddWithValue("@uid", userId.Value);
        cmd.Parameters.AddWithValue("@sid", songId);
        await cmd.ExecuteNonQueryAsync();

        return Ok();
    }

    // DELETE /api/favorites/{songId}
    [HttpDelete("{songId:long}")]
    public async Task<IActionResult> RemoveFavorite(long songId)
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        await using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM user_favorites WHERE user_id = @uid AND song_id = @sid";
        cmd.Parameters.AddWithValue("@uid", userId.Value);
        cmd.Parameters.AddWithValue("@sid", songId);
        await cmd.ExecuteNonQueryAsync();

        return NoContent();
    }
}
