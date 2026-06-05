namespace KaraWeb.Shared.Models.Songs.Players;

/// <summary>
///     Represent a voice singer in the song
/// </summary>
public class SongPlayerDto
{
    /// <summary>
    /// The player's number
    /// </summary>
    public int PlayerNumber { get; init; }

    /// <summary>
    /// The player's name
    /// </summary>
    public string Name { get; init; }
}