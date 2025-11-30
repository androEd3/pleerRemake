using System.Text.Json.Serialization;

namespace pleer.Models.Jamendo;

public class JamendoTracksResponse
{
    [JsonPropertyName("results")]
    public List<JamendoTrack> Results { get; set; }
}

public class JamendoArtistsResponse
{
    [JsonPropertyName("results")]
    public List<JamendoArtist> Results { get; set; }
}

public class JamendoAlbumsResponse
{
    [JsonPropertyName("results")]
    public List<JamendoAlbum> Results { get; set; }
}

public class JamendoTrack
{
    [JsonPropertyName("id")]
    public string Id { get; set; }  // ← string

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("artist_id")]
    public string ArtistId { get; set; }  // ← string

    [JsonPropertyName("artist_name")]
    public string ArtistName { get; set; }

    [JsonPropertyName("album_id")]
    public string AlbumId { get; set; }  // ← string

    [JsonPropertyName("album_name")]
    public string AlbumName { get; set; }

    [JsonPropertyName("album_image")]
    public string AlbumImage { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }

    [JsonPropertyName("audio")]
    public string Audio { get; set; }

    [JsonPropertyName("musicinfo")]
    public JamendoMusicInfo MusicInfo { get; set; }
}

public class JamendoMusicInfo
{
    [JsonPropertyName("tags")]
    public JamendoTags Tags { get; set; }
}

public class JamendoTags
{
    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; }
}

public class JamendoArtist
{
    [JsonPropertyName("id")]
    public string Id { get; set; }  // ← string

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }
}

public class JamendoAlbum
{
    [JsonPropertyName("id")]
    public string Id { get; set; }  // ← string

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("releasedate")]
    public string ReleaseDate { get; set; }

    [JsonPropertyName("artist_id")]
    public string ArtistId { get; set; }  // ← string

    [JsonPropertyName("artist_name")]
    public string ArtistName { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }
}