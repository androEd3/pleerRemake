using Microsoft.Extensions.Caching.Memory;
using pleer.Models.Media;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace pleer.Models.Jamendo
{
    public interface IMusicService
    {
        // Поиск
        Task<SearchResult> SearchAsync(string query, int limit = 20);

        // Треки
        Task<Track> GetTrackAsync(int trackId);
        Task<IEnumerable<Track>> GetPopularTracksAsync(int limit = 50);
        Task<IEnumerable<Track>> GetTracksByGenreAsync(string genre, int limit = 50);
        Task<IEnumerable<Track>> GetNewReleasesAsync(int limit = 50);

        // Артисты
        Task<Artist> GetArtistAsync(int artistId);
        Task<IEnumerable<Track>> GetArtistTracksAsync(int artistId);
        Task<IEnumerable<Album>> GetArtistAlbumsAsync(int artistId);
        Task<IEnumerable<Artist>> GetPopularArtistsAsync(int limit = 20);

        // Альбомы
        Task<Album> GetAlbumAsync(int albumId);
        Task<IEnumerable<Track>> GetAlbumTracksAsync(int albumId);
        Task<IEnumerable<Album>> GetPopularAlbumsAsync(int limit = 20);


        // Стриминг
        Task<string> GetStreamUrlAsync(int trackId);

        // Жанры
        List<string> GetAvailableGenres();
    }

    public enum MusicSource
    {
        Local,
        Jamendo,
        Deezer
    }

    public class SearchResult
    {
        public List<Track> Tracks { get; set; } = new();
        public List<Album> Albums { get; set; } = new();
        public List<Artist> Artists { get; set; } = new();

        public int TotalCount => Tracks.Count + Artists.Count + Albums.Count;
        public bool HasResults => TotalCount > 0;
    }


    public class JamendoService : IMusicService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly IMemoryCache _cache;
        private const string BaseUrl = "https://api.jamendo.com/v3.0";

        public JamendoService(string clientId, IMemoryCache cache)
        {
            _clientId = clientId;
            _cache = cache;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        #region Поиск
        public async Task<SearchResult> SearchAsync(string query, int limit = 20)
        {
            var cacheKey = $"search_{query}_{limit}";
            if (_cache.TryGetValue(cacheKey, out SearchResult cached))
                return cached;

            var tracksTask = SearchTracksAsync(query, limit);
            var artistsTask = SearchArtistsAsync(query, 10);
            var albumsTask = SearchAlbumsAsync(query, 10);

            await Task.WhenAll(tracksTask, artistsTask, albumsTask);

            var result = new SearchResult
            {
                Tracks = (await tracksTask).ToList(),
                Artists = (await artistsTask).ToList(),
                Albums = (await albumsTask).ToList()
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
            return result;
        }

        private async Task<IEnumerable<Track>> SearchTracksAsync(string query, int limit)
        {
            var url = BuildUrl("tracks", new()
            {
                ["search"] = query,
                ["limit"] = limit.ToString(),
                ["include"] = "musicinfo",
                ["audioformat"] = "mp32"
            });

            var response = await FetchAsync<JamendoTracksResponse>(url);
            return response?.Results?.Select(MapToTrack) ?? [];
        }

        private async Task<IEnumerable<Artist>> SearchArtistsAsync(string query, int limit)
        {
            var url = BuildUrl("artists", new()
            {
                ["search"] = query,
                ["limit"] = limit.ToString()
            });

            var response = await FetchAsync<JamendoArtistsResponse>(url);
            return response?.Results?.Select(MapToArtist) ?? [];
        }

        private async Task<IEnumerable<Album>> SearchAlbumsAsync(string query, int limit)
        {
            var url = BuildUrl("albums", new()
            {
                ["search"] = query,
                ["limit"] = limit.ToString()
            });

            var response = await FetchAsync<JamendoAlbumsResponse>(url);
            return response?.Results?.Select(MapToAlbum) ?? [];
        }
        #endregion

        #region Треки
        public async Task<Track> GetTrackAsync(int trackId)
        {
            var cacheKey = $"track_{trackId}";
            if (_cache.TryGetValue(cacheKey, out Track cached))
                return cached;

            var url = BuildUrl("tracks", new()
            {
                ["id"] = trackId.ToString(),
                ["include"] = "musicinfo",
                ["audioformat"] = "mp32"
            });

            var response = await FetchAsync<JamendoTracksResponse>(url);
            var jamendoTrack = response?.Results?.FirstOrDefault();
            if (jamendoTrack == null) return null;

            var track = MapToTrack(jamendoTrack);
            _cache.Set(cacheKey, track, TimeSpan.FromHours(2));
            return track;
        }

        public async Task<IEnumerable<Track>> GetPopularTracksAsync(int limit = 50)
        {
            var cacheKey = $"popular_{limit}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Track> cached))
                return cached;

            var url = BuildUrl("tracks", new()
            {
                ["limit"] = limit.ToString(),
                ["order"] = "popularity_total",
                ["include"] = "musicinfo",
                ["audioformat"] = "mp32"
            });

            var response = await FetchAsync<JamendoTracksResponse>(url);
            var tracks = response?.Results?.Select(MapToTrack) ?? [];

            _cache.Set(cacheKey, tracks, TimeSpan.FromHours(1));
            return tracks;
        }

        public async Task<IEnumerable<Track>> GetTracksByGenreAsync(string genre, int limit = 50)
        {
            var cacheKey = $"genre_{genre}_{limit}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Track> cached))
                return cached;

            var url = BuildUrl("tracks", new()
            {
                ["tags"] = genre.ToLower(),
                ["limit"] = limit.ToString(),
                ["order"] = "popularity_total",
                ["include"] = "musicinfo",
                ["audioformat"] = "mp32"
            });

            var response = await FetchAsync<JamendoTracksResponse>(url);
            var tracks = response?.Results?.Select(MapToTrack) ?? [];

            _cache.Set(cacheKey, tracks, TimeSpan.FromHours(1));
            return tracks;
        }

        public async Task<IEnumerable<Track>> GetNewReleasesAsync(int limit = 50)
        {
            var cacheKey = $"new_{limit}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Track> cached))
                return cached;

            var url = BuildUrl("tracks", new()
            {
                ["limit"] = limit.ToString(),
                ["order"] = "releasedate_desc",
                ["include"] = "musicinfo",
                ["audioformat"] = "mp32"
            });

            var response = await FetchAsync<JamendoTracksResponse>(url);
            var tracks = response?.Results?.Select(MapToTrack) ?? [];

            _cache.Set(cacheKey, tracks, TimeSpan.FromHours(1));
            return tracks;
        }
        #endregion

        #region Артисты
        public async Task<Artist> GetArtistAsync(int artistId)
        {
            var cacheKey = $"artist_{artistId}";
            if (_cache.TryGetValue(cacheKey, out Artist cached))
                return cached;

            var url = BuildUrl("artists", new()
            {
                ["id"] = artistId.ToString()
            });

            var response = await FetchAsync<JamendoArtistsResponse>(url);
            var jamendoArtist = response?.Results?.FirstOrDefault();
            if (jamendoArtist == null) return null;

            var artist = MapToArtist(jamendoArtist);

            var tracksTask = GetArtistTracksAsync(artistId);
            var albumsTask = GetArtistAlbumsAsync(artistId);
            await Task.WhenAll(tracksTask, albumsTask);

            artist.TopTracks = (await tracksTask).Take(10).ToList();
            artist.Albums = (await albumsTask).ToList();

            _cache.Set(cacheKey, artist, TimeSpan.FromHours(2));
            return artist;
        }

        public async Task<IEnumerable<Track>> GetArtistTracksAsync(int artistId)
        {
            var cacheKey = $"artist_tracks_{artistId}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Track> cached))
                return cached;

            var url = BuildUrl("tracks", new()
            {
                ["artist_id"] = artistId.ToString(),
                ["limit"] = "100",
                ["order"] = "popularity_total",
                ["audioformat"] = "mp32"
            });

            var response = await FetchAsync<JamendoTracksResponse>(url);
            var tracks = response?.Results?.Select(MapToTrack) ?? [];

            _cache.Set(cacheKey, tracks, TimeSpan.FromHours(1));
            return tracks;
        }

        public async Task<IEnumerable<Album>> GetArtistAlbumsAsync(int artistId)
        {
            var cacheKey = $"artist_albums_{artistId}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Album> cached))
                return cached;

            var url = BuildUrl("albums", new()
            {
                ["artist_id"] = artistId.ToString(),
                ["limit"] = "50"
            });

            var response = await FetchAsync<JamendoAlbumsResponse>(url);
            var albums = response?.Results?.Select(MapToAlbum) ?? [];

            _cache.Set(cacheKey, albums, TimeSpan.FromHours(2));
            return albums;
        }

        public async Task<IEnumerable<Artist>> GetPopularArtistsAsync(int limit = 20)
        {
            var cacheKey = $"popular_artists_{limit}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Artist> cached))
                return cached;

            var url = BuildUrl("artists", new()
            {
                ["limit"] = limit.ToString(),
                ["order"] = "popularity_total"
            });

            var response = await FetchAsync<JamendoArtistsResponse>(url);
            var artists = response?.Results?.Select(MapToArtist) ?? [];

            _cache.Set(cacheKey, artists, TimeSpan.FromHours(2));
            return artists;
        }
        #endregion

        #region Альбомы
        public async Task<Album> GetAlbumAsync(int albumId)
        {
            var cacheKey = $"album_{albumId}";
            if (_cache.TryGetValue(cacheKey, out Album cached))
                return cached;

            var url = BuildUrl("albums", new()
            {
                ["id"] = albumId.ToString()
            });

            var response = await FetchAsync<JamendoAlbumsResponse>(url);
            var jamendoAlbum = response?.Results?.FirstOrDefault();
            if (jamendoAlbum == null) return null;

            var album = MapToAlbum(jamendoAlbum);
            album.Tracks = (await GetAlbumTracksAsync(albumId)).ToList();

            _cache.Set(cacheKey, album, TimeSpan.FromHours(2));
            return album;
        }

        public async Task<IEnumerable<Track>> GetAlbumTracksAsync(int albumId)
        {
            var cacheKey = $"album_tracks_{albumId}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Track> cached))
                return cached;

            var url = BuildUrl("tracks", new()
            {
                ["album_id"] = albumId.ToString(),
                ["audioformat"] = "mp32",
                ["order"] = "position"
            });

            var response = await FetchAsync<JamendoTracksResponse>(url);
            var tracks = response?.Results?.Select(MapToTrack) ?? [];

            _cache.Set(cacheKey, tracks, TimeSpan.FromHours(2));
            return tracks;
        }

        public async Task<IEnumerable<Album>> GetPopularAlbumsAsync(int limit = 20)
        {
            var cacheKey = $"popular_albums_{limit}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Album> cached))
                return cached;

            var url = BuildUrl("albums", new()
            {
                ["limit"] = limit.ToString(),
                ["order"] = "popularity_total"
            });

            var response = await FetchAsync<JamendoAlbumsResponse>(url);
            var albums = response?.Results?.Select(MapToAlbum) ?? [];

            _cache.Set(cacheKey, albums, TimeSpan.FromHours(2));
            return albums;
        }
        #endregion

        #region Стриминг
        public Task<string> GetStreamUrlAsync(int trackId)
        {
            var url = $"https://mp3l.jamendo.com/?trackid={trackId}&format=mp32";
            return Task.FromResult(url);
        }
        #endregion

        #region Жанры
        public List<string> GetAvailableGenres()
        {
            return
            [
                "rock", "pop", "electronic", "hiphop", "jazz",
            "classical", "metal", "folk", "ambient", "blues",
            "country", "reggae", "punk", "soul", "funk",
            "indie", "alternative", "dance", "house", "techno",
            "trance", "dubstep", "chillout", "lounge",
            "acoustic", "instrumental", "world", "latin"
            ];
        }
        #endregion

        #region Вспомогательные методы
        private string BuildUrl(string endpoint, Dictionary<string, string> parameters)
        {
            var queryParams = new List<string>
        {
            $"client_id={_clientId}",
            "format=json"
        };

            foreach (var param in parameters)
                queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value)}");

            return $"{BaseUrl}/{endpoint}/?{string.Join("&", queryParams)}";
        }

        private async Task<T> FetchAsync<T>(string url) where T : class
        {
            try
            {
                var json = await _httpClient.GetStringAsync(url);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Jamendo error: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Маппинг
        private Track MapToTrack(JamendoTrack t) => new()
        {
            Id = ParseInt(t.Id),
            Title = t.Name ?? "Unknown",
            Artist = t.ArtistName ?? "Unknown Artist",
            ArtistId = ParseInt(t.ArtistId),
            Album = t.AlbumName,
            AlbumId = ParseInt(t.AlbumId),
            CoverUrl = t.AlbumImage ?? t.Image,
            StreamUrl = t.Audio,
            Duration = TimeSpan.FromSeconds(t.Duration)
        };

        private Artist MapToArtist(JamendoArtist a) => new()
        {
            Id = ParseInt(a.Id),
            Name = a.Name ?? "Unknown Artist",
            ImageUrl = a.Image
        };

        private Album MapToAlbum(JamendoAlbum a) => new()
        {
            Id = ParseInt(a.Id),
            Title = a.Name ?? "Unknown Album",
            ArtistName = a.ArtistName,
            ArtistId = ParseInt(a.ArtistId),
            CoverUrl = a.Image,
            ReleaseDate = a.ReleaseDate
        };

        private int ParseInt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            return int.TryParse(value, out int result) ? result : 0;
        }
        #endregion
    }
}
