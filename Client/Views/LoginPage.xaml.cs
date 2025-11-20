using DotBotCarClient.Protocol;
using DotBotCarClient.Views;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MyApp.Helpers;

namespace DotBotCarClient.Views
{
    public partial class LoginPage : Page, IProtocolHandler
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        // 서버 응답 메시지를 처리하는 공식 메서드 (IProtocolHandler 방식)
        public void HandleProtocolMessage(BaseMessage msg)
        {
            if (msg is LoginRes res)
            {
                if (res.logined)
                {
                    NavigationService?.Navigate(new StatusPage());
                }
                else
                {
                    MessageBox.Show($"로그인 실패: {res.reason}", "ERROR");
                }
            }
        }
        // 로그인 요청 전송
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtLoginId.Text) || string.IsNullOrEmpty(pwLogin.Password))
            {
                MessageBox.Show("아이디와 비밀번호를 입력하세요");
                return;
            }

            var msg = new LoginReq
            {
                id = txtLoginId.Text,
                password = SecurityHelper.ComputeSHA256(pwLogin.Password)
            };

            await App.Network.SendAsync(msg);
        }

        // 회원가입 화면 이동
        private void BtnEnroll_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EnrollPage());
        }
    }
}