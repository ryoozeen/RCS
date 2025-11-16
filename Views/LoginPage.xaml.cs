using System.Windows;
using System.Windows.Controls;
using DotBotCarClient.Views;

namespace DotBotCarClient.Views
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // 나중에 서버 연결 추가할 자리
            NavigationService.Navigate(new StatusPage());
        }

        private void BtnEnroll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("회원가입은 나중에 구현됩니다.");
        }
    }
}
