namespace pleer.Models.Media;

public class Track
{
    public string Id { get; set; }

    public string Title { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }

    public string StreamUrl { get; set; }
    public string CoverUrl { get; set; }

    public string Genre { get; set; }
    public TimeSpan? Duration { get; set; }
}