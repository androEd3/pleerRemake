using MaterialDesignThemes.Wpf;
using NAudio.Wave;
using pleer.Models.DatabaseContext;
using pleer.Models.IA;
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
using System.Windows.Media.Imaging;

namespace pleer.Resources.Windows
{
    public partial class ListenerMainWindow : Window
    {
        DBContext _context = new();
        Listener _listener;

        HomePage _homePage;
        ListenedHistoryPage _listenedHistoryPage;

        IWavePlayer _wavePlayer;
        WaveStream _audioStream; // MediaFoundationReader
        public IMusicService _musicService;

        Track _selectedTrack;

        Border _selectedCard;
        Playlist _currentPlaylist;
        public Album _currentAlbum;
        public Artist _currentArtist;

        public List<Track> _listeningHistory = new();
        int _songSerialNumber;

        bool _isDraggingMediaSlider;
        double _previousVolume = 0.1;
        bool _isDraggingVolumeSlider;

        double _timeTick = 0.033; // ~30 FPS

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
            if (App.Services != null)
            {
                _musicService = (IMusicService)App.Services.GetService(typeof(IMusicService));
                Debug.WriteLine($"Сервис из DI: {_musicService != null}");
            }
        }

        void InitializeNAudio()
        {
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

            if (_audioStream != null &&
                _audioStream.CurrentTime >= _audioStream.TotalTime - TimeSpan.FromSeconds(0.5))
            {
                PlayNextTrack();
            }
        }

        #region === ВОСПРОИЗВЕДЕНИЕ ===
        async void SelectTrack(Track track)
        {
            try
            {
                StopPlayback();

                _selectedTrack = track;

                string streamUrl = track.StreamUrl;

                if (string.IsNullOrWhiteSpace(streamUrl) && _musicService != null)
                {
                    streamUrl = await _musicService.GetStreamUrlAsync(track);
                }

                Debug.WriteLine($"🔗 URL: {streamUrl}");

                if (string.IsNullOrWhiteSpace(streamUrl))
                {
                    MessageBox.Show("Не удалось получить ссылку на поток", "Ошибка");
                    return;
                }

                // новый считыватель потока и плеер
                _audioStream?.Dispose();
                _audioStream = new MediaFoundationReader(streamUrl);

                if (_wavePlayer != null)
                {
                    _wavePlayer.PlaybackStopped -= WavePlayer_PlaybackStopped;
                    _wavePlayer.Dispose();
                }

                _wavePlayer = new WaveOutEvent();
                _wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
                _wavePlayer.Init(_audioStream);
                _wavePlayer.Volume = (float)VolumeSlider.Value;
                _wavePlayer.Play();

                totalMediaTime.Text = _audioStream.TotalTime.ToString(@"mm\:ss");
                PositionSlider.Maximum = _audioStream.TotalTime.TotalSeconds;

                _playerState = PlayerState.Playing;

                AddTrackToHistory(track);
                await LoadTrackMetadata();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка: {ex}");
                MessageBox.Show($"Ошибка воспроизведения: {ex.Message}");
            }
        }

        async Task LoadTrackMetadata()
        {
            if (_audioStream == null || _selectedTrack == null)
                return;

            VisualBorder.Visibility = Visibility.Collapsed;

            PlayIcon.Kind = PackIconKind.Pause;

            totalMediaTime.Text = _audioStream.TotalTime.ToString(@"mm\:ss");
            PositionSlider.Maximum = _audioStream.TotalTime.TotalSeconds;

            SongName.Text = _selectedTrack.Title;
            SongName.Tag = _currentAlbum;
            AlbumCoverBorder.Tag = _currentAlbum;

            ArtistName.Text = _selectedTrack.Artist;
            ArtistName.Tag = _selectedTrack.Artist;

            await LoadCoverFromUrl(_selectedTrack.CoverUrl);
        }

        async Task LoadCoverFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                await SetDefaultCover();
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                AlbumCover.ImageSource = bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Ошибка загрузки обложки: {ex.Message}");
                await SetDefaultCover();
            }
        }

        async Task SetDefaultCover()
        {
            try
            {
                AlbumCover.ImageSource = UIElementsFactory.DecodePhoto(
                    InitializeData.GetDefaultCoverPath(),
                    (int)(AlbumCoverBorder.ActualWidth > 0 ? AlbumCoverBorder.ActualWidth : 100));
            }
            catch { }
        }
        #endregion

        #region === ОБРАБОТЧИКИ КЛИКОВ ===
        public async void TrackCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card && card.Tag is Track track)
            {
                if (card.Child is Grid grid)
                {
                    if (grid.Tag is Album album)
                        _currentAlbum = album;
                    else if (grid.Tag is Playlist playlist)
                        _currentPlaylist = playlist;
                    else
                    {
                        _currentAlbum = null;
                        _currentPlaylist = null;
                    }
                }

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

        public async void AlbumCard_Click(object sender, MouseButtonEventArgs e)
        {
            Album album = null;

            if (sender is Border border && border.Tag is Album a1)
            {
                album = a1;
            }
            else if (sender is TextBlock textBlock && textBlock.Tag is Album a2)
            {
                album = a2;
            }
            else if (sender is Border borderImage && borderImage.Tag is Album a3)
            {
                album = a3;
            }

            if (album == null)
                return;

            _currentAlbum = album;
            CenterField.Navigate(new OpenCollectionPage(this, album, _listener));
        }

        public async void ArtistCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border card && card.Tag is Artist artist)
            {
                if (!string.Equals(_currentArtist?.Name, artist.Name, StringComparison.OrdinalIgnoreCase))
                {
                    CenterField.Navigate(new ArtistProfilePage(this, artist, _listener));
                }
                _currentArtist = artist;
            }
            else if (sender is TextBlock textBlock &&
                     textBlock.Tag is string artistName &&
                     !string.IsNullOrWhiteSpace(artistName))
            {
                try
                {
                    var artistFromService = await _musicService.GetArtistAsync(artistName);
                    if (artistFromService != null)
                    {
                        CenterField.Navigate(new ArtistProfilePage(this, artistFromService, _listener));
                        _currentArtist = artistFromService;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Ошибка загрузки артиста: {ex.Message}");
                }
            }
        }
        #endregion

        #region === ПОИСК ===
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

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            CenterField.Navigate(new HomePage(this, _listener));
        }
        #endregion

        #region === НАВИГАЦИЯ ПО ТРЕКАМ ===
        private void PlayMedia_Click(object sender, RoutedEventArgs e)
        {
            if (_wavePlayer == null || _audioStream == null)
                return;

            switch (_playerState)
            {
                case PlayerState.Paused:
                    _wavePlayer.Play();
                    _playerState = PlayerState.Playing;
                    PlayIcon.Kind = PackIconKind.Pause;
                    break;

                case PlayerState.Playing:
                    _wavePlayer.Pause();
                    _playerState = PlayerState.Paused;
                    PlayIcon.Kind = PackIconKind.Play;
                    break;
            }
        }

        private void PreviousMedia_Click(object sender, RoutedEventArgs e)
        {
            if (_audioStream == null)
                return;

            if (_audioStream.CurrentTime < TimeSpan.FromSeconds(3) && _songSerialNumber > 1)
            {
                _songSerialNumber--;
                var track = _listeningHistory[_songSerialNumber - 1];
                SelectTrack(track);
            }
            else
                PositionSlider_ChangeValue(0);
        }

        private void NextMedia_Click(object sender, RoutedEventArgs e)
        {
            if (_audioStream == null)
                return;

            if (_songSerialNumber < _listeningHistory.Count)
            {
                _songSerialNumber++;
                var track = _listeningHistory[_songSerialNumber - 1];
                SelectTrack(track);
            }
            else
            {
                PlayNextTrack();
            }
        }

        void PlayNextTrack()
        {
            // Из альбома
            if (_currentAlbum?.Tracks != null && _selectedTrack != null)
            {
                var currentIndex = _currentAlbum.Tracks.FindIndex(t => t.Id == _selectedTrack.Id);

                if (currentIndex >= 0 && currentIndex < _currentAlbum.Tracks.Count - 1)
                {
                    var nextTrack = _currentAlbum.Tracks[currentIndex + 1];
                    SelectTrack(nextTrack);
                    return;
                }
            }

            // Из плейлиста
            if (_currentPlaylist?.Tracks != null && _selectedTrack != null)
            {
                var currentIndex = _currentPlaylist.Tracks.FindIndex(t => t.Id == _selectedTrack.Id);

                if (currentIndex >= 0 && currentIndex < _currentPlaylist.Tracks.Count - 1)
                {
                    var nextTrack = _currentPlaylist.Tracks[currentIndex + 1];
                    SelectTrack(nextTrack);
                    return;
                }
            }

            StopPlayback();
        }

        void AddTrackToHistory(Track track)
        {
            _listeningHistory.Add(track);
            _songSerialNumber = _listeningHistory.Count;
            _listenedHistoryPage.LoadListenedHistory();
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
            if (_isDraggingMediaSlider && _audioStream != null)
            {
                PositionSlider_ChangeValue(e.NewValue);
            }
        }

        private void PositionSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingMediaSlider = true;
        }

        private void PositionSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingMediaSlider = false;

            if (_audioStream != null)
            {
                _audioStream.CurrentTime = TimeSpan.FromSeconds(PositionSlider.Value);
            }
        }

        // изменение громкости
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.Volume = (float)e.NewValue;
            }

            UpdateVolumeIcon(e.NewValue);
        }

        private void VolumeSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingVolumeSlider = true;
        }

        private void VolumeSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingVolumeSlider = false;
        }

        private void MutePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (VolumeSlider.Value > 0)
            {
                _previousVolume = VolumeSlider.Value;
                VolumeSlider.Value = 0;
            }
            else
            {
                VolumeSlider.Value = _previousVolume > 0 ? _previousVolume : 0.5;
            }
        }

        private void UpdateVolumeIcon(double volume)
        {
            if (VolumeIcon == null) return;

            if (volume == 0)
                VolumeIcon.Kind = PackIconKind.VolumeMute;
            else if (volume < 0.1)
                VolumeIcon.Kind = PackIconKind.VolumeLow;
            else
                VolumeIcon.Kind = PackIconKind.VolumeHigh;
        }
        #endregion

        void StopPlayback()
        {
            try
            {
                _wavePlayer?.Stop();
            }
            catch { }

            _playerState = PlayerState.Paused;
            PlayIcon.Kind = PackIconKind.Play;
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

            _wavePlayer?.Dispose();
            _audioStream?.Dispose();

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