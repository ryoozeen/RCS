using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using DotBotCarClient.Protocol;
using DotBotCarClient.Network;

namespace DotBotCarClient.Views
{
    public partial class StatusPage : Page, IProtocolHandler
    {
        public StatusPage()
        {
            InitializeComponent();

            // 초기 테스트용 데이터는 그대로 유지
            UpdateTemperature(22, 18);
            UpdateRange(320);
            UpdateLocation("집", "2분 전");
            UpdateLastTrip(15.2);
        }

        // ==========================================================
        // 서버 메시지 처리 (STATUS_REQ 푸시 업데이트)
        // ==========================================================
        public async void HandleProtocolMessage(BaseMessage msg)
        {
            if (msg.Msg != MsgType.STATUS_REQ)
                return;

            var st = msg as StatusReq;
            if (st == null)
                return;

            // ---------- UI 업데이트 ----------
            Dispatcher.Invoke(() =>
            {
                BatteryText.Text = $"{st.Battery}%";

                string state = "대기 중";
                if (st.Charging) state = "충전 중";
                else if (st.Driving) state = "주행 중";
                else if (st.Parking) state = "주차 중";

                CarStateText.Text = $"차량 상태 : {state}";
            });

            // ---------- 서버에 "정상 수신" 응답 ----------
            var res = new StatusRes { resulted = true };
            await App.Network.SendAsync(res);
        }

        // ==========================================================
        // 테스트 데이터용 UI 함수들 (그대로 유지)
        // ==========================================================

        public void UpdateTemperature(int indoor, int outdoor)
        {
            TempText.Text = $"실내 {indoor}°C";
            OutTempText.Text = $"실외 {outdoor}°C";
        }

        public void UpdateRange(int range)
        {
            RangeText.Text = $"Range: {range} km";
        }

        public void UpdateLocation(string location, string time)
        {
            LocationText.Text = $"주차 위치: {location} ({time})";
        }

        public void UpdateLastTrip(double distance)
        {
            LastTripDistanceText.Text = $"{distance:F1} km";
        }

        public void UpdateStatus(int battery, string state)
        {
            BatteryText.Text = $" {battery}%";
            CarStateText.Text = $"차량 상태 : {state}";
        }

        // ==========================================================
        // 버튼 이벤트 (그대로 유지)
        // ==========================================================

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

        private void GoCharging(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ChargingPage());
        }

        private void GoControls(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ControlsPage());
        }
    }
}
