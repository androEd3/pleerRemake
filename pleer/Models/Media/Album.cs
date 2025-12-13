using pleer.Models.Media;

public class Album
{
    public string Id { get; set; }

    public string Title { get; set; }
    public string Artist { get; set; }

    public string ReleaseDate { get; set; }

    public string CoverUrl { get; set; }

    public List<Track> Tracks { get; set; } = new();
}
