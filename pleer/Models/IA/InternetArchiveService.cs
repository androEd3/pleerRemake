using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace pleer.Models.IA
{
    public interface IMusicService
    {
        Task<SearchResult> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);

        Task<List<string>> GetAvailableGenres();
        Task<List<Track>> GetTracksByGenreAsync(string genre, int limit = 50, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Track>> GetPopularTracksAsync(int limit = 10);
        Task<IReadOnlyList<Album>> GetPopularAlbumsAsync(int limit = 10);

        Task<Album> GetAlbumAsync(string albumId);
        Task<List<Track>> GetAlbumTracksAsync(string albumId);

        Task<Track> GetTrackAsync(string trackId);
        Task<string> GetStreamUrlAsync(Track track);

        Task<Artist> GetArtistAsync(string artistName);

        Task<List<Track>> GetArtistTracksAsync(string artistId);
        Task<List<Album>> GetArtistAlbumsAsync(string artistId);

        Task<ServiceStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

    public class SearchResult
    {
        public List<Track> Tracks { get; set; } = new();
        public List<Album> Albums { get; set; } = new();
        public List<Artist> Artists { get; set; } = new();
    }

    public class ServiceStatistics
    {
        public long TotalAudioItems { get; set; }
        public long TotalMusicItems { get; set; }
        public Track MostPopularTrack { get; set; }
        public Album MostPopularAlbum { get; set; }
        public Artist MostPopularArtist { get; set; }
        public Dictionary<string, long> GenreStats { get; set; } = new();
    }

    public class InternetArchiveService : IMusicService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(5, 5);
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

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

        public async Task<SearchResult> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new SearchResult();

            var q = $"({query}) AND mediatype:audio";
            var docs = await SearchItemsAsync(q, limit, cancellationToken);

            if (!docs.Any())
                return new SearchResult();

            var trackTasks = docs.Take(limit).Select(doc =>
                GetFirstTrackWithSemaphoreAsync(doc.Identifier, cancellationToken));

            var tracks = (await Task.WhenAll(trackTasks))
                .Where(t => t != null)
                .ToList();

            // Группируем для альбомов и артистов
            var albums = tracks
                .Where(t => !string.IsNullOrWhiteSpace(t.AlbumId))
                .GroupBy(t => t.AlbumId)
                .Select(g => new Album
                {
                    Id = g.Key,
                    Title = g.First().Album ?? "Unknown Album",
                    Artist = g.First().Artist ?? "Unknown Artist",
                    CoverUrl = g.First().CoverUrl
                })
                .Take(20)
                .ToList();

            var artists = tracks
                .Where(t => !string.IsNullOrWhiteSpace(t.Artist))
                .GroupBy(t => t.Artist.ToLower())
                .Select(g => new Artist
                {
                    Name = g.First().Artist,
                    ProfileImageUrl = g.First().CoverUrl
                })
                .Take(20)
                .ToList();

            return new SearchResult
            {
                Tracks = tracks,
                Albums = albums,
                Artists = artists
            };
        }

        private async Task<Track> GetFirstTrackWithSemaphoreAsync(string identifier, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return await GetFirstTrackAsync(identifier, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading track from {identifier}: {ex.Message}");
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }


        public Task<List<string>> GetAvailableGenres() => Task.FromResult(new List<string>
        {
            "rock", "jazz", "electronic", "blues", "classical",
            "folk", "metal", "reggae", "punk", "funk"
        });

        public async Task<List<Track>> GetTracksByGenreAsync(string genre, int limit = 20, CancellationToken cancellationToken = default)
        {
            var q = $"subject:\"{genre}\" AND mediatype:audio";
            var docs = await SearchItemsAsync(q, limit, cancellationToken);

            var trackTasks = docs.Select(doc =>
                GetFirstTrackWithSemaphoreAsync(doc.Identifier, cancellationToken));

            var tracks = (await Task.WhenAll(trackTasks))
                .Where(t => t != null)
                .ToList();

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

            return new Album
            {
                Id = albumId,
                Title = !string.IsNullOrWhiteSpace(title) ? title : "Unknown Album",
                Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown Artist",
                ReleaseDate = releaseDate,
                CoverUrl = BuildCoverUrl(albumId)
            };
        }

        public async Task<List<Track>> GetAlbumTracksAsync(string albumId)
        {
            var meta = await GetMetadataAsync(albumId);
            if (meta?.Metadata == null) return new List<Track>();

            var genre = FirstGenre(meta.Metadata.Subject);
            var title = GetString(meta.Metadata.Title);
            var creator = GetString(meta.Metadata.Creator);

            var tracks = meta.Files
                .Where(f => IsAudio(f.Format))
                .GroupBy(f => GetTrackBaseName(f.Name))
                .Select(g => g.OrderByDescending(f => GetFormatPriority(f.Format)).First())
                .OrderBy(TrackNum)
                .Select(f => new Track
                {
                    Id = $"{albumId}/{f.Name}",
                    Title = f.Title ?? CleanName(f.Name),
                    Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown",
                    Album = title,
                    AlbumId = albumId,
                    StreamUrl = BuildStreamUrl(albumId, f.Name),
                    CoverUrl = BuildCoverUrl(albumId),
                    Duration = ParseSec(f.Length),
                    Genre = genre
                }).ToList();

            string GetTrackBaseName(string fileName)
            {
                var name = Path.GetFileNameWithoutExtension(fileName);
                return Regex.Replace(name, @"_?(vbr|128|192|256|320|flac|ogg)$", "", RegexOptions.IgnoreCase);
            }

            int GetFormatPriority(string format)
            {
                return format?.ToLower() switch
                {
                    "vbr mp3" => 5,
                    "mp3" => 4,
                    "ogg vorbis" => 3,
                    "flac" => 2,
                    _ => 1
                };
            }

            return tracks;
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

            var title = GetString(meta.Metadata.Title) ?? "Unknown";
            var creator = GetString(meta.Metadata.Creator) ?? "Unknown";

            if (file == null || meta?.Metadata == null) return null;

            return new Track
            {
                Id = trackId,
                Title = file.Title ?? CleanName(file.Name),
                Artist = !string.IsNullOrWhiteSpace(creator) ? creator : "Unknown",
                Album = title,
                AlbumId = identifier,
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

            return artist;
        }

        public async Task<List<Track>> GetArtistTracksAsync(string artistName)
        {
            if (string.IsNullOrWhiteSpace(artistName))
                return new List<Track>();

            var q = $"creator:\"{artistName}\" AND mediatype:audio";
            var docs = await SearchItemsAsync(q, 30);

            if (!docs.Any())
                return new List<Track>();

            var albumsToLoad = docs.Take(10).ToList();

            var tracksTasks = albumsToLoad.Select(async doc =>
            {
                try
                {
                    return await GetAlbumTracksAsync(doc.Identifier);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading tracks from {doc.Identifier}: {ex.Message}");
                    return new List<Track>();
                }
            });

            var tracksArrays = await Task.WhenAll(tracksTasks);
            var allTracks = tracksArrays.SelectMany(t => t).ToList();

            var uniqueTracks = allTracks
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .GroupBy(t => new
                {
                    Title = t.Title.Trim().ToLower(),
                    Duration = (int)(t.Duration?.TotalSeconds ?? 0)
                })
                .Select(g => g.First())
                .ToList();

            return uniqueTracks;
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

        private async Task<List<IaDoc>> SearchItemsAsync(string query, int rows, CancellationToken cancellationToken = default)
        {
            var url = $"{SearchUrl}?q={Uri.EscapeDataString(query)}" +
                      $"&fl[]=identifier,title,creator,date,year,subject" +
                      $"&rows={rows}" +
                      $"&sort[]=downloads desc" +
                      $"&output=json";

            try
            {
                var response = await _http.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var resp = JsonSerializer.Deserialize<IaSearchResponse>(json, JsonOpts);
                return resp?.Response?.Docs ?? new List<IaDoc>();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search error: {ex.Message}");
                return new List<IaDoc>();
            }
        }

        private async Task<IaMetadataResponse> GetMetadataAsync(string identifier, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _http.GetAsync($"{MetadataUrl}/{identifier}", cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IaMetadataResponse>(json, JsonOpts);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting metadata for {identifier}: {ex.Message}");
                return null;
            }
        }

        private async Task<Track> GetFirstTrackAsync(string identifier, CancellationToken cancellationToken = default)
        {
            var meta = await GetMetadataAsync(identifier, cancellationToken);
            var file = meta?.Files?
                .Where(f => IsAudio(f.Format))
                .OrderBy(TrackNum)
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
                AlbumId = identifier,
                StreamUrl = BuildStreamUrl(identifier, file.Name),
                CoverUrl = BuildCoverUrl(identifier),
                Duration = ParseSec(file.Length),
                Genre = FirstGenre(meta.Metadata.Subject)
            };
        }

        public async Task<ServiceStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new ServiceStatistics();

            try
            {
                var totalAudioTask = GetTotalCountAsync("mediatype:audio", cancellationToken);
                var totalMusicTask = GetTotalCountAsync("mediatype:audio AND subject:music", cancellationToken);
                var popularTrackTask = GetMostPopularTrackAsync(cancellationToken);
                var popularAlbumTask = GetMostPopularAlbumAsync(cancellationToken);
                var popularArtistTask = GetMostPopularArtistAsync(cancellationToken);
                var genreStatsTask = GetGenreStatisticsAsync(cancellationToken);

                await Task.WhenAll(
                    totalAudioTask,
                    totalMusicTask,
                    popularTrackTask,
                    popularAlbumTask,
                    popularArtistTask,
                    genreStatsTask
                );

                stats.TotalAudioItems = await totalAudioTask;
                stats.TotalMusicItems = await totalMusicTask;
                stats.MostPopularTrack = await popularTrackTask;
                stats.MostPopularAlbum = await popularAlbumTask;
                stats.MostPopularArtist = await popularArtistTask;
                stats.GenreStats = await genreStatsTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading statistics: {ex.Message}");
            }

            return stats;
        }

        private async Task<long> GetTotalCountAsync(string query, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{SearchUrl}?q={Uri.EscapeDataString(query)}" +
                          $"&rows=0" +
                          $"&output=json";

                var response = await _http.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var resp = JsonSerializer.Deserialize<IaSearchResponse>(json, JsonOpts);

                return resp?.Response?.NumFound ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting count: {ex.Message}");
                return 0;
            }
        }

        private async Task<Track> GetMostPopularTrackAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var q = "mediatype:audio AND subject:music";
                var docs = await SearchItemsAsync(q, 1, cancellationToken); 

                if (docs.Any())
                {
                    return await GetFirstTrackAsync(docs[0].Identifier, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting popular track: {ex.Message}");
            }

            return null;
        }

        private async Task<Album> GetMostPopularAlbumAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var q = "mediatype:audio AND subject:music";
                var docs = await SearchItemsAsync(q, 1, cancellationToken);

                if (docs.Any())
                {
                    var doc = docs[0];
                    return new Album
                    {
                        Id = doc.Identifier,
                        Title = GetString(doc.Title) ?? "Unknown Album",
                        Artist = GetString(doc.Creator) ?? "Unknown Artist",
                        ReleaseDate = GetYear(doc.Year, doc.Date),
                        CoverUrl = BuildCoverUrl(doc.Identifier)
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting popular album: {ex.Message}");
            }

            return null;
        }

        private async Task<Artist> GetMostPopularArtistAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var q = "mediatype:audio AND subject:music";
                var docs = await SearchItemsAsync(q, 10, cancellationToken);

                var artistName = docs
                    .Where(d => !string.IsNullOrWhiteSpace(GetString(d.Creator)))
                    .GroupBy(d => GetString(d.Creator))
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key;

                if (!string.IsNullOrWhiteSpace(artistName))
                {
                    return new Artist
                    {
                        Name = artistName,
                        ProfileImageUrl = BuildCoverUrl(docs.First(d => GetString(d.Creator) == artistName).Identifier)
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting popular artist: {ex.Message}");
            }

            return null;
        }

        private async Task<Dictionary<string, long>> GetGenreStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var genres = new[] { "rock", "pop", "jazz", "classical", "electronic", "hip-hop", "folk", "blues" };
            var stats = new Dictionary<string, long>();

            var tasks = genres.Select(async genre =>
            {
                var count = await GetTotalCountAsync($"subject:\"{genre}\" AND mediatype:audio", cancellationToken);
                return (genre, count);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (genre, count) in results.OrderByDescending(r => r.count))
            {
                stats[CapitalizeFirst(genre)] = count;
            }

            return stats;
        }

        private string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
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
            => Path.GetFileNameWithoutExtension(n);

        private TimeSpan? ParseSec(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            s = s.Trim();

            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var sec))
                return TimeSpan.FromSeconds(sec);

            // Попытка 2: Ручной парсинг MM:SS или H:MM:SS
            var parts = s.Split(':');

            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var min) &&
                int.TryParse(parts[1], out var secs))
            {
                return new TimeSpan(0, min, secs);
            }

            if (parts.Length == 3 &&
                int.TryParse(parts[0], out var hrs) &&
                int.TryParse(parts[1], out var mins) &&
                int.TryParse(parts[2], out var seconds))
            {
                return new TimeSpan(hrs, mins, seconds);
            }

            Debug.WriteLine($"Не удалось распарсить длительность: '{s}'");
            return null;
        }
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
