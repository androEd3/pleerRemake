namespace pleer.Models.Media;

public class Track
{
    public int Id { get; set; }

    public string Title { get; set; }
    public string Artist { get; set; }
    public int ArtistId { get; set; }

    public string Album { get; set; }
    public int AlbumId { get; set; }

    public string CoverUrl { get; set; }
    public string StreamUrl { get; set; }

    public TimeSpan Duration { get; set; }

    public string DurationFormatted => Duration.ToString(@"m\:ss");
}
