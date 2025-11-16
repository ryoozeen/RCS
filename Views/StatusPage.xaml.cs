using System.Windows;
using System.Windows.Controls;

namespace DotBotCarClient.Views
{
    public partial class StatusPage : Page
    {
        public StatusPage()
        {
            InitializeComponent();
        }

        // 버튼들 -------------------------
        private void AirBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("에어컨 제어 예정");
        }

        private void LockBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("문 잠금 / 해제 요청 예정");
        }

        private void TrunkBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("트렁크 제어 예정");
        }

        private void GoCharging(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ChargingPage());
        }

        private void GoControls(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ControlsPage());
        }

        // 서버 연동 후 상태 업데이트
        public void UpdateStatus(int battery, string state)
        {
            BatteryText.Text = $" {battery}%";
            CarStateText.Text = $"차량 상태 : {state}";
        }
    }
}
