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
            // 테스트용 초기 데이터
            UpdateTemperature(22, 18);
            UpdateRange(320);
            UpdateLocation("집", "2분 전");
            UpdateLastTrip(15.2);
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

        // 온도 업데이트
        public void UpdateTemperature(int indoor, int outdoor)
        {
            TempText.Text = $"실내 {indoor}°C";
            OutTempText.Text = $"실외 {outdoor}°C";
        }

        // 주행거리 업데이트
        public void UpdateRange(int range)
        {
            RangeText.Text = $"Range: {range} km";
        }

        // 위치 업데이트
        public void UpdateLocation(string location, string time)
        {
            LocationText.Text = $"주차 위치: {location} ({time})";
        }

        // 최근 이동거리 업데이트
        public void UpdateLastTrip(double distance)
        {
            LastTripDistanceText.Text = $"{distance:F1} km";
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
