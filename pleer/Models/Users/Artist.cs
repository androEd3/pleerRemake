using pleer.Models.Media;

public class Artist
{
    public int Id { get; set; }

    public string Name { get; set; }
    public string ImageUrl { get; set; }

    public List<Track> TopTracks { get; set; } = new();
    public List<Album> Albums { get; set; } = new();
}
