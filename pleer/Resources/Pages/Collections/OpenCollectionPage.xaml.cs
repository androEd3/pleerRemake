using pleer.Models.DatabaseContext;
using pleer.Models.IA;
using pleer.Models.Media;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Pages.UserPages;
using pleer.Resources.Windows;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace pleer.Resources.Pages.Collections
{
    /// <summary>
    /// Логика взаимодействия для OpenCollectionPage.xaml
    /// </summary>
    public partial class OpenCollectionPage : Page
    {
        DBContext _context = new();

        ListenerMainWindow _listenerMain;
        CollectionsListPage _collectionMain;
        IMusicService _musicService;

        Album _album;
        Playlist _playlist;
        Listener _listener;

        public OpenCollectionPage(
            CollectionsListPage collectionMain,
            ListenerMainWindow listenerMain,
            Playlist playlist,
            Listener listener)
        {
            InitializeComponent();

            _listenerMain = listenerMain;
            _collectionMain = collectionMain;
            _listener = listener;
            _playlist = playlist;
            _musicService = listenerMain._musicService;

            Loaded += async (s, e) => await LoadPlaylistDataAsync();
        }

        public OpenCollectionPage(
            ListenerMainWindow listenerMain,
            Album album,
            Listener listener)
        {
            InitializeComponent();

            _listenerMain = listenerMain;
            _listener = listener;
            _album = album;
            _musicService = listenerMain._musicService;

            PlaylistFunctionalPanel.Visibility = Visibility.Collapsed;

            Loaded += async (s, e) => await LoadAlbumDataAsync();
            Unloaded += (s, e) => _listenerMain._currentAlbum = null;
        }


        #region === ЗАГРУЗКА АЛЬБОМА ===
        async Task LoadAlbumDataAsync()
        {
            if (_album == null) return;

            try
            {
                AlbumName.Text = _album.Title ?? "Неизвестно";
                ArtistName.Text = _album.Artist ?? "Неизвестен";
                CollectionType.Text = "Альбом";
                CreatonDate.Text = UIElementsFactory.FormatReleaseDate(_album.ReleaseDate);

                LoadCoverFromUrl(_album.CoverUrl);

                var tracks = await _musicService.GetAlbumTracksAsync(_album.Id);
                var trackList = _album.Tracks.ToList();

                if (trackList.Any())
                {
                    TracksCount.Text = $"Треков: {trackList.Count}";
                    SummaryDuration.Text = " | Длительность: " + UIElementsFactory.FormatTotalDuration(trackList);
                    DisplayTracks(trackList);
                }
                else
                {
                    TracksCount.Text = "Треков: 0";
                    SummaryDuration.Text = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading album: {ex.Message}");
            }
        }

        void LoadCoverFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Debug.WriteLine("URL обложки пустой");
                AlbumCoverCenterField.ImageSource = null;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                AlbumCoverCenterField.ImageSource = bitmap;
                Debug.WriteLine("Обложка загружена");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки обложки: {ex.Message}");
                AlbumCoverCenterField.ImageSource = null;
            }
        }
        #endregion

        #region === ЗАГРУЗКА ПЛЕЙЛИСТА ===
        async Task LoadPlaylistDataAsync()
        {
            if (_playlist == null) return;

            if (_playlist.Id == _context.Playlists.First(p => p.CreatorId == _listener.Id).Id)
                PlaylistFunctionalPanel.Visibility = Visibility.Collapsed;

            try
            {
                var playlist = _context.Playlists.Find(_playlist.Id);
                if (playlist == null) return;

                AlbumName.Text = playlist.Title ?? "Неизвестно";
                ArtistName.Text = _context.Listeners.Find(playlist.CreatorId)?.Name ?? "Неизвестен";
                CollectionType.Text = "Плейлист";
                CreatonDate.Text = playlist.CreatedAt.ToString("d MMM yyyy");

                if (!string.IsNullOrEmpty(playlist.Description))
                {
                    DescriptionText.Text = "Описание:\n" + playlist.Description;
                    DescriptionText.Visibility = Visibility.Visible;
                }

                var cover = _context.PlaylistCovers.Find(playlist.CoverId);
                if (cover != null)
                    LoadCoverFromUrl(cover.FilePath);

                var tracks = await LoadPlaylistTracksAsync(playlist.TracksId);

                TracksCount.Text = $"Треков: {tracks.Count}";
                SummaryDuration.Text = " | Длительность: " + UIElementsFactory.FormatTotalDuration(tracks);

                DisplayTracks(tracks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading playlist: {ex.Message}");
            }
        }
        #endregion

        #region === ОТОБРАЖЕНИЕ ===
        void DisplayTracks(List<Track> tracks)
        {
            Debug.WriteLine($"📋 Отображение {tracks.Count} треков");

            SongsList.Children.Clear();

            for (int i = 0; i < tracks.Count; i++)
            {
                Debug.WriteLine($"  {i + 1}. {tracks[i].Title} - {tracks[i].Artist}");

                Border card = _listener != null
                    ? UIElementsFactory.CreateTrackCard(tracks[i], _listener, i, _listenerMain.TrackCard_Click)
                    : UIElementsFactory.CreateTrackCard(tracks[i], i, _listenerMain.TrackCard_Click);

                if (card.Child is Grid grid)
                {
                    grid.Tag = _album ?? (object)_playlist;
                }

                SongsList.Children.Add(card);
            }

            Debug.WriteLine($"✅ Добавлено карточек: {SongsList.Children.Count}");
        }

        async Task<List<Track>> LoadPlaylistTracksAsync(List<string> trackIds)
        {
            var tracks = new List<Track>();

            foreach (var trackId in trackIds)
            {
                try
                {
                    var track = await _musicService.GetTrackAsync(trackId);
                    if (track != null)
                        tracks.Add(track);
                }
                catch { }
            }

            return tracks;
        }
        #endregion

        #region === УПРАВЛЕНИЕ ПЛЕЙЛИСТОМ ===
        private void EditPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (_playlist != null)
            {
                var playlist = _context.Playlists.Find(_playlist.Id);
                new PlaylistEditWindow(playlist).ShowDialog();
                _collectionMain?.LoadMediaLibrary();
            }
        }

        private void DeletePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (_playlist == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить {_playlist.Title}?", "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                DeletePlaylist();
        }

        void DeletePlaylist()
        {
            var playlist = _context.Playlists.Find(_playlist.Id);
            if (playlist == null) return;

            var link = _context.ListenerPlaylistsLinks.First(l => l.ListenerId == _listener.Id && l.PlaylistId == playlist.Id);
            if (link == null) return;

            _context.ListenerPlaylistsLinks.Remove(link);
            _context.Playlists.Remove(playlist);
            _context.SaveChanges();

            _collectionMain?.LoadMediaLibrary();
            _listenerMain.CenterField.Navigate(new HomePage(_listenerMain, _listener));
        }

        #endregion

        private void ArtistName_Click(object sender, MouseButtonEventArgs e)
        {
            if (_album != null && _album.Artist != null)
            {
                _ = NavigateToArtistAsync(_album.Artist);
            }
        }

        async Task NavigateToArtistAsync(string artistName)
        {
            try
            {
                var artist = await _musicService.GetArtistAsync(artistName);
                if (artist != null)
                {
                    _listenerMain.CenterField.Navigate(
                        new ArtistProfilePage(_listenerMain, artist, _listener));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading artist: {ex.Message}");
            }
        }
    }
}
