using pleer.Models.DatabaseContext;
using pleer.Models.Media;
using pleer.Models.Users;
using pleer.Resources.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using pleer.Models.Service;
using System.Threading.Tasks;

namespace pleer.Resources.Pages.Collections
{
    public partial class CollectionsListPage : Page
    {
        DBContext _context = new();

        ListenerMainWindow _listenerMain;
        Listener _listener = new();

        Playlist _playlist;

        public CollectionsListPage(ListenerMainWindow main, Listener listener)
        {
            InitializeComponent();

            _listenerMain = main;
            _listener = listener;

            Loaded += async (s, e) => await LoadMediaLibrary_Loaded();
        }

        public async Task LoadMediaLibrary_Loaded()
        {
            MediaLibraryList.Children.Clear();

            if (_listener == null)
                return;

            var links = _context.ListenerPlaylistsLinks
                .Where(u => u.ListenerId == _listener.Id)
                .Select(u => u.PlaylistId)
                .ToArray();

            foreach (var id in links)
            {
                var playlist = _context.Playlists.Find(id);

                if (playlist != null)
                {
                    var card = UIElementsFactory.CreateCollectionCard(playlist, PlaylistCard_Click);
                    MediaLibraryList.Children.Add(card);
                }
            }
        }

        public async Task CreatePlaylist()
        {
            if (_listener == null)
            {
                MessageBox.Show("Зайдите в аккаунт чтобы воспользоваться данной функцией");
                return;
            }
            await ServiceMethods.AddPlaylistWithLink(_listener);

            await LoadMediaLibrary_Loaded();
        }

        public void AlbumCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Album album)
            {
                _listenerMain.CenterField.Navigate(new OpenCollectionPage(_listenerMain, album, _listener));
            }
        }

        public void PlaylistCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Playlist playlist)
            {
                _playlist = playlist;
                _listenerMain.CenterField.Navigate(new OpenCollectionPage(this, _listenerMain, _playlist, _listener));
            }
        }
    }
}
