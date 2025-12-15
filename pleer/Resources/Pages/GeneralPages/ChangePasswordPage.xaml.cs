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
            string oldPassword = ServiceMethods.IsPasswordsValidOutput(OldUserPassword.Password);
            if (oldPassword != OldUserPassword.Password)
            {
                ErrorNoticePanel.Style = Application.Current.TryFindResource("SmallErrorPanel") as Style;
                ErrorNoticePanel.Text = oldPassword;
                return;
            }

            var isPasswordsSame = ServiceMethods.IsPasswordsSame(NewUserPassword.Password, RepeatedNewUserPassword.Password);
            if (!isPasswordsSame)
            {
                ErrorNoticePanel.Style = Application.Current.TryFindResource("SmallErrorPanel") as Style;
                ErrorNoticePanel.Text = "Пароли не совпадают";
                return;
            }

            string newPassword = ServiceMethods.IsPasswordsValidOutput(NewUserPassword.Password);
            if (newPassword != NewUserPassword.Password)
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

        private bool isUpdating = false;
        private void OldUserPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                OldUserPasswordVisible.Text = OldUserPassword.Password;
                isUpdating = false;
            }
        }

        private void OldUserPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                OldUserPassword.Password = OldUserPasswordVisible.Text;
                isUpdating = false;
            }
        }

        private void ToggleOldPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (OldUserPassword.Visibility == Visibility.Visible)
            {
                OldUserPassword.Visibility = Visibility.Collapsed;
                OldUserPasswordVisible.Visibility = Visibility.Visible;
                OldUserPasswordVisible.Focus();
                OldUserPasswordVisible.CaretIndex = OldUserPasswordVisible.Text.Length;
            }
            else
            {
                OldUserPassword.Visibility = Visibility.Visible;
                OldUserPasswordVisible.Visibility = Visibility.Collapsed;
                OldUserPassword.Focus();
            }
        }

        private void NewUserPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                NewUserPasswordVisible.Text = NewUserPassword.Password;
                isUpdating = false;
            }
        }

        private void NewUserPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                NewUserPassword.Password = NewUserPasswordVisible.Text;
                isUpdating = false;
            }
        }

        private void ToggleNewPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (NewUserPassword.Visibility == Visibility.Visible)
            {
                NewUserPassword.Visibility = Visibility.Collapsed;
                NewUserPasswordVisible.Visibility = Visibility.Visible;
                NewUserPasswordVisible.Focus();
                NewUserPasswordVisible.CaretIndex = NewUserPasswordVisible.Text.Length;
            }
            else
            {
                NewUserPassword.Visibility = Visibility.Visible;
                NewUserPasswordVisible.Visibility = Visibility.Collapsed;
                NewUserPassword.Focus();
            }
        }

        private void RepeatedNewUserPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                RepeatedNewUserPasswordVisible.Text = RepeatedNewUserPassword.Password;
                isUpdating = false;
            }
        }

        private void RepeatedNewUserPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                RepeatedNewUserPassword.Password = RepeatedNewUserPasswordVisible.Text;
                isUpdating = false;
            }
        }

        private void ToggleRepeatedNewPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (RepeatedNewUserPassword.Visibility == Visibility.Visible)
            {
                RepeatedNewUserPassword.Visibility = Visibility.Collapsed;
                RepeatedNewUserPasswordVisible.Visibility = Visibility.Visible;
                RepeatedNewUserPasswordVisible.Focus();
                RepeatedNewUserPasswordVisible.CaretIndex = RepeatedNewUserPasswordVisible.Text.Length;
            }
            else
            {
                RepeatedNewUserPassword.Visibility = Visibility.Visible;
                RepeatedNewUserPasswordVisible.Visibility = Visibility.Collapsed;
                RepeatedNewUserPassword.Focus();
            }
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
