namespace KaraWeb.Shared.Models.Songs.Players;

/// <summary>
///     Represent a voice singer in the song
/// </summary>
public class SongPlayerDto
{
    /// <summary>
    /// The player's number
    /// </summary>
    public int PlayerNumber { get; set; }

    /// <summary>
    /// The player's name
    /// </summary>
    public string Name { get; set; }
}