using Microsoft.Win32;
using pleer.Models.DatabaseContext;
using pleer.Models.Media;
using pleer.Models.Service;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace pleer.Resources.Windows
{
    /// <summary>
    /// Логика взаимодействия для PlaylistEditWindow.xaml
    /// </summary>
    public partial class PlaylistEditWindow : Window
    {
        DBContext _context = new();

        Playlist _playlist;

        string _coverPath;

        public PlaylistEditWindow(Playlist playlist)
        {
            InitializeComponent();

            _playlist = playlist;

            CoverMouseEvents();
            LoadPlaylistData();

            DescriptionPanel.Visibility = Visibility.Visible;
        }

        void CoverMouseEvents()
        {
            AlbumCoverGrid.MouseEnter += (s, e) => ChangeAlbumCoverGrid.Visibility = Visibility.Visible;
            AlbumCoverGrid.MouseLeave += (s, e) => ChangeAlbumCoverGrid.Visibility = Visibility.Collapsed;
        }

        void LoadPlaylistData()
        {
            AlbumTitle.Text = _playlist.Title;
            PlaylistDescription.Text = _playlist.Description;

            var cover = _context.PlaylistCovers
               .Find(_playlist.CoverId);
            LoadCover(cover.FilePath);
        }

        void LoadCover(string filePath)
        {
            AlbumCoverDemonstrate.ImageSource =
                UIElementsFactory
                    .DecodePhoto(filePath, (int)AlbumCoverDemonstrate.ImageSource.Width) ??
                UIElementsFactory
                    .DecodePhoto(InitializeData.GetDefaultCoverPath(), (int)AlbumCoverDemonstrate.ImageSource.Width);
        }

        private void ChangeAlbumCoverGrid_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ErrorNoticePanel.Text = string.Empty;

            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите обложку альбома",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _coverPath = openFileDialog.FileName;
                    var bitmap = new BitmapImage(new Uri(_coverPath));

                    AlbumCoverDemonstrate.ImageSource = bitmap;
                }
                catch (Exception ex)
                {
                    ErrorNoticePanel.Text = "Ошибка загрузки изображения";
                }
            }
        }

        private async void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorNoticePanel.Text = string.Empty;

            if (string.IsNullOrEmpty(AlbumTitle.Text))
            {
                ErrorNoticePanel.Text = "Название не может быть пустым";
                return;
            }

            if (_playlist != null)
                await UpdatePlaylistAsync();
        }

        async Task UpdatePlaylistAsync()
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var playlist = _context.Playlists.Find(_playlist.Id);

                    playlist.Title = AlbumTitle.Text;
                    playlist.Description = PlaylistDescription.Text;

                    if (_coverPath != null)
                    {
                        int coverId = _context.PlaylistCovers.Max(pc => pc.Id) + 1;
                        var playlistCover = PictureService.SavePlaylistCover(_coverPath, coverId);

                        _context.Add(playlistCover);
                        await _context.SaveChangesAsync();

                        playlist.CoverId = playlistCover.Id;
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    Debug.WriteLine("Успешное обновление");

                    Close();
                }
                catch (Exception ex)
                {
                    ErrorNoticePanel.Text = "Ошибка изменения плейлиста.";
                }
            }
        }
    }
}
