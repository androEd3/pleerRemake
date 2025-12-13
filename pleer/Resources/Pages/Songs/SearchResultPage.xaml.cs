using pleer.Models.IA;
using pleer.Models.Media;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Windows;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace pleer.Resources.Pages.Songs
{
    public partial class SearchResultPage : Page
    {
        ListenerMainWindow _listenerMain;
        Listener _listener;
        IMusicService _musicService;

        string _searchQuery;
        string _selectedGenre;

        enum SearchType
        {
            Tracks,
            Albums,
            Artists
        }

        SearchType _currentSearchType = SearchType.Tracks;

        public SearchResultPage(
            ListenerMainWindow mainWindow,
            Listener listener,
            string searchQuery)
        {
            InitializeComponent();

            _listenerMain = mainWindow;
            _listener = listener;
            _searchQuery = searchQuery;
            _musicService = _listenerMain._musicService;

            Loaded += SearchResult_Loaded;
        }

        async void SearchResult_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGenres();
            await LoadTrackSearchResults();
        }

        #region Загрузка жанров
        void LoadGenres()
        {
            try
            {
                var genres = _musicService.GetAvailableGenres();

                var genresList = new List<string> { "Все жанры" };
                genresList.AddRange(genres.Result.Select(CapitalizeFirst));

                GenreComboBox.ItemsSource = genresList;
                GenreComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading genres: {ex.Message}");
            }
        }

        string CapitalizeFirst(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
        #endregion

        #region Поиск
        async Task LoadTrackSearchResults()
        {
            _currentSearchType = SearchType.Tracks;

            try
            {
                if (_musicService == null) return;

                List<Track> tracks;

                if (string.IsNullOrEmpty(_selectedGenre) || _selectedGenre == "Все жанры")
                {
                    // Обычный поиск
                    var results = await _musicService.SearchAsync(_searchQuery, 50);
                    tracks = results.Tracks;
                }
                else
                {
                    var genreTracks = await _musicService.GetTracksByGenreAsync(_selectedGenre.ToLower(), 50);

                    tracks = genreTracks
                        .Where(t =>
                            t.Title.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                            t.Artist.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var searchResult = new SearchResult { Tracks = tracks };
                DisplaySongsResults(searchResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search error: {ex.Message}");
            }
        }

        async Task LoadAlbumsSearchResults()
        {
            _currentSearchType = SearchType.Albums;

            try
            {
                if (_musicService == null) return;

                SearchResult results;

                if (string.IsNullOrEmpty(_selectedGenre) || _selectedGenre == "Все жанры")
                {
                    results = await _musicService.SearchAsync(_searchQuery, 50);
                }
                else
                {
                    var tracks = await _musicService.GetTracksByGenreAsync(_selectedGenre.ToLower(), 100);

                    var albums = tracks
                        .Where(t => t.Album != null)
                        .GroupBy(t => t.Album)
                        .Select(g => new Album
                        {
                            Id = g.Key,
                            Title = g.First().Album,
                            Artist = g.First().Artist,
                            CoverUrl = g.First().CoverUrl
                        })
                        .Where(a =>
                            a.Title.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                            a.Artist.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    results = new SearchResult { Albums = albums };
                }

                DisplayAlbumsResults(results);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search error: {ex.Message}");
            }
        }

        async Task LoadArtistsSearchResults()
        {
            _currentSearchType = SearchType.Artists;

            try
            {
                if (_musicService == null) return;

                SearchResult results;

                if (string.IsNullOrEmpty(_selectedGenre) || _selectedGenre == "Все жанры")
                {
                    results = await _musicService.SearchAsync(_searchQuery, 50);
                }
                else
                {
                    var tracks = await _musicService.GetTracksByGenreAsync(_selectedGenre.ToLower(), 100);

                    var artists = tracks
                        .Where(t => t.Artist != null)
                        .GroupBy(t => t.Artist)
                        .Select(g => new Artist
                        {
                            Name = g.First().Artist
                        })
                        .Where(a =>
                            a.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var fullArtists = new List<Artist>();
                    foreach (var artist in artists.Take(20))
                    {
                        try
                        {
                            var fullArtist = await _musicService.GetArtistAsync(artist.Name);
                            if (fullArtist != null)
                                fullArtists.Add(fullArtist);
                        }
                        catch { }
                    }

                    results = new SearchResult { Artists = fullArtists };
                }

                DisplayArtistsResults(results);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search error: {ex.Message}");
            }
        }
        #endregion

        #region Отображение результатов
        void DisplaySongsResults(SearchResult results)
        {
            ContentList.Children.Clear();

            SetActiveButton(SearchSongsButton);

            if (results.Tracks.Any())
            {
                for (int i = 0; i < results.Tracks.Count; i++)
                {
                    var card = _listener != null
                        ? UIElementsFactory.CreateTrackCard(results.Tracks[i], _listener, i, _listenerMain.TrackCard_Click)
                        : UIElementsFactory.CreateTrackCard(results.Tracks[i], i, _listenerMain.TrackCard_Click);

                    ContentList.Children.Add(card);
                }
            }
            else
            {
                UIElementsFactory.NoContent("По запросу ничего не найдено", ContentList);
            }
        }

        void DisplayAlbumsResults(SearchResult results)
        {
            ContentList.Children.Clear();

            SetActiveButton(SearchPlaylistsButton);

            if (results.Albums.Any())
            {
                foreach (var album in results.Albums)
                {
                    var card = UIElementsFactory.CreateAlbumCard(album, _listenerMain.AlbumCard_Click);
                    ContentList.Children.Add(card);
                }
            }
            else
            {
                UIElementsFactory.NoContent("По запросу ничего не найдено", ContentList);
            }
        }

        void DisplayArtistsResults(SearchResult results)
        {
            ContentList.Children.Clear();

            SetActiveButton(SearchArtistsButton);

            if (results.Artists.Any())
            {
                foreach (var artist in results.Artists)
                {
                    var card = UIElementsFactory.CreateArtistCard(artist, _listenerMain.ArtistCard_Click);
                    ContentList.Children.Add(card);
                }
            }
            else
            {
                UIElementsFactory.NoContent("По запросу ничего не найдено", ContentList);
            }
        }

        void SetActiveButton(Button activeButton)
        {
            var buttons = new[] { SearchSongsButton, SearchPlaylistsButton, SearchArtistsButton };

            foreach (var btn in buttons)
            {
                if (btn == activeButton)
                {
                    btn.Background = UIElementsFactory.ColorConvert("#eee");
                    btn.Foreground = UIElementsFactory.ColorConvert("#333");
                }
                else
                {
                    btn.Background = UIElementsFactory.ColorConvert("#333");
                    btn.Foreground = UIElementsFactory.ColorConvert("#eee");
                }
            }
        }
        #endregion

        #region Обработчики событий
        private async void SearchTracksButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadTrackSearchResults();
        }

        private async void SearchAlbumsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadAlbumsSearchResults();
        }

        private async void SearchArtistsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadArtistsSearchResults();
        }

        private async void GenreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GenreComboBox.SelectedItem == null) return;

            _selectedGenre = GenreComboBox.SelectedItem.ToString();

            switch (_currentSearchType)
            {
                case SearchType.Tracks:
                    await LoadTrackSearchResults();
                    break;
                case SearchType.Albums:
                    await LoadAlbumsSearchResults();
                    break;
                case SearchType.Artists:
                    await LoadArtistsSearchResults();
                    break;
            }
        }

        private async void ClearGenreButton_Click(object sender, RoutedEventArgs e)
        {
            GenreComboBox.SelectedIndex = 0; // Все жанры
        }
        #endregion
    }
}
