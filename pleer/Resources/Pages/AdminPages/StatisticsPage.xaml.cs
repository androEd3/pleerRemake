using pleer.Models.DatabaseContext;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace pleer.Resources.Pages.AdminPages
{
    /// <summary>
    /// Логика взаимодействия для ReportPage.xaml
    /// </summary>
    public partial class StatisticsPage : Page
    {
        DBContext _context = new();

        public StatisticsPage()
        {
            InitializeComponent();

            Loaded += async (s, e) => await LoadReportDataAsync();
        }

        private async Task LoadReportDataAsync()
        {
            try
            {
                await LoadApiDataAsync();
            }
            catch { }
        }

        #region API данные
        private async Task LoadApiDataAsync()
        {
            try
            {
                TotalListeners.Text = _context.Listeners.Count().ToString();
                TotalPlaylists.Text = _context.Playlists.Count().ToString();

                // Параллельная загрузка
                //await Task.WhenAll(tracksTask, albumsTask, artistsTask);

                //var tracks = (await tracksTask).ToList();
                //var albums = (await albumsTask).ToList();
                //var artists = (await artistsTask).ToList();

                //// Статистика
                ////TotalSongs.Text = $"{tracks.Count}+ (топ)";
                ////TotalAlbums.Text = $"{albums.Count}+ (топ)";
                ////TotalArtists.Text = $"{artists.Count}+ (топ)";

                //// Самые популярные
                //var mostPopularTrack = tracks.FirstOrDefault();
                //MostPopularSong.Text = mostPopularTrack != null
                //    ? $"{mostPopularTrack.Title} — {mostPopularTrack.Artist}"
                //    : "Нет данных";

                //var mostPopularAlbum = albums.FirstOrDefault();
                //MostPopularAlbum.Text = mostPopularAlbum != null
                //    ? $"{mostPopularAlbum.Title} — {mostPopularAlbum.ArtistName}"
                //    : "Нет данных";

                //var mostPopularArtist = artists.FirstOrDefault();
                //MostPopularArtist.Text = mostPopularArtist?.Name ?? "Нет данных";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API error: {ex.Message}");

                //TotalSongs.Text = "Ошибка";
                //TotalAlbums.Text = "Ошибка";
                MostPopularSong.Text = "Ошибка загрузки";
                MostPopularAlbum.Text = "Ошибка загрузки";
            }
        }
        #endregion

        private async void DateSelector_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private async void UpdateReportDataButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportDataAsync();
        }
    }
}
