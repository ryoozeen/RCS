using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;

namespace DotBotCarClient.Views
{
    /// <summary>
    /// EnrollPage.xaml 상호작용 로직
    /// </summary>
    public partial class EnrollPage : Page
    {
        public EnrollPage()
        {
            InitializeComponent();
        }

        // 회원가입 완료 버튼
        private void BtnEnrollOk_Click(object sender, RoutedEventArgs e)
        {
            string id = txtId.Text.Trim();
            string name = txtName.Text.Trim();
            string pw = pwBox.Password;
            string pw2 = pwConfirmBox.Password;

            // 1) 필수값 체크
            if (string.IsNullOrEmpty(id) ||
                string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(pw) ||
                string.IsNullOrEmpty(pw2))
            {
                MessageBox.Show("모든 항목을 입력해 주세요.", "회원가입",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2) 비밀번호 일치
            if (pw != pw2)
            {
                MessageBox.Show("비밀번호가 서로 일치하지 않습니다.", "회원가입",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3) 비밀번호 길이 간단 검증 (원하면 규칙 더 추가 가능)
            if (pw.Length < 6)
            {
                MessageBox.Show("비밀번호는 6자 이상으로 설정해 주세요.", "회원가입",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: 여기에서 서버로 회원가입 요청 보내기
            // ex) await CarClientApi.RegisterAsync(id, pw, name);

            MessageBox.Show("회원가입이 완료되었습니다.\n로그인 화면으로 이동합니다.",
                "회원가입", MessageBoxButton.OK, MessageBoxImage.Information);

            // 로그인 페이지로 이동
            NavigationService?.Navigate(new LoginPage());
        }

        // 취소 버튼 → 로그인 화면/이전 화면으로 돌아가기
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
            }
            else
            {
                NavigationService?.Navigate(new LoginPage());
            }
        }
    }
}
