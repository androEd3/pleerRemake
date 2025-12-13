using pleer.Models.Media;
using System.Net.Http;
using System.Text.Json;

namespace pleer.Models.IA
{
    public interface IMusicService
    {
        Task<SearchResult> SearchAsync(string query, int limit = 50);

        Task<List<string>> GetAvailableGenres();
        Task<List<Track>> GetTracksByGenreAsync(string genre, int limit = 50);

        Task<IReadOnlyList<Track>> GetPopularTracksAsync(int limit = 10);
        Task<IReadOnlyList<Album>> GetPopularAlbumsAsync(int limit = 10);

        Task<Album> GetAlbumAsync(string albumId);
        Task<List<Track>> GetAlbumTracksAsync(string albumId);

        Task<Track> GetTrackAsync(string trackId);
        Task<string> GetStreamUrlAsync(Track track);

        Task<Artist> GetArtistAsync(string artistName);

        Task<List<Track>> GetArtistTracksAsync(string artistId);
        Task<List<Album>> GetArtistAlbumsAsync(string artistId);
    }

    public class SearchResult
    {
        public List<Track> Tracks { get; set; } = new();
        public List<Album> Albums { get; set; } = new();
        public List<Artist> Artists { get; set; } = new();
    }

    public class InternetArchiveService : IMusicService
    {
        private readonly HttpClient _http = new();
        private const string Base = "https://archive.org";
        private const string SearchUrl = Base + "/advancedsearch.php";
        private const string MetadataUrl = Base + "/metadata";
        private const string DownloadUrl = Base + "/download";
        private const string ImgUrl = Base + "/services/img";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public InternetArchiveService()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("MusicApp/1.0");
        }

        public async Task<SearchResult> SearchAsync(string query, int limit = 50)
        {
            var q = string.IsNullOrWhiteSpace(query)
                ? "collection:(etree OR georgeblood) AND mediatype:audio"
                : $"({query}) AND collection:(etree OR georgeblood) AND mediatype:audio";

            var docs = await SearchItemsAsync(q, 100);

            var result = new SearchResult();
            var seenAlbums = new HashSet<string>();
            var seenArtists = new HashSet<string>();

            foreach (var doc in docs)
            {
                var meta = await GetMetadataAsync(doc.Identifier);
                if (meta?.Files == null) continue;

                var audioFiles = meta.Files.Where(f => IsAudio(f.Format)).OrderBy(f => TrackNum(f)).ToList();
                if (!audioFiles.Any()) continue;

                var genre = FirstGenre(meta.Metadata.Subject);
                var first = audioFiles.First();

                var title = GetString(meta.Metadata.Title);
                var creator = GetString(meta.Metadata.Creator);
                var releaseDate = GetYear(meta.Metadata.Year, meta.Metadata.Date);

                // Трек
                result.Tracks.Add(new Track
                {
                    Id = $"{doc.Identifier}/{first.Name}",
                    Title = first.Title ?? CleanName(first.Name),
                    Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown Artist",
                    Album = !string.IsNullOrWhiteSpace(title) ? title : "Unknown Album",
                    StreamUrl = BuildStreamUrl(doc.Identifier, first.Name),
                    CoverUrl = BuildCoverUrl(doc.Identifier),
                    Duration = ParseSec(first.Length),
                    Genre = genre
                });

                // Альбом
                if (seenAlbums.Add(doc.Identifier))
                {
                    result.Albums.Add(new Album
                    {
                        Id = doc.Identifier,
                        Title = !string.IsNullOrWhiteSpace(title) ? title : "Unknown Album",
                        Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown Artist",
                        ReleaseDate = releaseDate,
                        CoverUrl = BuildCoverUrl(doc.Identifier)
                    });
                }

                // Артист
                if (!string.IsNullOrWhiteSpace(creator) && seenArtists.Add(creator))
                {
                    result.Artists.Add(new Artist
                    {
                        Name = creator
                    });
                }
            }

            result.Tracks = result.Tracks.Take(limit).ToList();
            return result;
        }

        public Task<List<string>> GetAvailableGenres() => Task.FromResult(new List<string>
        {
            "rock", "jazz", "electronic", "blues", "classical",
            "folk", "metal", "reggae", "punk", "funk"
        });

        public async Task<List<Track>> GetTracksByGenreAsync(string genre, int limit = 50)
        {
            var q = $"subject:\"{genre}\" AND mediatype:audio";
            var docs = await SearchItemsAsync(q, limit);

            var tracks = new List<Track>();
            foreach (var doc in docs)
            {
                var track = await GetFirstTrackAsync(doc.Identifier);
                if (track != null) tracks.Add(track);
            }
            return tracks;
        }

        public async Task<IReadOnlyList<Track>> GetPopularTracksAsync(int limit = 10)
        {
            var q = "collection:etree AND mediatype:audio";
            var docs = await SearchItemsAsync(q, limit * 2);

            var tracks = new List<Track>();
            foreach (var doc in docs)
            {
                if (tracks.Count >= limit) break;
                var track = await GetFirstTrackAsync(doc.Identifier);
                if (track != null) tracks.Add(track);
            }
            return tracks;
        }

        public async Task<IReadOnlyList<Album>> GetPopularAlbumsAsync(int limit = 10)
        {
            var q = "collection:etree AND mediatype:audio";
            var docs = await SearchItemsAsync(q, limit);

            return docs.Select(d => new Album
            {
                Id = d.Identifier,
                Title = GetString(d.Title) is string t && !string.IsNullOrWhiteSpace(t) ? t : "Unknown Album",
                Artist = GetString(d.Creator) is string c && !string.IsNullOrWhiteSpace(c) ? c : "Unknown Artist",
                ReleaseDate = GetYear(d.Year, d.Date),
                CoverUrl = BuildCoverUrl(d.Identifier)
            }).ToList();
        }

        public async Task<Album> GetAlbumAsync(string albumId)
        {
            var meta = await GetMetadataAsync(albumId);
            if (meta?.Metadata == null) return null;

            var genre = FirstGenre(meta.Metadata.Subject);
            var title = GetString(meta.Metadata.Title);
            var creator = GetString(meta.Metadata.Creator);
            var releaseDate = GetYear(meta.Metadata.Year, meta.Metadata.Date);

            var tracks = meta.Files
                .Where(f => IsAudio(f.Format))
                .OrderBy(f => TrackNum(f))
                .Select(f => new Track
                {
                    Id = $"{albumId}/{f.Name}",
                    Title = f.Title ?? CleanName(f.Name),
                    Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown",
                    Album = title,
                    StreamUrl = BuildStreamUrl(albumId, f.Name),
                    CoverUrl = BuildCoverUrl(albumId),
                    Duration = ParseSec(f.Length),
                    Genre = genre
                }).ToList();

            return new Album
            {
                Id = albumId,
                Title = !string.IsNullOrWhiteSpace(title) ? title : "Unknown Album",
                Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown Artist",
                ReleaseDate = releaseDate,
                CoverUrl = BuildCoverUrl(albumId),
                Tracks = tracks
            };
        }

        public async Task<List<Track>> GetAlbumTracksAsync(string albumId)
        {
            var album = await GetAlbumAsync(albumId);
            return album?.Tracks ?? new List<Track>();
        }

        public async Task<Track> GetTrackAsync(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId)) return null;

            var parts = trackId.Split(['/'], 2);
            if (parts.Length != 2) return null;

            var identifier = parts[0];
            var fileName = Uri.UnescapeDataString(parts[1]);

            var meta = await GetMetadataAsync(identifier);
            var file = meta?.Files?.FirstOrDefault(f =>
                f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

            var title = GetString(meta.Metadata.Title);
            var creator = GetString(meta.Metadata.Creator);

            if (file == null || meta?.Metadata == null) return null;

            return new Track
            {
                Id = trackId,
                Title = file.Title ?? CleanName(file.Name),
                Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown",
                Album = title,
                StreamUrl = BuildStreamUrl(identifier, file.Name),
                CoverUrl = BuildCoverUrl(identifier),
                Duration = ParseSec(file.Length),
                Genre = FirstGenre(meta.Metadata.Subject)
            };
        }

        public Task<string> GetStreamUrlAsync(Track track)
            => Task.FromResult(track.StreamUrl);

        public async Task<Artist> GetArtistAsync(string artistName)
        {
            if (string.IsNullOrWhiteSpace(artistName))
                return new Artist { Name = "Unknown" };

            var q = $"creator:\"{artistName}\" AND mediatype:audio";
            var docs = await SearchItemsAsync(q, 50);

            var artist = new Artist
            {
                Name = artistName
            };

            if (!docs.Any()) return artist;

            artist.ProfileImageUrl = BuildCoverUrl(docs[0].Identifier);

            var popularTracks = new List<Track>();
            foreach (var doc in docs)
            {
                if (popularTracks.Count >= 6) break;
                var track = await GetFirstTrackAsync(doc.Identifier);
                if (track != null) popularTracks.Add(track);
            }
            artist.PopularTracks = popularTracks;

            // Все альбомы
            artist.Albums = docs.Select(d => new Album
            {
                Id = d.Identifier,
                Title = GetString(d.Title) ?? "Unknown Album",
                Artist = GetString(d.Creator) ?? artistName,
                ReleaseDate = GetYear(d.Year, d.Date),
                CoverUrl = BuildCoverUrl(d.Identifier)
            }).ToList();

            return artist;
        }

        public async Task<List<Track>> GetArtistTracksAsync(string artistId)
        {
            if (string.IsNullOrWhiteSpace(artistId))
                return new List<Track>();

            var q = $"creator:\"{artistId}\" AND mediatype:audio";
            var docs = await SearchItemsAsync(q, 30);

            var tracks = new List<Track>();
            foreach (var doc in docs)
            {
                var track = await GetFirstTrackAsync(doc.Identifier);
                if (track != null) tracks.Add(track);
            }

            return tracks;
        }

        public async Task<List<Album>> GetArtistAlbumsAsync(string artistId)
        {
            if (string.IsNullOrWhiteSpace(artistId))
                return new List<Album>();

            var q = $"creator:\"{artistId}\" AND mediatype:audio";
            var docs = await SearchItemsAsync(q, 50);

            return docs.Select(d => new Album
            {
                Id = d.Identifier,
                Title = GetString(d.Title) ?? "Unknown Album",
                Artist = GetString(d.Creator) ?? artistId,
                ReleaseDate = GetYear(d.Year, d.Date),
                CoverUrl = BuildCoverUrl(d.Identifier)
            }).ToList();
        }

        private async Task<List<IaDoc>> SearchItemsAsync(string query, int rows)
        {
            var url = $"{SearchUrl}?q={Uri.EscapeDataString(query)}" +
                      $"&fl[]=identifier,title,creator,date,year,subject" +
                      $"&rows={rows}" +
                      $"&sort[]=downloads desc" +
                      $"&output=json";

            var json = await _http.GetStringAsync(url);
            var resp = JsonSerializer.Deserialize<IaSearchResponse>(json, JsonOpts);

            return resp?.Response?.Docs ?? new List<IaDoc>();
        }

        private async Task<IaMetadataResponse> GetMetadataAsync(string identifier)
        {
            try
            {
                var json = await _http.GetStringAsync($"{MetadataUrl}/{identifier}");
                return JsonSerializer.Deserialize<IaMetadataResponse>(json, JsonOpts);
            }
            catch { return null; }
        }

        private async Task<Track> GetFirstTrackAsync(string identifier)
        {
            var meta = await GetMetadataAsync(identifier);
            var file = meta?.Files?
                .Where(f => IsAudio(f.Format))
                .OrderBy(f => TrackNum(f))
                .FirstOrDefault();

            if (file == null || meta?.Metadata == null) return null;

            var title = GetString(meta.Metadata.Title);
            var creator = GetString(meta.Metadata.Creator);

            return new Track
            {
                Id = $"{identifier}/{file.Name}",
                Title = file.Title ?? CleanName(file.Name),
                Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown",
                Album = title,
                StreamUrl = BuildStreamUrl(identifier, file.Name),
                CoverUrl = BuildCoverUrl(identifier),
                Duration = ParseSec(file.Length),
                Genre = FirstGenre(meta.Metadata.Subject)
            };
        }

        private string BuildStreamUrl(string id, string file)
            => $"{DownloadUrl}/{id}/{Uri.EscapeDataString(file)}";

        private string BuildCoverUrl(string id)
            => $"{ImgUrl}/{id}";

        private bool IsAudio(string f)
            => f?.Contains("MP3", StringComparison.OrdinalIgnoreCase) == true ||
               f?.Contains("Flac", StringComparison.OrdinalIgnoreCase) == true ||
               f?.Contains("Ogg", StringComparison.OrdinalIgnoreCase) == true;

        private int TrackNum(IaFile f)
            => int.TryParse(f.Track, out var n) ? n : int.MaxValue;

        private string CleanName(string n)
            => System.IO.Path.GetFileNameWithoutExtension(n);

        private TimeSpan? ParseSec(string s)
            => double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var sec)
                ? TimeSpan.FromSeconds(sec)
                : null;

        private string FirstGenre(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Array)
                return e.EnumerateArray().FirstOrDefault().GetString() ?? "";
            if (e.ValueKind == JsonValueKind.String)
                return e.GetString() ?? "";
            return "";
        }

        // ХЕЛПЕРЫ ДЛЯ ПАРСИНГА JsonElement
        private string GetString(JsonElement element)
        {
            try
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString() ?? "";

                    case JsonValueKind.Number:
                        return element.ToString();

                    case JsonValueKind.Array:
                        foreach (var item in element.EnumerateArray())
                        {
                            var val = GetString(item);
                            if (!string.IsNullOrWhiteSpace(val))
                                return val;
                        }
                        return "";

                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return "";

                    default:
                        return element.ToString();
                }
            }
            catch
            {
                return "";
            }
        }

        private string GetYear(JsonElement yearElement, JsonElement dateElement)
        {
            var year = GetString(yearElement);
            if (!string.IsNullOrWhiteSpace(year))
                return year;

            var date = GetString(dateElement);
            if (!string.IsNullOrWhiteSpace(date))
            {
                return date.Split(' ', '-', 'T')[0];
            }

            return "";
        }
    }
}
