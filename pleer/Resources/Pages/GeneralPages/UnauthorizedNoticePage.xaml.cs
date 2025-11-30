using pleer.Resources.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace pleer.Resources.Pages.GeneralPages
{
    /// <summary>
    /// Логика взаимодействия для UnauthorizedNoticePage.xaml
    /// </summary>
    public partial class UnauthorizedNoticePage : Page
    {
        ListenerMainWindow _listenerMain;


        public UnauthorizedNoticePage(ListenerMainWindow main)
        {
            InitializeComponent();

            _listenerMain = main;
        }

        private void TurnToAuthorization_Click(object sender, MouseButtonEventArgs e)
        {
            if (_listenerMain != null)
                _listenerMain.FullWindow.Navigate(new LoginPage(_listenerMain));
        }
    }
}
