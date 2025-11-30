using pleer.Models.Jamendo;
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
            await LoadTrackSearchResults();
        }

        async Task LoadTrackSearchResults()
        {
            try
            {
                if (_musicService != null)
                {
                    var streamingResults = await _musicService.SearchAsync(_searchQuery, 20);
                    DisplaySongsResults(streamingResults);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search error: {ex.Message}");
            }
        }

        async Task LoadAlbumsSearchResults()
        {
            try
            {
                if (_musicService != null)
                {
                    var streamingResults = await _musicService.SearchAsync(_searchQuery, 20);
                    DisplayAlbumsResults(streamingResults);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search error: {ex.Message}");
            }
        }

        async Task LoadArtistsSearchResults()
        {
            try
            {
                if (_musicService != null)
                {
                    var streamingResults = await _musicService.SearchAsync(_searchQuery, 20);
                    DisplayArtistsResults(streamingResults);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Search error: {ex.Message}");
            }
        }

        void DisplaySongsResults(SearchResult results)
        {
            ContentList.Children.Clear();

            SearchSongsButton.Background = UIElementsFactory.ColorConvert("#eee");
            SearchSongsButton.Foreground = UIElementsFactory.ColorConvert("#333");

            SearchPlaylistsButton.Background = UIElementsFactory.ColorConvert("#333");
            SearchPlaylistsButton.Foreground = UIElementsFactory.ColorConvert("#eee");

            SearchArtistsButton.Background = UIElementsFactory.ColorConvert("#333");
            SearchArtistsButton.Foreground = UIElementsFactory.ColorConvert("#eee");

            if (results.Tracks.Any())
            {
                for (int i = 0; i < results.Tracks.Count; i++)
                {
                    Border card;

                    card = _listener != null ?
                        card = UIElementsFactory.CreateTrackCard(results.Tracks[i], _listener, i, _listenerMain.TrackCard_Click) :
                        card = UIElementsFactory.CreateTrackCard(results.Tracks[i], i, _listenerMain.TrackCard_Click);
                    ContentList.Children.Add(card);
                }
            }
            else
            {
                UIElementsFactory.NoContent("По запросу ничего не найдено", ContentList);
                return;
            }
        }

        void DisplayAlbumsResults(SearchResult results)
        {
            ContentList.Children.Clear();

            SearchSongsButton.Background = UIElementsFactory.ColorConvert("#333");
            SearchSongsButton.Foreground = UIElementsFactory.ColorConvert("#eee");

            SearchPlaylistsButton.Background = UIElementsFactory.ColorConvert("#eee");
            SearchPlaylistsButton.Foreground = UIElementsFactory.ColorConvert("#333");

            SearchArtistsButton.Background = UIElementsFactory.ColorConvert("#333");
            SearchArtistsButton.Foreground = UIElementsFactory.ColorConvert("#eee");

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
                return;
            }
        }

        void DisplayArtistsResults(SearchResult results)
        {
            ContentList.Children.Clear();

            SearchSongsButton.Background = UIElementsFactory.ColorConvert("#333");
            SearchSongsButton.Foreground = UIElementsFactory.ColorConvert("#eee");

            SearchPlaylistsButton.Background = UIElementsFactory.ColorConvert("#333");
            SearchPlaylistsButton.Foreground = UIElementsFactory.ColorConvert("#eee");

            SearchArtistsButton.Background = UIElementsFactory.ColorConvert("#eee");
            SearchArtistsButton.Foreground = UIElementsFactory.ColorConvert("#333");

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
                return;
            }
        }

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
    }
}
