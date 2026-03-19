using MySqlConnector;

namespace MusicFinder.Api.Helpers;

public static class DbHelper
{
    /// <summary>
    /// Resolves (or creates) a user by email. Returns the user's DB id.
    /// </summary>
    public static async Task<long> ResolveUserIdAsync(MySqlConnection db, string email)
    {
        await db.OpenAsync();

        await using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT id FROM users WHERE email = @email LIMIT 1";
        cmd.Parameters.AddWithValue("@email", email);

        var result = await cmd.ExecuteScalarAsync();
        if (result != null && result != DBNull.Value)
            return Convert.ToInt64(result);

        throw new InvalidOperationException($"User not found: {email}");
    }

    /// <summary>
    /// Maps a DB row (from songs + optional user_resume join) to the frontend-friendly DTO.
    /// </summary>
    public static SongDto MapSong(MySqlDataReader reader)
    {
        return new SongDto
        {
            SongId = reader.GetInt64("id"),
            YoutubeVideoId = reader.IsDBNull(reader.GetOrdinal("youtube_video_id"))
                ? null
                : reader.GetString("youtube_video_id"),
            Title = reader.GetString("title"),
            Artist = reader.GetString("artist"),
            ArtworkUrl = reader.IsDBNull(reader.GetOrdinal("artwork_url"))
                ? null
                : reader.GetString("artwork_url"),
        };
    }
}

public record SongDto
{
    public long SongId { get; init; }
    public string? YoutubeVideoId { get; init; }
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    public string? ArtworkUrl { get; init; }

    /// <summary>Frontend-compatible id string: "yt_&lt;videoId&gt;"</summary>
    public string FrontendId => YoutubeVideoId is { Length: > 0 } vid ? $"yt_{vid}" : $"db_{SongId}";
}
