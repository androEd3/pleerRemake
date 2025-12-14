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
        private CancellationTokenSource _searchCancellation;

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

            Loaded += async (sender, e) => await SearchResult_Loaded(sender, e);
        }

        async Task SearchResult_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadGenresAsync();
            await LoadTrackSearchResults();
        }

        #region Загрузка жанров
        async Task LoadGenresAsync()
        {
            try
            {
                var genres = await _musicService.GetAvailableGenres();

                var genresList = new List<string> { "Все жанры" };
                genresList.AddRange(genres.Select(CapitalizeFirst));

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

            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();
            var token = _searchCancellation.Token;

            try
            {
                ShowLoading();

                if (_musicService == null) return;

                List<Track> tracks;

                if (string.IsNullOrEmpty(_selectedGenre) || _selectedGenre == "Все жанры")
                {
                    var results = await _musicService.SearchAsync(_searchQuery, 20, token);
                    tracks = results.Tracks;
                }
                else
                {
                    var genreTracks = await _musicService.GetTracksByGenreAsync(_selectedGenre.ToLower(), 20, token);

                    tracks = genreTracks
                        .Where(t =>
                            (t.Title?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (t.Artist?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();
                }

                if (token.IsCancellationRequested) return;

                var searchResult = new SearchResult { Tracks = tracks };
                DisplaySongsResults(searchResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search error: {ex.Message}");
                if (!token.IsCancellationRequested)
                    ShowError($"Ошибка поиска: {ex.Message}");
            }
        }

        async Task LoadAlbumsSearchResults()
        {
            _currentSearchType = SearchType.Albums;

            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();
            var token = _searchCancellation.Token;

            try
            {
                ShowLoading();

                if (_musicService == null) return;

                SearchResult results;

                if (string.IsNullOrEmpty(_selectedGenre) || _selectedGenre == "Все жанры")
                {
                    results = await _musicService.SearchAsync(_searchQuery, 20, token);
                }
                else
                {
                    var tracks = await _musicService.GetTracksByGenreAsync(_selectedGenre.ToLower(), 30, token);

                    var albums = tracks
                        .Where(t => t.Album != null)
                        .GroupBy(t => t.AlbumId ?? t.Album)
                        .Select(g => new Album
                        {
                            Id = g.First().AlbumId ?? g.Key,
                            Title = g.First().Album,
                            Artist = g.First().Artist,
                            CoverUrl = g.First().CoverUrl
                        })
                        .Where(a =>
                            (a.Title?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (a.Artist?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();

                    results = new SearchResult { Albums = albums };
                }

                if (token.IsCancellationRequested) return;

                DisplayAlbumsResults(results);
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    ShowError($"Ошибка поиска: {ex.Message}");
            }
        }

        async Task LoadArtistsSearchResults()
        {
            _currentSearchType = SearchType.Artists;

            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();
            var token = _searchCancellation.Token;

            try
            {
                ShowLoading();

                if (_musicService == null) return;

                SearchResult results;

                if (string.IsNullOrEmpty(_selectedGenre) || _selectedGenre == "Все жанры")
                {
                    results = await _musicService.SearchAsync(_searchQuery, 20, token);
                }
                else
                {
                    var tracks = await _musicService.GetTracksByGenreAsync(_selectedGenre.ToLower(), 30, token);

                    var artists = tracks
                        .Where(t => !string.IsNullOrWhiteSpace(t.Artist))
                        .GroupBy(t => t.Artist.ToLower())
                        .Select(g => new Artist
                        {
                            Name = g.First().Artist,
                            ProfileImageUrl = g.First().CoverUrl
                        })
                        .Where(a => a.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                        .Take(20)
                        .ToList();

                    results = new SearchResult { Artists = artists };
                }

                if (token.IsCancellationRequested) return;

                DisplayArtistsResults(results);
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    ShowError($"Ошибка поиска: {ex.Message}");
            }
        }
        #endregion

        #region Отображение результатов
        void ShowLoading()
        {
            ContentList.Children.Clear();
            UIElementsFactory.NoContent("Загрузка...", ContentList);
        }

        void ShowError(string message)
        {
            ContentList.Children.Clear();
            UIElementsFactory.NoContent(message, ContentList);
        }

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
                UIElementsFactory.NoContent(
                    "По запросу ничего не найдено.\n" +
                    "Попробуйте поиск на английском языке.",
                    ContentList);
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
                UIElementsFactory.NoContent(
                    "По запросу ничего не найдено.\n" +
                    "Попробуйте поиск на английском языке.",
                    ContentList);
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
                UIElementsFactory.NoContent(
                    "По запросу ничего не найдено.\n" +
                    "Попробуйте поиск на английском языке.",
                    ContentList);
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
        #endregion
    }
}
