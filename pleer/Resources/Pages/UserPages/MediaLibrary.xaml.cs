using pleer.Models.Users;
using pleer.Resources.Pages.Collections;
using pleer.Resources.Windows;
using System.Windows;
using System.Windows.Controls;

namespace pleer.Resources.Pages.UserPages
{
    public partial class MediaLibrary : Page
    {
        CollectionsListPage _albumsList;

        public MediaLibrary(ListenerMainWindow main, Listener listener)
        {
            InitializeComponent();

            _albumsList = new CollectionsListPage(main, listener);

            ShowAlbumsList();
        }

        public void ShowAlbumsList()
        {
            MediaLibraryAlbumsList.Navigate(_albumsList);
        }

        private async void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            await _albumsList.CreatePlaylist();
        }
    }
}
