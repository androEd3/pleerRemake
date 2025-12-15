using Microsoft.EntityFrameworkCore;
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
    /// Логика взаимодействия для RegistrationPage.xaml
    /// </summary>
    public partial class RegistrationPage : Page
    {
        DBContext _context = new();

        ListenerMainWindow _listenerMain;

        public RegistrationPage(ListenerMainWindow main)
        {
            InitializeComponent();

            _listenerMain = main;
        }

        void UserActiveGrid()
        {
            _listenerMain.FullWindow.Visibility = Visibility.Collapsed;
            _listenerMain.MainGrid.Effect = null;
            _listenerMain.MainGrid.IsEnabled = true;
        }

        private async void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_listenerMain != null)
                {
                    if (!CheckUserDataValid())
                        return;

                    var isEmailValid = ServiceMethods.IsValidEmail(UserEmail.Text);
                    if (!isEmailValid)
                    {
                        ErrorNoticePanel.Text = "Неправильный формат почты";
                        return;
                    }

                    if (await _context.Listeners.FirstOrDefaultAsync(l => l.Email == UserEmail.Text) != default)
                    {
                        ErrorNoticePanel.Text = "Слушатель с такой почтой уже существует";
                        return;
                    }

                    var isPasswordSame = ServiceMethods.IsPasswordsSame(UserPassword.Password, RepeatedUserPassword.Password);
                    if (!isPasswordSame)
                    {
                        ErrorNoticePanel.Text = "Пароли не совпадают";
                        return;
                    }

                    var isPasswordValid = ServiceMethods.IsPasswordsValidOutput(UserPassword.Password);
                    if (isPasswordValid != UserPassword.Password)
                    {
                        ErrorNoticePanel.Text = isPasswordValid;
                        return;
                    }

                    var passwordHash = ServiceMethods.GetSha256Hash(UserPassword.Password);

                    var profilePicture = _context.ProfilePictures
                        .FirstOrDefault(pp => pp.FilePath == InitializeData.GetDefaultProfilePicturePath());

                    var newListener = new Listener()
                    {
                        Name = UserName.Text,
                        Email = UserEmail.Text,
                        ProfilePictureId = profilePicture.Id, // default
                        PasswordHash = passwordHash,
                        CreatedAt = DateOnly.FromDateTime(DateTime.Now),
                    };
                    await _context.Listeners.AddAsync(newListener);
                    await _context.SaveChangesAsync();

                    await ServiceMethods.AddPlaylistWithLink(newListener);

                    MessageBox.Show("Вы успешно зарегистрировались", Title = "Регистрация",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    _listenerMain.FullWindow.Navigate(new LoginPage(_listenerMain));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Во время регистрации произошла ошибка: {ex}", Title = "Регистрация",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool isUpdating = false;

        // Первый пароль
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

        // Повторный пароль
        private void RepeatedUserPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                RepeatedUserPasswordVisible.Text = RepeatedUserPassword.Password;
                isUpdating = false;
            }
        }

        private void RepeatedUserPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isUpdating)
            {
                isUpdating = true;
                RepeatedUserPassword.Password = RepeatedUserPasswordVisible.Text;
                isUpdating = false;
            }
        }

        private void ToggleRepeatedPasswordVisibility(object sender, MouseButtonEventArgs e)
        {
            if (RepeatedUserPassword.Visibility == Visibility.Visible)
            {
                RepeatedUserPassword.Visibility = Visibility.Collapsed;
                RepeatedUserPasswordVisible.Visibility = Visibility.Visible;
            }
            else
            {
                RepeatedUserPassword.Visibility = Visibility.Visible;
                RepeatedUserPasswordVisible.Visibility = Visibility.Collapsed;
            }
        }

        bool CheckUserDataValid()
        {
            if (string.IsNullOrEmpty(UserEmail.Text) ||
                string.IsNullOrEmpty(UserPassword.Password) ||
                string.IsNullOrEmpty(RepeatedUserPassword.Password) ||
                string.IsNullOrEmpty(UserName.Text))
            {
                ErrorNoticePanel.Text = "Заполните все необходимые поля";
                return false;
            }
            else
                return true;
        }

        private void TurnToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            if (_listenerMain != null)
                _listenerMain.FullWindow.Navigate(new LoginPage(_listenerMain));
        }

        private void CloseFullWindowFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_listenerMain != null)
                UserActiveGrid();
        }

        private void TurnToPassword_Click(object sender, MouseButtonEventArgs e)
        {
            EmailPanel.Visibility = Visibility.Collapsed;
            PasswordPanel.Visibility = Visibility.Visible;
        }

        private void TurnToEmail_Click(object sender, MouseButtonEventArgs e)
        {
            PasswordPanel.Visibility = Visibility.Collapsed;
            EmailPanel.Visibility = Visibility.Visible;
        }
    }
}
