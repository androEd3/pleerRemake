using pleer.Models.DatabaseContext;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Pages.AdminPages;
using pleer.Resources.Pages.GeneralPages;
using System.Windows;
using System.Windows.Navigation;

namespace pleer.Resources.Windows
{
    /// <summary>
    /// Логика взаимодействия для AdminMainWindow.xaml
    /// </summary>
    public partial class AdminMainWindow : Window
    {
        DBContext _context = new();

        public AdminMainWindow()
        {
            InitializeComponent();

            LoadNonUserWindow();
        }

        public AdminMainWindow(Admin admin)
        {
            InitializeComponent();
        }

        void LoadNonUserWindow()
        {
            InitializeData.CreateAdmin(_context);
            FullWindow.Navigate(new LoginPage(this));
        }

        private void UsersListButton_Click(object sender, RoutedEventArgs e)
        {
            OperationField.Navigate(new ListenersBanListPage());
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            OperationField.Navigate(new StatisticsPage());
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

        private void OperationField_Navigated(object sender, NavigationEventArgs e)
        {
            UpdateButtonState();
        }
    }
}
