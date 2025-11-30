using Microsoft.Extensions.Caching.Memory;
using pleer.Models.DatabaseContext;
using pleer.Models.Jamendo;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Pages.AdminPages;
using pleer.Resources.Pages.GeneralPages;
using System.Windows;

namespace pleer.Resources.Windows
{
    /// <summary>
    /// Логика взаимодействия для AdminMainWindow.xaml
    /// </summary>
    public partial class AdminMainWindow : Window
    {
        DBContext _context = new();

        public IMusicService _musicService;

        Admin _admin;

        public AdminMainWindow()
        {
            InitializeComponent();

            LoadNonUserWindow();
            InitializeMusicService();
        }

        public AdminMainWindow(Admin admin)
        {
            InitializeComponent();

            _admin = admin;
        }

        void LoadNonUserWindow()
        {
            InitializeData.CreateAdmin(_context);
            FullWindow.Navigate(new LoginPage(this));
        }

        void InitializeMusicService()
        {
            if (App.Services != null)
            {
                _musicService = App.Services.GetService(typeof(IMusicService)) as IMusicService;
            }
            else
            {
                var cache = new MemoryCache(new MemoryCacheOptions());
                _musicService = new JamendoService("99575e94", cache);
            }
        }

        private void UsersListButton_Click(object sender, RoutedEventArgs e)
        {
            OperationField.Navigate(new UsersBanListPage());
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            OperationField.Navigate(new ReportPage(_musicService));
        }

        // login as ktoto tam
        private void LoginAsListenerButton_Click(object sender, RoutedEventArgs e)
        {
            new ListenerMainWindow().Show(); Close();
        }

        // PAGE navigation
        private void BackPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (OperationField.CanGoBack)
                OperationField.GoBack();
        }

        private void ForwardPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (OperationField.CanGoForward)
                OperationField.GoForward();
        }

        private void UpdateButtonState()
        {
            BackButton.IsEnabled = OperationField.CanGoBack;
            ForwardButton.IsEnabled = OperationField.CanGoForward;
        }

        private void OperationField_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            UpdateButtonState();
        }
    }
}
