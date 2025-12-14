using pleer.Models.DatabaseContext;
using pleer.Models.IA;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace pleer.Resources.Pages.AdminPages
{
    /// <summary>
    /// Логика взаимодействия для ReportPage.xaml
    /// </summary>
    public partial class StatisticsPage : Page
    {
        DBContext _context = new();
        IMusicService _musicService;
        private CancellationTokenSource _loadCancellation;

        public StatisticsPage()
        {
            InitializeComponent();
            InitializeMusicService();

            Loaded += async (s, e) => await LoadReportDataAsync();
        }

        void InitializeMusicService()
        {
            if (App.Services != null)
            {
                _musicService = (IMusicService)App.Services.GetService(typeof(IMusicService));
                Debug.WriteLine($"Сервис из DI: {_musicService != null}");
            }
        }

        private async Task LoadReportDataAsync()
        {
            _loadCancellation?.Cancel();
            _loadCancellation = new CancellationTokenSource();
            var token = _loadCancellation.Token;

            try
            {
                ShowLoading(true);

                // Локальные данные из БД
                await LoadLocalDataAsync();

                // Данные из API
                await LoadApiDataAsync(token);

                ShowLoading(false);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Loading cancelled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading report: {ex.Message}");
                ShowLoading(false);
                ShowError("Ошибка загрузки данных");
            }
        }

        private async Task LoadLocalDataAsync()
        {
            await Task.Run(() =>
            {
                var listenersCount = _context.Listeners.Count();
                var playlistsCount = _context.Playlists.Count();

                Dispatcher.Invoke(() =>
                {
                    TotalListeners.Text = listenersCount.ToString("N0");
                    TotalPlaylists.Text = playlistsCount.ToString("N0");
                });
            });
        }

        private async Task LoadApiDataAsync(CancellationToken token)
        {
            try
            {
                var stats = await _musicService.GetStatisticsAsync(token);

                if (token.IsCancellationRequested) return;

                // Общие данные
                TotalSongs.Text = FormatLargeNumber(stats.TotalAudioItems);
                TotalMusicItems.Text = FormatLargeNumber(stats.TotalMusicItems);

                // Самые популярные
                if (stats.MostPopularTrack != null)
                {
                    MostPopularSong.Text = $"{stats.MostPopularTrack.Title}";
                    MostPopularSongArtist.Text = stats.MostPopularTrack.Artist;
                }
                else
                {
                    MostPopularSong.Text = "Нет данных";
                }

                if (stats.MostPopularAlbum != null)
                {
                    MostPopularAlbum.Text = stats.MostPopularAlbum.Title;
                    MostPopularAlbumArtist.Text = stats.MostPopularAlbum.Artist;
                }
                else
                {
                    MostPopularAlbum.Text = "Нет данных";
                }

                if (stats.MostPopularArtist != null)
                {
                    MostPopularArtist.Text = stats.MostPopularArtist.Name;
                }
                else
                {
                    MostPopularArtist.Text = "Нет данных";
                }

                // Статистика по жанрам
                DisplayGenreStats(stats.GenreStats);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API error: {ex.Message}");
                MostPopularSong.Text = "Ошибка загрузки";
                MostPopularAlbum.Text = "Ошибка загрузки";
                MostPopularArtist.Text = "Ошибка загрузки";
            }
        }

        private void DisplayGenreStats(Dictionary<string, long> genreStats)
        {
            GenreStatsPanel.Children.Clear();

            foreach (var (genre, count) in genreStats.Take(8))
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };

                panel.Children.Add(new TextBlock
                {
                    Text = genre,
                    Width = 100,
                    Style = (Style)Application.Current.TryFindResource("SmallInfoPanel")
                });

                panel.Children.Add(new TextBlock
                {
                    Text = FormatLargeNumber(count),
                    Style = (Style)Application.Current.TryFindResource("SmallMainInfoPanel")
                });

                GenreStatsPanel.Children.Add(panel);
            }
        }

        private string FormatLargeNumber(long number)
        {
            if (number >= 1_000_000)
                return $"{number / 1_000_000.0:F1}M";
            if (number >= 1_000)
                return $"{number / 1_000.0:F1}K";
            return number.ToString("N0");
        }

        private void ShowLoading(bool show)
        {
            LoadingIndicator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ContentPanel.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            TitleBorder.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private async void UpdateReportDataButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportDataAsync();
        }
    }
}
