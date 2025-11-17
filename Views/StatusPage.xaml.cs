using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace DotBotCarClient.Views
{
    public partial class StatusPage : Page
    {
        public StatusPage()
        {
            InitializeComponent();
        }

        // ===============================
        //   POWER / LOCK / TRUNK 버튼
        // ===============================

        private void start_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("시동 제어 예정");
        }

        private void Lock_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("문 잠금 / 해제 예정");
        }

        private void Trunk_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("트렁크 열기 / 닫기 예정");
        }


        // ===============================
        //   하단 네비게이션 버튼
        // ===============================

        private void GoCharging(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ChargingPage());
        }

        private void GoControls(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ControlsPage());
        }


        // ===============================
        //   서버 연동 후 UI 업데이트
        // ===============================

        public void UpdateStatus(int battery, string state)
        {
            BatteryText.Text = $" {battery}%";
            CarStateText.Text = $"차량 상태 : {state}";
        }
    }
}
