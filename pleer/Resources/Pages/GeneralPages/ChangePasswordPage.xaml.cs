using pleer.Models.DatabaseContext;
using pleer.Models.Service;
using pleer.Models.Users;
using pleer.Resources.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace pleer.Resources.Pages.GeneralPages
{
    /// <summary>
    /// Логика взаимодействия для ChangePasswordPage.xaml
    /// </summary>
    public partial class ChangePasswordPage : Page
    {
        DBContext _context = new();

        ListenerMainWindow _listenerMain;

        Listener _listener;

        public ChangePasswordPage(ListenerMainWindow main, Listener listener)
        {
            InitializeComponent();

            _listenerMain = main;
            _listener = listener;
        }

        void UserActiveGrid()
        {
            _listenerMain.FullWindow.Visibility = Visibility.Collapsed;
            _listenerMain.MainGrid.Effect = null;
            _listenerMain.MainGrid.IsEnabled = true;
        }

        private async void SaveUserDataButton_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = ServiceMethods.IsPasswordsValidOutput(OldUserPassword.Text);
            if (oldPassword != OldUserPassword.Text)
            {
                ErrorNoticePanel.Style = Application.Current.TryFindResource("SmallErrorPanel") as Style;
                ErrorNoticePanel.Text = oldPassword;
                return;
            }

            var isPasswordsSame = ServiceMethods.IsPasswordsSame(NewUserPassword.Text, RepeatedNewUserPassword.Text);
            if (!isPasswordsSame)
            {
                ErrorNoticePanel.Style = Application.Current.TryFindResource("SmallErrorPanel") as Style;
                ErrorNoticePanel.Text = "Пароли не совпадают";
                return;
            }

            string newPassword = ServiceMethods.IsPasswordsValidOutput(NewUserPassword.Text);
            if (newPassword != NewUserPassword.Text)
            {
                ErrorNoticePanel.Style = Application.Current.TryFindResource("SmallErrorPanel") as Style;
                ErrorNoticePanel.Text = newPassword;
                return;
            }

            try
            {
                var listener = await _context.Listeners
                    .FindAsync(_listener.Id);

                if (listener != null)
                {
                    string oldPasswordHash = ServiceMethods.GetSha256Hash(oldPassword);
                    if (oldPasswordHash != listener.PasswordHash)
                    {
                        ErrorNoticePanel.Style = Application.Current.TryFindResource("SmallErrorPanel") as Style;
                        ErrorNoticePanel.Text = "Неверный старый пароль";
                        return;
                    }
                    await ChangeListenerPassword(listener, newPassword);
                    ChangeListenerPassword(listener, newPassword);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка смены пароля: {ex}", Title = "Профиль",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        async Task ChangeListenerPassword(Listener listener, string newPassword)
        {
            string newPasswordHash = ServiceMethods.GetSha256Hash(newPassword);

            listener.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync();

            MessageBox.Show("Пароль успешно изменен", Title = "Профиль",
                                MessageBoxButton.OK, MessageBoxImage.Information);

            _listenerMain.FullWindow.Navigate(new ProfilePage(_listenerMain, _listener));
        }

        private void TurnToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            if (_listenerMain != null)
                _listenerMain.FullWindow.Navigate(new ProfilePage(_listenerMain, _listener));
        }

        private void CloseFullWindowFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_listenerMain != null)
                UserActiveGrid();
        }
    }
}
