using Microsoft.AspNetCore.Mvc;
using MusicFinder.Api.Helpers;
using MySqlConnector;

namespace MusicFinder.Api.Controllers;

[ApiController]
[Route("api/library")]
public class LibraryController : ControllerBase
{
    private readonly MySqlConnection _db;

    public LibraryController(MySqlConnection db)
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

    // GET /api/library
    [HttpGet]
    public async Task<IActionResult> GetLibrary()
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        var songs = new List<SongDto>();
        await using var cmd = _db.CreateCommand();
        cmd.CommandText = @"
            SELECT s.id, s.youtube_video_id, s.title, s.artist, s.artwork_url
            FROM user_library ul
            JOIN songs s ON s.id = ul.song_id
            WHERE ul.user_id = @uid
            ORDER BY ul.added_at";
        cmd.Parameters.AddWithValue("@uid", userId.Value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            songs.Add(DbHelper.MapSong(reader));

        return Ok(songs);
    }

    // POST /api/library
    [HttpPost]
    public async Task<IActionResult> AddToLibrary([FromBody] AddSongRequest req)
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        if (string.IsNullOrWhiteSpace(req.YoutubeVideoId))
            return BadRequest(new { error = "youtubeVideoId is required." });

        long songId;

        // Upsert into songs
        await using (var upsertCmd = _db.CreateCommand())
        {
            upsertCmd.CommandText = @"
                INSERT INTO songs (youtube_video_id, title, artist, artwork_url)
                VALUES (@vid, @title, @artist, @artwork)
                ON DUPLICATE KEY UPDATE
                    title = VALUES(title),
                    artist = VALUES(artist),
                    artwork_url = VALUES(artwork_url);
                SELECT id FROM songs WHERE youtube_video_id = @vid LIMIT 1;";
            upsertCmd.Parameters.AddWithValue("@vid", req.YoutubeVideoId);
            upsertCmd.Parameters.AddWithValue("@title", req.Title ?? "");
            upsertCmd.Parameters.AddWithValue("@artist", req.Artist ?? "");
            upsertCmd.Parameters.AddWithValue("@artwork", (object?)req.ArtworkUrl ?? DBNull.Value);
            songId = Convert.ToInt64(await upsertCmd.ExecuteScalarAsync());
        }

        // Insert into user_library if not exists
        await using (var libCmd = _db.CreateCommand())
        {
            libCmd.CommandText = @"
                INSERT IGNORE INTO user_library (user_id, song_id)
                VALUES (@uid, @sid)";
            libCmd.Parameters.AddWithValue("@uid", userId.Value);
            libCmd.Parameters.AddWithValue("@sid", songId);
            await libCmd.ExecuteNonQueryAsync();
        }

        return Ok(new { songId });
    }

    // DELETE /api/library/{songId}
    [HttpDelete("{songId:long}")]
    public async Task<IActionResult> RemoveFromLibrary(long songId)
    {
        var email = Request.Headers["X-User-Email"].FirstOrDefault();
        var userId = await GetUserIdAsync(email);
        if (userId == null) return Unauthorized(new { error = "X-User-Email header required." });

        await using var cmd = _db.CreateCommand();
        cmd.CommandText = "DELETE FROM user_library WHERE user_id = @uid AND song_id = @sid";
        cmd.Parameters.AddWithValue("@uid", userId.Value);
        cmd.Parameters.AddWithValue("@sid", songId);
        await cmd.ExecuteNonQueryAsync();

        return NoContent();
    }
}

public record AddSongRequest(
    string YoutubeVideoId,
    string? Title,
    string? Artist,
    string? ArtworkUrl);
