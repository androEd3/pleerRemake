using pleer.Models.Jamendo;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Windows;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace pleer.Resources.Pages.UserPages
{
    /// <summary>
    /// Логика взаимодействия для ArtistProfilePage.xaml
    /// </summary>
    public partial class ArtistProfilePage : Page
    {
        ListenerMainWindow _listenerMain;
        IMusicService _musicService;

        Artist _artist;
        Listener _listener;

        public ArtistProfilePage(ListenerMainWindow listenerMain, Artist artist)
        {
            InitializeComponent();

            _listenerMain = listenerMain;
            _artist = artist;
            _musicService = listenerMain._musicService;

            Unloaded += (s, e) => _listenerMain._currentArtist = null;
            Loaded += async (s, e) => await LoadArtistDataAsync();
        }

        public ArtistProfilePage(ListenerMainWindow listenerMain, Artist artist, Listener listener)
        {
            InitializeComponent();

            _listenerMain = listenerMain;
            _artist = artist;
            _listener = listener;
            _musicService = listenerMain._musicService;

            Unloaded += (s, e) => _listenerMain._currentArtist = null;
            Loaded += async (s, e) => await LoadArtistDataAsync();
        }

        async Task LoadArtistDataAsync()
        {
            if (_artist == null) return;

            try
            {
                var artist = await _musicService.GetArtistAsync(_artist.Id);
                if (artist != null)
                    _artist = artist;

                // Метаданные
                ArtistName.Text = _artist.Name;

                LoadPictureFromUrl(_artist.ImageUrl);

                // контент артиста
                ArtistsContentField.Navigate(
                    new HomePage(_listenerMain, _artist, _listener));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading artist: {ex.Message}");
            }
        }

        void LoadPictureFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                UserPicture.ImageSource = UIElementsFactory
                    .DecodePhoto(InitializeData.GetDefaultProfilePicturePath(), 200);
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(url);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                UserPicture.ImageSource = bitmap;
            }
            catch
            {
                UserPicture.ImageSource = UIElementsFactory
                    .DecodePhoto(InitializeData.GetDefaultProfilePicturePath(), 200);
            }
        }
        private void ArtistProfilePage_Unloaded(object sender, RoutedEventArgs e)
        {
            _listenerMain._currentArtist = null;
        }
    }
}
