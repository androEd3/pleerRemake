using pleer.Models.Media;

public class Album
{
    public int Id { get; set; }

    public string Title { get; set; }
    public string ArtistName { get; set; }
    public int ArtistId { get; set; }

    public string CoverUrl { get; set; }
    public string ReleaseDate { get; set; }

    public List<Track> Tracks { get; set; } = new();
}
