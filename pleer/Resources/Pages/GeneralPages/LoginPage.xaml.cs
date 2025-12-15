using pleer.Models.DatabaseContext;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace pleer.Resources.Pages.GeneralPages
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        DBContext _context = new();

        ListenerMainWindow _listenerMain;
        AdminMainWindow _adminMain;

        Listener _listener;
        Admin _admin;

        public LoginPage(ListenerMainWindow main)
        {
            InitializeComponent();

            _listenerMain = main;

            Loaded += async (s, e) => await LoadListenerLoginPage();
        }

        async Task LoadListenerLoginPage()
        {
            if (_context.Listeners.Any())
            {
                _listener = _context.Listeners.First();
                //await OpenNewWindow(_listener);
            }
            UserInactiveGrid();
        }

        public LoginPage(AdminMainWindow main)
        {
            InitializeComponent();

            _adminMain = main;

            Loaded += async (s, e) => await LoadAdminLoginPage();
        }

        async Task LoadAdminLoginPage()
        {
            if (_context.Admins.Any())
            {
                _admin = _context.Admins.First();
                //await OpenNewWindow(_admin);
            }

            CloseFullWindowFrameButton.Visibility = Visibility.Collapsed;
            TurnToRegistration.Visibility = Visibility.Collapsed;

            EmailTextBlock.Text = "Логин";

            AdminInactiveGrid();
        }

        void UserInactiveGrid()
        {
            _listenerMain.FullWindow.Visibility = Visibility.Visible;
            _listenerMain.MainGrid.Effect = new BlurEffect { Radius = 15, KernelType = KernelType.Gaussian };
            _listenerMain.MainGrid.IsEnabled = false;
        }

        void UserActiveGrid()
        {
            _listenerMain.FullWindow.Visibility = Visibility.Collapsed;
            _listenerMain.MainGrid.Effect = null;
            _listenerMain.MainGrid.IsEnabled = true;
        }

        void AdminInactiveGrid()
        {
            _adminMain.MainGrid.Effect = new BlurEffect { Radius = 15, KernelType = KernelType.Gaussian };
            _adminMain.MainGrid.IsEnabled = false;
            _adminMain.BackButton.IsEnabled = false; _adminMain.ForwardButton.IsEnabled = false;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckUserDataValid())
                return;

            string password = string.Empty;

            if (_adminMain == null)
            {
                var isEmailValid = ServiceMethods.IsValidEmail(UserEmail.Text);
                if (!isEmailValid)
                {
                    ErrorNoticePanel.Text = "Неверный формат почты";
                    return;
                }
                else ErrorNoticePanel.Text = string.Empty;

                password = ServiceMethods.IsPasswordsValidOutput(UserPassword.Password);
                if (password != UserPassword.Password)
                {
                    ErrorNoticePanel.Text = password;
                    return;
                }
                else ErrorNoticePanel.Text = string.Empty;
            }

            try
            {
                if (_listenerMain != null)
                {
                    _listener = _context.Listeners
                        .FirstOrDefault(u => u.Email == UserEmail.Text);

                    if (_listener != default && _listener.Status)
                    if (_listener.Status)
                    {
                        ErrorNoticePanel.Text = "Ваш аккаунт был временно заблокирован, дождитесь его разблокировки";
                        return;
                    }

                    if (_listener != null)
                    {
                        var pass = ServiceMethods.GetSha256Hash(password);

                        if (_listener.PasswordHash != pass)
                        {
                            ErrorNoticePanel.Text = "Неверный пароль";
                            return;
                        }
                        else ErrorNoticePanel.Text = string.Empty;

                        await OpenNewWindow(_listener);
                    }
                    else
                    {
                        ErrorNoticePanel.Text = "Пользователь не найден";
                        return;
                    }
                }

                if (_adminMain != null)
                {
                    _admin = _context.Admins
                        .FirstOrDefault(a => a.Login == UserEmail.Text);

                    if (_admin != default)
                    {
                        var passwordHash = ServiceMethods.GetSha256Hash(UserPassword.Password);

                        if (_admin.PasswordHash != passwordHash)
                        {
                            ErrorNoticePanel.Text = "Неверный пароль";
                            return;
                        }

                        await OpenNewWindow(_admin);
                    }
                    else
                    {
                        ErrorNoticePanel.Text = "Администратор не найден";
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        async Task OpenNewWindow(Listener listener)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                var newWindow = new ListenerMainWindow(listener);
                Application.Current.MainWindow = newWindow;

                newWindow.Show();
                _listenerMain.Close();
            }), DispatcherPriority.Background);
        }

        async Task OpenNewWindow(Admin admin)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                var newWindow = new AdminMainWindow(admin);
                Application.Current.MainWindow = newWindow;

                newWindow.Show();
                _adminMain.Close();
            }), DispatcherPriority.Background);
        }

        private bool isUpdating = false;
        private void UserPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                UserPasswordVisible.Text = UserPassword.Password;
                isUpdating = false;
            }
        }

        private void UserPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                UserPassword.Password = UserPasswordVisible.Text;
                isUpdating = false;
            }
        }

        private void TogglePasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (UserPassword.Visibility == Visibility.Visible)
            {
                UserPassword.Visibility = Visibility.Collapsed;
                UserPasswordVisible.Visibility = Visibility.Visible;
            }
            else
            {
                UserPassword.Visibility = Visibility.Visible;
                UserPasswordVisible.Visibility = Visibility.Collapsed;
            }
        }

        bool CheckUserDataValid()
        {
            if (string.IsNullOrEmpty(UserEmail.Text) ||
                string.IsNullOrEmpty(UserPassword.Password))
            {
                ErrorNoticePanel.Text = "Заполните все необходимые поля";
                return false;
            }
            else
                return true;
        }

        private void TurnToRegistration_Click(object sender, MouseButtonEventArgs e)
        {
            if (_listenerMain != null)
                _listenerMain.FullWindow.Navigate(new RegistrationPage(_listenerMain));
        }

        private void CloseFullWindowFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_listenerMain != null)
                UserActiveGrid();
        }
    }
}
