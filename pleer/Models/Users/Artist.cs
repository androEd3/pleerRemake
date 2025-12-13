using pleer.Models.Media;

public class Artist
{
    public string Name { get; set; }
    public string ProfileImageUrl { get; set; }

    public List<Track> PopularTracks { get; set; } = new();
    public List<Album> Albums { get; set; } = new();
}
