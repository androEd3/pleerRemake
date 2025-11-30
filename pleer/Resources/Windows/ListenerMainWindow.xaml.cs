using Microsoft.Extensions.Caching.Memory;
using NAudio.Wave;
using pleer.Models.DatabaseContext;
using pleer.Models.Jamendo;
using pleer.Models.Media;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Pages.Collections;
using pleer.Resources.Pages.GeneralPages;
using pleer.Resources.Pages.Songs;
using pleer.Resources.Pages.UserPages;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace pleer.Resources.Windows
{
    public partial class ListenerMainWindow : Window
    {
        DBContext _context = new();
        Listener _listener;

        HomePage _homePage;
        ListenedHistoryPage _listenedHistoryPage;

        IWavePlayer _wavePlayer;
        WaveStream _audioStream;
        public IMusicService _musicService;

        Track _selectedTrack;
        AudioFileReader _audioFile;

        Border _selectedCard;
        Playlist _currentPlaylist;
        public Album _currentAlbum;
        public Artist _currentArtist;

        public List<Track> _listeningHistory = [];
        int _songSerialNumber;

        bool _isDraggingMediaSlider = false;
        bool _isDraggingVolumeSlider;
        bool _isUnpressedMediaSlider = true;

        double _timeTick = 0.03; // ~30 FPS

        PlayerState _playerState = PlayerState.Paused;
        private enum PlayerState
        {
            Playing,
            Paused
        }

        public ListenerMainWindow()
        {
            InitializeComponent();

            InitializeNAudio();
            LoadNonUserWindow();
        }

        public ListenerMainWindow(Listener listener)
        {
            InitializeComponent();

            _listener = listener;

            InitializeNAudio();
            LoadListenerData();
        }

        void InitializeMusicService()
        {
            Debug.WriteLine("🔧 Инициализация MusicService...");

            if (App.Services != null)
            {
                _musicService = App.Services.GetService(typeof(IMusicService)) as IMusicService;
                Debug.WriteLine($"✅ Сервис из DI: {_musicService != null}");
            }

            if (_musicService == null)
            {
                Debug.WriteLine("⚠️ DI не сработал, создаю напрямую");
                var cache = new MemoryCache(new MemoryCacheOptions());
                _musicService = new JamendoService("99575e94", cache);
            }

            Debug.WriteLine($"✅ MusicService готов: {_musicService != null}");
        }

        void InitializeNAudio()
        {
            _wavePlayer = new WaveOutEvent();

            _wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
            InitializeMusicService();

            InitializeProgressUpdates();
        }

        void LoadNonUserWindow()
        {
            InitializeData.SeedData(_context);

            _homePage = new HomePage(this, _listener);
            _listenedHistoryPage = new ListenedHistoryPage(this);

            MediaLibrary.Navigate(new UnauthorizedNoticePage(this));

            CenterField.Navigate(_homePage);
            ListenedHistoryField.Navigate(_listenedHistoryPage);
        }

        void LoadListenerData()
        {
            StopPlayback();

            _homePage = new HomePage(this, _listener);
            _listenedHistoryPage = new ListenedHistoryPage(this);

            CenterField.Navigate(_homePage);
            ListenedHistoryField.Navigate(_listenedHistoryPage);

            if (_listener != null)
            {
                LoginButton.Visibility = Visibility.Collapsed;
                ProfilePictureEllipse.Visibility = Visibility.Visible;

                ListenerName.Text = _listener.Name;

                var picture = _context.ProfilePictures.Find(_listener.ProfilePictureId);

                if (picture != null)
                    ProfilePictureImage.ImageSource = UIElementsFactory
                        .DecodePhoto(picture.FilePath, (int)ProfilePictureEllipse.Width);
                else
                    ProfilePictureImage.ImageSource = UIElementsFactory
                        .DecodePhoto(InitializeData.GetDefaultProfilePicturePath(), (int)ProfilePictureEllipse.Width);

                MediaLibrary.Content = null;
                MediaLibrary.Navigate(new MediaLibrary(this, _listener));
            }
        }

        void InitializeProgressUpdates()
        {
            CompositionTarget.Rendering += OnCompositionRendering;
        }

        void StopProgressUpdates()
        {
            CompositionTarget.Rendering -= OnCompositionRendering;
        }

        void OnCompositionRendering(object sender, EventArgs e)
        {
            if (_audioStream != null && !_isDraggingMediaSlider)
            {
                var position = _audioStream.CurrentTime;

                currentMediaTime.Text = position.ToString(@"mm\:ss");
                PositionSlider.Value = position.TotalSeconds;
            }
        }

        void WavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                Debug.WriteLine($"Playback error: {e.Exception.Message}");
                return;
            }

            // Проверяем, действительно ли трек закончился
            if (_audioStream != null &&
                _audioStream.CurrentTime >= _audioStream.TotalTime - TimeSpan.FromSeconds(0.5))
            {
                PlayNextSong();
            }
        }

        #region === ВОСПРОИЗВЕДЕНИЕ (ИЗМЕНЕНО) ===
        void SelectTrack(Track track)
        {
            if (_audioFile != null && !_isDraggingMediaSlider)
            {
                var position = _audioFile.CurrentTime;

                currentMediaTime.Text = position.ToString(@"mm\:ss");
                PositionSlider.Value = position.TotalSeconds;

                LoadTrackMetadata();
            }
        }

        // Воспроизведение песни
        void LoadTrackMetadata()
        {
            if (_audioStream == null || _selectedTrack == null)
                return;

            PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;

            totalMediaTime.Text = _audioStream.TotalTime.ToString(@"mm\:ss");
            PositionSlider.Maximum = _audioStream.TotalTime.TotalSeconds;

            SongName.Text = _selectedTrack.Title;
            ArtistName.Text = _selectedTrack.Artist;

            try
            {
                LoadCoverFromUrl(_selectedTrack.CoverUrl);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки обложки: {ex.Message}");
                AlbumCover.ImageSource = UIElementsFactory.DecodePhoto(
                    InitializeData.GetDefaultCoverPath(),
                    (int)AlbumCoverBorder.ActualWidth);
            }
        }

        async void LoadCoverFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                AlbumCover.ImageSource = UIElementsFactory.DecodePhoto(
                    InitializeData.GetDefaultCoverPath(),
                    (int)AlbumCoverBorder.ActualWidth);
                return;
            }

            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                AlbumCover.ImageSource = bitmap;
            }
            catch
            {
                AlbumCover.ImageSource = UIElementsFactory.DecodePhoto(
                    InitializeData.GetDefaultCoverPath(),
                    (int)AlbumCoverBorder.ActualWidth);
            }
        }
        #endregion

        #region === ОБРАБОТЧИКИ КЛИКОВ ===
        public void TrackCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card && card.Tag is Models.Media.Track track)
            {
                _currentPlaylist = null;
                _currentAlbum = null;

                CardPlaying(card);
                SelectTrack(track);
            }
        }

        void CardPlaying(Border card)
        {
            if (_selectedCard != null)
                UIElementsFactory.SetCardTitleColor(_selectedCard, "#eeeeee");

            UIElementsFactory.SetCardTitleColor(card, "#90ee90");

            _selectedCard = card;
        }

        public void AlbumCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Album album)
            {
                if (_currentAlbum != album)
                    CenterField.Navigate(new OpenCollectionPage(this, album, _listener));

                _currentAlbum = album;
            }
            else if (sender is TextBlock textBlock && textBlock.Tag is Album songAlbum)
            {
                if (_currentAlbum != songAlbum)
                    CenterField.Navigate(new OpenCollectionPage(this, songAlbum, _listener));

                _currentAlbum = songAlbum;
            }
        }

        public void ArtistCard_Click(object sender, MouseButtonEventArgs e)
        {
            
            if (sender is Border card && card.Tag is Artist artist)
            {
                if (_currentArtist != artist)
                {
                    if (_listener != null)
                        CenterField.Navigate(new ArtistProfilePage(this, artist, _listener));
                    else
                        CenterField.Navigate(new ArtistProfilePage(this, artist));
                }

                _currentArtist = artist;
            }
            else if (sender is TextBlock textBlock && textBlock.Tag is Artist artistName)
            {
                if (_currentArtist != artistName)
                {
                    if (_listener != null)
                        CenterField.Navigate(new ArtistProfilePage(this, artistName, _listener));
                    else
                        CenterField.Navigate(new ArtistProfilePage(this, artistName));
                }

                _currentArtist = artistName;
            }
        }
        #endregion

        #region === ПОИСК (ИЗМЕНЕНО) ===
        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchContent();
        }

        private void Enter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchContent();
        }

        void SearchContent()
        {
            string currentSearchText = SearchBar.Text;

            if (string.IsNullOrEmpty(currentSearchText))
                CenterField.Navigate(new HomePage(this, _listener));
            else
                CenterField.Navigate(new SearchResultPage(this, _listener, currentSearchText));
        }
        #endregion

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            CenterField.Navigate(new HomePage(this, _listener));
        }

        #region === НАВИГАЦИЯ ПО ТРЕКАМ ===
        private void PlayMedia_Click(object sender, RoutedEventArgs e)
        {
            if (_wavePlayer == null || _audioStream == null) ;
        }

        private void PreviousMedia_Click(object sender, RoutedEventArgs e)
        {
            if (_audioStream == null)
                return;

            if (_audioStream.CurrentTime < TimeSpan.FromSeconds(3) && _songSerialNumber - 1 > 0)
            {
                _songSerialNumber -= 1;
                PlayTrackFromHistory(_listeningHistory[_songSerialNumber - 1]);
            }
            else
            {
            }
        }

        // СКИП
        private void NextMedia_Click(object sender, RoutedEventArgs e)
        {
            if (_audioStream == null)
                return;

            if (_songSerialNumber < _listeningHistory.Count)
            {
                _songSerialNumber += 1;
                PlayTrackFromHistory(_listeningHistory[_songSerialNumber - 1]);
            }
            else
            {
                PlayNextSong();
            }
        }

        void PlayTrackFromHistory(Models.Media.Track track)
        {
            // Если не локальный - это стриминговый трек
            // Нужно будет хранить треки в кэше или повторно загружать
            // Для простоты пока просто пропускаем
        }

        void PlayNextSong()
        {
            //    // Для локальных коллекций
            //    if (_currentAlbum != null && _selectedTrack != null &&
            //        int.TryParse(_selectedTrack.Id, out int currentSongId))
            //    {
            //        int songIndex = _currentAlbum.SongsId.IndexOf(currentSongId);

            //        if (songIndex >= 0 && songIndex < _currentAlbum.SongsId.Count - 1)
            //        {
            //            var nextSong = _context.Songs.Find(_currentAlbum.SongsId[songIndex + 1]);
            //            if (nextSong != null)
            //            {
            //                SelectSong(nextSong);
            //                return;
            //            }
            //        }
            //    }

            //    if (_currentPlaylist != null && _selectedTrack != null &&
            //        int.TryParse(_selectedTrack.Id, out int playlistSongId))
            //    {
            //        int songIndex = _currentPlaylist.SongsId.IndexOf(playlistSongId);

            //        if (songIndex >= 0 && songIndex < _currentPlaylist.SongsId.Count - 1)
            //        {
            //            var nextSong = _context.Songs.Find(_currentPlaylist.SongsId[songIndex + 1]);
            //            if (nextSong != null)
            //            {
            //                SelectSong(nextSong);
            //                return;
            //            }
            //        }
            //    }

            // Если дошли до конца или это стриминг - останавливаем
            StopPlayback();
        }

        void AddTrackToHistory(Models.Media.Track track)
        {
            _listeningHistory.Add(track);
        }
        #endregion

        #region === СЛАЙДЕРЫ ===
        void PositionSlider_ChangeValue(double value)
        {
            if (_audioStream != null)
            {
                _audioStream.CurrentTime = TimeSpan.FromSeconds(value);
                currentMediaTime.Text = _audioStream.CurrentTime.ToString(@"mm\:ss");
            }
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingMediaSlider && _isUnpressedMediaSlider && _audioStream != null)
            {
                PositionSlider_ChangeValue(e.NewValue);
            }
        }

        private void PositionSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingMediaSlider = true;
            _isUnpressedMediaSlider = false;
        }

        private void PositionSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingMediaSlider = false;
            _isUnpressedMediaSlider = true;

            if (_audioStream != null)
            {
                _audioStream.CurrentTime = TimeSpan.FromSeconds(PositionSlider.Value);
            }
        }

        // Изменение громкости
        private void VolumeSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingVolumeSlider = true;
        }

        private void VolumeSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingVolumeSlider = false;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Громкость регулируется через WaveChannel32 при инициализации
            UpdateVolumeIcon(e.NewValue);
            if (_audioFile != null)
            {
                _audioFile.Volume = (float)e.NewValue;

                UpdateVolumeIcon(e.NewValue);
            }
        }

        private void MutePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (VolumeSlider.Value != 0)
                VolumeSlider.Value = 0;
            else
                VolumeSlider.Value = (float)VolumeSlider.Maximum / 2;
        }

        private void UpdateVolumeIcon(double volume)
        {
            if (volume == 0)
                VolumeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeMute;
            else if (volume < 0.5)
                VolumeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeLow;
            else
                VolumeIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeHigh;
        }
        #endregion

        void StopPlayback()
        {
            _wavePlayer?.Stop();
            _audioFile?.Dispose();
            _audioFile = null;

            _currentPlaylist = null;
            _currentAlbum = null;

            _playerState = PlayerState.Paused;
            PlayIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
        }

        private void ProfileImage_Click(object sender, MouseButtonEventArgs e)
        {
            FullWindow.Navigate(new ProfilePage(this, _listener));
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            FullWindow.Navigate(new LoginPage(this));
        }

        private void LoginAsAdminButton_Click(object sender, RoutedEventArgs e)
        {
            new AdminMainWindow().Show(); Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CleanupResources();
        }

        void CleanupResources()
        {
            StopProgressUpdates();

            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();
            _audioStream?.Dispose();
            _audioFile?.Dispose();

            _context?.Dispose();
        }

        // ПЕРЕМЕЩЕНИЕ СТРАНИЦ В ЦЕНТРАЛЬНОМ ОКНЕ
        private void BackPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (CenterField.CanGoBack)
                CenterField.GoBack();
        }

        private void ForwardPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (CenterField.CanGoForward)
                CenterField.GoForward();
        }

        private void UpdateButtonState()
        {
            BackButton.IsEnabled = CenterField.CanGoBack;
            ForwardButton.IsEnabled = CenterField.CanGoForward;
        }

        private void CenterField_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            UpdateButtonState();
        }
    }
}