using pleer.Models.Jamendo;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Windows;
using System.Diagnostics;
using System.Windows.Controls;

namespace pleer.Resources.Pages.UserPages
{
    /// <summary>
    /// Логика взаимодействия для HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        ListenerMainWindow _listenerMain;
        IMusicService _musicService;

        Listener _listener;
        Artist _artist;

        public HomePage(ListenerMainWindow listenerMain, Listener listener)
        {
            InitializeComponent();

            _listenerMain = listenerMain;
            _listener = listener;

            _musicService = listenerMain._musicService;

            Loaded += async (s, e) => await LoadPopularContentAsync();
        }

        // Страница артиста (без слушателя)
        public HomePage(ListenerMainWindow listenerMain, Artist artist)
        {
            InitializeComponent();

            _listenerMain = listenerMain;
            _artist = artist;
            _musicService = listenerMain._musicService;

            Loaded += async (s, e) => await LoadArtistContentAsync();
        }

        // Страница артиста (со слушателя)
        public HomePage(ListenerMainWindow listenerMain, Artist artist, Listener listener)
        {
            InitializeComponent();

            _listenerMain = listenerMain;
            _artist = artist;
            _listener = listener;

            _musicService = listenerMain._musicService;

            Loaded += async (s, e) => await LoadArtistContentAsync();
        }

        #region Популярный контент (домашняя страница)

        async Task LoadPopularContentAsync()
        {
            try
            {
                var tracksTask = LoadPopularTracksAsync();
                var albumsTask = LoadPopularAlbumsAsync();

                await Task.WhenAll(tracksTask, albumsTask);
            }
            catch { }
        }

        async Task LoadPopularTracksAsync()
        {
            PopularSongsControl.Children.Clear();

            try
            {
                var tracks = await _musicService.GetPopularTracksAsync(10);
                var trackList = tracks.ToList();

                if (!trackList.Any())
                {
                    UIElementsFactory.NoContent("Пока что нет песен", PopularSongsControl);
                    return;
                }

                for (int i = 0; i < trackList.Count; i++)
                {
                    Border card;

                    card = _listener != null ?
                        UIElementsFactory.CreateTrackCard(trackList[i], _listener, i, _listenerMain.TrackCard_Click) :
                        UIElementsFactory.CreateTrackCard(trackList[i], i, _listenerMain.TrackCard_Click);

                    PopularSongsControl.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading tracks: {ex.Message}");
                UIElementsFactory.NoContent("Ошибка загрузки", PopularSongsControl);
            }
        }

        async Task LoadPopularAlbumsAsync()
        {
            PopularAlbumsControl.Items.Clear();

            try
            {
                var albums = await _musicService.GetPopularAlbumsAsync(6);
                var albumList = albums.ToList();

                if (!albumList.Any())
                {
                    UIElementsFactory.NoContent("Пока что нет выпусков", NotFoundAlbumsPanel);
                    return;
                }

                foreach (var album in albumList)
                {
                    var card = UIElementsFactory.CreateAlbumCard(album, _listenerMain.AlbumCard_Click);
                    PopularAlbumsControl.Items.Add(card);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading albums: {ex.Message}");
                UIElementsFactory.NoContent("Ошибка загрузки", NotFoundAlbumsPanel);
            }
        }
        #endregion

        #region Контент артиста
        async Task LoadArtistContentAsync()
        {
            if (_artist == null) return;

            try
            {
                var tracksTask = LoadArtistTracksAsync();
                var albumsTask = LoadArtistAlbumsAsync();

                await Task.WhenAll(tracksTask, albumsTask);
            }
            catch { }
        }

        async Task LoadArtistTracksAsync()
        {
            PopularSongsControl.Children.Clear();

            try
            {
                var tracks = await _musicService.GetArtistTracksAsync(_artist.Id);
                var trackList = tracks.Take(10).ToList();

                if (!trackList.Any())
                {
                    UIElementsFactory.NoContent("Пока что нет песен", PopularSongsControl);
                    return;
                }

                for (int i = 0; i < trackList.Count; i++)
                {
                    Border card;

                    card = _listener != null ?
                        UIElementsFactory.CreateTrackCard(trackList[i], _listener, i, _listenerMain.TrackCard_Click) :
                        UIElementsFactory.CreateTrackCard(trackList[i], i, _listenerMain.TrackCard_Click);

                    PopularSongsControl.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading artist tracks: {ex.Message}");
                UIElementsFactory.NoContent("Ошибка загрузки", PopularSongsControl);
            }
        }

        async Task LoadArtistAlbumsAsync()
        {
            PopularAlbumsControl.Items.Clear();

            try
            {
                var albums = await _musicService.GetArtistAlbumsAsync(_artist.Id);
                var albumList = albums.ToList();

                if (!albumList.Any())
                {
                    UIElementsFactory.NoContent("Пока что нет выпусков", NotFoundAlbumsPanel);
                    return;
                }

                foreach (var album in albumList)
                {
                    var card = UIElementsFactory.CreateAlbumCard(album, _listenerMain.AlbumCard_Click);
                    PopularAlbumsControl.Items.Add(card);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading artist albums: {ex.Message}");
                UIElementsFactory.NoContent("Ошибка загрузки", NotFoundAlbumsPanel);
            }
        }
        #endregion
    }
}
