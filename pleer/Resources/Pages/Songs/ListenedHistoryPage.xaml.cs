using pleer.Models.Service;
using pleer.Resources.Windows;
using System.Windows;
using System.Windows.Controls;

namespace pleer.Resources.Pages.Songs
{
    /// <summary>
    /// Логика взаимодействия для ListenedHistoryPage.xaml
    /// </summary>
    public partial class ListenedHistoryPage : Page
    {
        ListenerMainWindow _listenerMain;

        public ListenedHistoryPage(ListenerMainWindow listenerMain)
        {
            InitializeComponent();

            _listenerMain = listenerMain;

            LoadListenedHistory();
        }

        public void LoadListenedHistory()
        {
            if (_listenerMain._listeningHistory.Any())
                InfoPanel.Visibility = Visibility.Collapsed;
            else
            {
                InfoPanel.Visibility = Visibility.Visible;
                return;
            }

            ListenedSongsList.Children.Clear();

            for (int i = 0; i < _listenerMain._listeningHistory.Count; i++)
            {
                var track = _listenerMain._listeningHistory[i];

                var card = UIElementsFactory.CreateTrackCard(track, i, _listenerMain.TrackCard_Click);
            }

            ListenedSongsListScroll.ScrollToEnd();
        }

        private void HideHistoryVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            ListenedHistoryGrid.Visibility = Visibility.Collapsed;
            ShowHistoryButton.Visibility = Visibility.Visible;

            _listenerMain.MainFields.ColumnDefinitions[2].Width = GridLength.Auto;
        }

        private void ShowHistoryVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            ListenedHistoryGrid.Visibility = Visibility.Visible;
            ShowHistoryButton.Visibility = Visibility.Collapsed;

            _listenerMain.MainFields.ColumnDefinitions[2].Width = new GridLength(0.5, GridUnitType.Star);
        }
    }
}
