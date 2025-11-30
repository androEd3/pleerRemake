using pleer.Models.DatabaseContext;
using pleer.Models.Jamendo;
using pleer.Models.Users;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;

namespace pleer.Resources.Pages.AdminPages
{
    /// <summary>
    /// Логика взаимодействия для ReportPage.xaml
    /// </summary>
    public partial class ReportPage : Page
    {
        DBContext _context = new();

        IMusicService _musicService;

        public ReportPage(IMusicService musicService)
        {
            InitializeComponent();

            _musicService = musicService;

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

        #region Локальные данные
        private void LoadReportData()
        {
            DateOnly? startDate = StartDateSelector?.SelectedDate.HasValue == true
                ? DateOnly.FromDateTime(StartDateSelector.SelectedDate.Value)
                : null;

            DateOnly? endDate = EndDateSelector?.SelectedDate.HasValue == true
                ? DateOnly.FromDateTime(EndDateSelector.SelectedDate.Value)
                : null;

            var listeners = FilterByDate(_context.Listeners, l => l.CreatedAt, startDate, endDate).ToList();
            var playlists = FilterByDate(_context.Playlists, p => p.CreatedAt, startDate, endDate).ToList();

            TotalUsers.Text = listeners.Count.ToString();
            TotalPlaylists.Text = playlists.Count.ToString();
        }

        private IQueryable<T> FilterByDate<T>(
            IQueryable<T> query,
            Expression<Func<T, DateOnly>> dateSelector,
            DateOnly? startDate,
            DateOnly? endDate) where T : class
        {
            if (startDate.HasValue)
            {
                var start = startDate.Value;
                var parameter = dateSelector.Parameters[0];
                var body = System.Linq.Expressions.Expression.GreaterThanOrEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(start));
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value;
                var parameter = dateSelector.Parameters[0];
                var body = System.Linq.Expressions.Expression.LessThanOrEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(end));
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda);
            }

            return query;
        }
        #endregion

        #region API данные
        private async Task LoadApiDataAsync()
        {
            try
            {
                // Параллельная загрузка
                var tracksTask = _musicService.GetPopularTracksAsync(10);
                var albumsTask = _musicService.GetPopularAlbumsAsync(10);
                var artistsTask = _musicService.GetPopularArtistsAsync(10);

                await Task.WhenAll(tracksTask, albumsTask, artistsTask);

                var tracks = (await tracksTask).ToList();
                var albums = (await albumsTask).ToList();
                var artists = (await artistsTask).ToList();

                // Статистика
                TotalSongs.Text = $"{tracks.Count}+ (топ)";
                TotalAlbums.Text = $"{albums.Count}+ (топ)";
                TotalArtists.Text = $"{artists.Count}+ (топ)";

                // Самые популярные
                var mostPopularTrack = tracks.FirstOrDefault();
                MostPopularSong.Text = mostPopularTrack != null
                    ? $"{mostPopularTrack.Title} — {mostPopularTrack.Artist}"
                    : "Нет данных";

                var mostPopularAlbum = albums.FirstOrDefault();
                MostPopularAlbum.Text = mostPopularAlbum != null
                    ? $"{mostPopularAlbum.Title} — {mostPopularAlbum.ArtistName}"
                    : "Нет данных";

                var mostPopularArtist = artists.FirstOrDefault();
                MostPopularArtist.Text = mostPopularArtist?.Name ?? "Нет данных";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API error: {ex.Message}");

                TotalSongs.Text = "Ошибка";
                TotalAlbums.Text = "Ошибка";
                MostPopularSong.Text = "Ошибка загрузки";
                MostPopularAlbum.Text = "Ошибка загрузки";
            }
        }
        #endregion

        private async void DateSelector_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            //LoadLocalData();
        }

        private async void UpdateReportDataButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportDataAsync();
        }
    }
}
