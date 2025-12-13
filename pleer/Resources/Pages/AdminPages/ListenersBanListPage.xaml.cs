using pleer.Models.DatabaseContext;
using pleer.Models.Service;
using pleer.Models.Users;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace pleer.Resources.Pages.AdminPages
{
    /// <summary>
    /// Логика взаимодействия для UsersBanListPage.xaml
    /// </summary>
    public partial class ListenersBanListPage : Page
    {
        DBContext _context = new();

        string _searchParameter = string.Empty;

        public ListenersBanListPage()
        {
            InitializeComponent();

            LoadListenersList();
        }

        public void LoadListenersList()
        {
            UsersPanel.Children.Clear();

            List<Listener> listeners;

            if (string.IsNullOrEmpty(_searchParameter))
            {
                listeners = _context.Listeners
                    .ToList();
            }
            else
            {
                listeners = _context.Listeners
                    .Where(s => s.Name.Contains(_searchParameter))
                    .ToList();
            }

            if (!listeners.Any())
            {
                string message = "Слушателей не найдено";
                UIElementsFactory.NoContent(message, UsersPanel);
                return;
            }

            foreach (var listener in listeners)
            {
                int listenerId = listeners.IndexOf(listener);

                var card = UIElementsFactory.CreateUserCard(listenerId, listener, BlockListener);
                UsersPanel.Children.Add(card);
            }

            TotlaUsers.Text = $"Найдено слушателей: {listeners.Count}";
        }

        public void BlockListener(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Listener listener)
            {
                try
                {
                    var refreshedListener = _context.Listeners.Find(listener.Id);

                    refreshedListener.Status = !refreshedListener.Status;
                    _context.SaveChanges();

                    LoadListenersList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось заблокировать/разблокировать слушателя {listener}. Ошибка: {ex.Message}");
                }
            }
        }

        private void LoadListenersButton_Click(object sender, RoutedEventArgs e)
        {
            LoadListenersList();
        }

        // POISK
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
            _searchParameter = SearchBar.Text;

            LoadListenersList();
        }
    }
}
